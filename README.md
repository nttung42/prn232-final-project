# CourtSide - Badminton Booking System 🏸

> **Đồ án môn học PRN232 (.NET Web Application Development)**  
> Hệ thống quản lý và đặt sân cầu lông trực tuyến dành cho chủ sân và người chơi, tích hợp thanh toán VietQR Napas247, lưu trữ ảnh Cloudflare R2, xuất báo cáo đa định dạng (CSV, XML, JSON) và hỗ trợ OData v8.

---

## 🚀 Công nghệ sử dụng (Tech Stack)

| Thành phần | Công nghệ / Thư viện |
| :--- | :--- |
| **Backend** | .NET 8.0 Web API, Entity Framework Core 8.0 (Code First, SQL Server), OData v8 |
| **Bảo mật** | JWT Bearer Authentication, BCrypt.Net-Next (mã hóa mật khẩu) |
| **Tích hợp** | VietQR (chuẩn Napas247 QuickLink), Cloudflare R2 Storage (AWS S3 SDK) |
| **Frontend** | ASP.NET Core Razor Pages 8.0, Tailwind CSS, Lucide Icons, JavaScript (Fetch API) |
| **Định dạng dữ liệu** | Content Negotiation (JSON, CSV, XML) qua HTTP Accept Headers |

---

## 🏛️ Cấu trúc dự án (Solution Architecture)

```text
BadmintonBookingSystem/
├── src/
│   ├── BadmintonBooking.Domain/      # Entities, Enums, Constants (Core Business Models)
│   ├── BadmintonBooking.DAL/         # DbContext, Repositories, Unit of Work, Migrations
│   ├── BadmintonBooking.BLL/         # DTOs, AutoMapper, Business Services, VietQR, R2 Storage
│   ├── BadmintonBooking.WebAPI/      # ASP.NET Core Web API 8.0, OData Controllers, Formatters
│   └── BadmintonBooking.WebClient/   # Razor Pages Web Client, UI Tailwind CSS, Stitch-designed
└── docs/                             # Tài liệu đặc tả kỹ thuật và UI/UX
```

---

## ✨ Tính năng nổi bật

1. **Phân quyền 3 vai trò (RBAC)**:
   - **Admin**: Quản lý tài khoản người dùng, duyệt cơ sở sân mới, xem báo cáo tổng thể.
   - **Provider**: Quản lý cụm sân, thiết lập giá theo khung giờ cao điểm (Peak hours), xác nhận đơn đặt.
   - **Player**: Tìm kiếm sân theo quận/giá, chọn giờ trên ma trận trực quan (Live Matrix), đặt sân & quản lý lịch chơi.
2. **Ma trận đặt sân thời gian thực (Live Court Matrix)**:
   - Hiển thị tình trạng sân theo từng khung giờ (Trống / Đang chọn / Đã đặt).
   - Nhận diện khung giờ vàng (17:00 - 20:00) với mức giá chênh lệch linh hoạt.
3. **Ví điện tử CourtSide & VietQR**:
   - Tự động sinh mã VietQR Napas247 quét thanh toán tức thì qua ứng dụng ngân hàng.
   - Nạp tiền, trừ ví thanh toán đơn đặt sân và hoàn tiền khi hủy lịch.
4. **Quản lý & Tải ảnh Cloudflare R2**:
   - Lưu trữ thư viện ảnh cơ sở dưới định dạng JSON (`GalleryImagesJson`).
   - Tải ảnh trực tiếp lên Cloudflare R2 bucket `prn232_bucket` (kèm cơ chế fallback an toàn lưu cục bộ).
5. **OData v8 & Xuất báo cáo đa định dạng**:
   - Lọc dữ liệu chuẩn OData: `$filter`, `$orderby`, `$top`, `$skip`.
   - Xuất dữ liệu báo cáo đơn đặt sân linh hoạt theo 3 định dạng: CSV, XML, JSON.

---

## 🛠️ Hướng dẫn cài đặt & Khởi chạy (Getting Started)

### 1. Yêu cầu môi trường
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server (LocalDB hoặc SQL Server 2019/2022)

### 2. Cấu hình Connection String
Mở file `src/BadmintonBooking.WebAPI/appsettings.json` và cập nhật chuỗi kết nối SQL Server nếu cần:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BadmintonBookingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Chạy Backend (WebAPI)
Tại thư mục gốc dự án:
```bash
dotnet run --project src/BadmintonBooking.WebAPI
```
- **Swagger UI**: [http://localhost:6789](http://localhost:6789)
- Khi khởi động lần đầu, hệ thống sẽ **tự động áp dụng Migration và Seed dữ liệu mẫu**.

### 4. Chạy Frontend (WebClient)
Mở một cửa sổ Terminal mới:
```bash
dotnet run --project src/BadmintonBooking.WebClient
```
- **Web App**: [http://localhost:3456](http://localhost:3456)

---

## 👥 Tài khoản thử nghiệm (Demo Accounts)

| Vai trò | Email đăng nhập | Mật khẩu mặc định |
| :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin@badminton.com` | `Admin@123` |
| **Chủ sân (Provider)** | `provider1@badminton.com` | `Provider@123` |
| **Người chơi (Player)** | `player1@badminton.com` | `Player@123` |
