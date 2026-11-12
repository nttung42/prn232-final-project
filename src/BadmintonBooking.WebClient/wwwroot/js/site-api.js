/**
 * CourtSide - API Client Helper & Session Manager
 * Đồng bộ với ASP.NET Core Web API, OData v8 và Content Negotiation
 */

const API_BASE_URL = window.API_BASE_URL || "http://localhost:6789";

// Header bắt buộc cho mọi request thay đổi dữ liệu (chống CSRF). Form/đường dẫn của bên thứ ba
// không thể gắn header tùy chỉnh này nếu không qua CORS cho phép.
const CSRF_HEADER = { "X-Requested-With": "CourtSideWeb" };

// Escape nội dung đưa vào innerHTML để chống XSS. Mọi dữ liệu người dùng (tên, địa chỉ, ghi chú...) phải đi qua hàm này.
function escapeHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

// 1. Phiên đăng nhập: JWT nằm trong cookie HttpOnly do API đặt (JavaScript KHÔNG đọc được token).
// localStorage chỉ giữ thông tin hiển thị (id, tên, vai trò). Quyền thật được API kiểm tra qua cookie.
// Xóa token cũ (nếu còn từ phiên bản trước) để không để lại bí mật trong trình duyệt.
localStorage.removeItem("courtside_token");

const AuthManager = {
    getUser() {
        const userStr = localStorage.getItem("courtside_user");
        try {
            return userStr ? JSON.parse(userStr) : null;
        } catch (e) {
            return null;
        }
    },
    setSession(user) {
        if (user) {
            localStorage.setItem("courtside_user", JSON.stringify(user));
        }
    },
    clearSession() {
        localStorage.removeItem("courtside_user");
    },
    // Đã có thông tin người dùng. Nếu cookie đã hết hạn, API trả 401 và callApi sẽ đưa về trang đăng nhập.
    isAuthenticated() {
        return !!this.getUser();
    },
    getUserRole() {
        const user = this.getUser();
        return user ? user.role : null;
    },
    async logout() {
        try {
            await fetch(`${API_BASE_URL}/api/auth/logout`, { method: "POST", credentials: "include", headers: CSRF_HEADER });
        } catch (e) {
            // Mạng lỗi vẫn xóa phiên phía client
        }
        this.clearSession();
        window.location.href = "/Auth/Login";
    },
    requireAuth(allowedRoles = []) {
        if (!this.isAuthenticated()) {
            window.location.href = `/Auth/Login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
            return false;
        }
        if (allowedRoles && allowedRoles.length > 0) {
            const role = this.getUserRole();
            if (!allowedRoles.includes(role)) {
                alert("Bạn không có quyền truy cập trang này!");
                if (role === "Admin") window.location.href = "/Admin/Dashboard";
                else if (role === "Provider") window.location.href = "/Provider/Facilities";
                else window.location.href = "/";
                return false;
            }
        }
        return true;
    }
};

// 2. OData Query Helper
function buildODataQuery({ filter, orderby, top, skip, select, expand, count } = {}) {
    const params = [];
    if (filter) params.push(`$filter=${encodeURIComponent(filter)}`);
    if (orderby) params.push(`$orderby=${encodeURIComponent(orderby)}`);
    if (top !== undefined && top !== null) params.push(`$top=${top}`);
    if (skip !== undefined && skip !== null) params.push(`$skip=${skip}`);
    if (select) params.push(`$select=${encodeURIComponent(select)}`);
    if (expand) params.push(`$expand=${encodeURIComponent(expand)}`);
    if (count) params.push(`$count=true`);
    return params.length > 0 ? `?${params.join("&")}` : "";
}

// 3. Centralized API Fetcher
async function callApi(endpoint, { method = "GET", data = null, accept = "application/json", isBlob = false } = {}) {
    const url = endpoint.startsWith("http") ? endpoint : `${API_BASE_URL}${endpoint}`;

    // JWT đi kèm qua cookie HttpOnly (credentials: include), không đọc từ JavaScript
    const headers = {
        "Accept": accept,
        ...CSRF_HEADER
    };

    const options = {
        method: method.toUpperCase(),
        headers,
        credentials: "include"
    };

    if (data && ["POST", "PUT", "PATCH"].includes(options.method)) {
        if (data instanceof FormData) {
            options.body = data;
        } else {
            headers["Content-Type"] = "application/json";
            options.body = JSON.stringify(data);
        }
    }

    try {
        const response = await fetch(url, options);

        // Xử lý 401 Unauthorized
        if (response.status === 401) {
            AuthManager.clearSession();
            if (window.Swal) {
                await Swal.fire({
                    icon: "warning",
                    title: "Hết phiên đăng nhập",
                    text: "Vui lòng đăng nhập lại để tiếp tục.",
                    confirmButtonColor: "#06525c"
                });
            }
            window.location.href = `/Auth/Login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
            throw new Error("Unauthorized");
        }

        // Xử lý 403 Forbidden
        if (response.status === 403) {
            if (window.Swal) {
                Swal.fire({
                    icon: "error",
                    title: "Không có quyền",
                    text: "Bạn không có quyền thực hiện thao tác này.",
                    confirmButtonColor: "#06525c"
                });
            } else {
                alert("Bạn không có quyền thực hiện thao tác này.");
            }
            throw new Error("Forbidden");
        }

        // Tải file trực tiếp (Content Negotiation: CSV / XML / Download Blob)
        if (isBlob || accept === "text/csv" || accept === "application/xml") {
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || `Lỗi tải file (${response.status})`);
            }
            const blob = await response.blob();
            return blob;
        }

        // Không có nội dung trả về (204 No Content)
        if (response.status === 204) {
            return null;
        }

        // Kiểm tra phản hồi lỗi
        if (!response.ok) {
            let errorMsg = `Lỗi yêu cầu (${response.status})`;
            try {
                const errData = await response.json();
                // ASP.NET ModelState errors nằm trong errData.errors: { Field: [msg, ...] }
                const fieldErrors = errData.errors
                    ? Object.values(errData.errors).flat().filter(Boolean)
                    : [];
                errorMsg = fieldErrors.length > 0
                    ? fieldErrors.join("\n")
                    : (errData.message || errData.title || JSON.stringify(errData));
            } catch (e) {
                errorMsg = await response.text();
            }
            throw new Error(errorMsg);
        }

        // Parse JSON
        return await response.json();
    } catch (error) {
        console.error(`[API Error] ${method} ${endpoint}:`, error);
        throw error;
    }
}

// 4. Helper tải file về trình duyệt từ Blob (Dùng cho Export CSV/XML/JSON)
function triggerDownload(blob, filename) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
}

// 5. Formatters
function formatCurrency(amount) {
    if (amount === undefined || amount === null) return "0đ";
    return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(amount);
}

function formatDate(dateStr) {
    if (!dateStr) return "";
    const d = new Date(dateStr);
    return d.toLocaleDateString("vi-VN", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatTime(timeStr) {
    if (!timeStr) return "";
    return timeStr.substring(0, 5);
}

