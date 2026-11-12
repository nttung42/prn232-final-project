# 04. Thanh toán, Đối soát và Bảo mật

Tài liệu này mô tả luồng tiền thực tế đang chạy trong hệ thống và các biện pháp bảo mật đã áp dụng. Khi có thay đổi ở `WalletService`, `BookingService`, `AuthController`, `Program.cs` hoặc `site-api.js`, cần cập nhật tài liệu này.

---

## 1. Ví CourtSide và luồng thanh toán

Hệ thống dùng **một tài khoản ngân hàng mặc định** (cấu hình `VietQr` trong `.env`) và **đối soát thủ công**: mỗi yêu cầu chuyển khoản có một mã riêng `CSIDE` + 8 ký tự hex. Người chơi chuyển khoản đúng số tiền và đúng nội dung mã; Admin hoặc chủ sân đối chiếu sao kê rồi xác nhận trên hệ thống.

Webhook SePay đã viết nhưng đang **tắt** (`Sepay:Enabled=false`, endpoint trả 404). Muốn bật lại cần cấu hình `Sepay:Enabled=true`, `Sepay:ApiKey`, và URL công khai.

### 1.1. Nạp tiền vào ví

1. Người chơi vào **Ví CourtSide**, chọn số tiền và bấm **Nạp tiền qua VietQR**. API tạo `PaymentOrder` (`Purpose = TopUp`, hết hạn sau 24 giờ) và `WalletTransaction` `Pending` cùng mã.
2. Người chơi chuyển khoản đúng số tiền, nội dung là mã hiển thị trên QR.
3. Admin mở **Đối soát Chuyển khoản** (`/Admin/Topups`), thấy yêu cầu đang chờ, đối chiếu sao kê rồi bấm **Đã nhận tiền**. Giao dịch chuyển `Completed`, số dư ví tăng, yêu cầu chuyển `Paid`.
4. Trang Ví tự cập nhật khi yêu cầu được xác nhận.

### 1.2. Đặt sân bằng chuyển khoản QR

1. Người chơi chọn ca, bấm **Tiến hành Đặt sân**, chọn **Chuyển khoản VietQR**. Đơn tạo ở `Pending` (giữ chỗ 15 phút) và `PaymentOrder` (`Purpose = Booking`, số tiền = giá ca).
2. Trang hiển thị QR, mã và số tiền. Trang tự cập nhật khi đơn được xác nhận.
3. Chủ sân (hoặc Admin) mở **Đối soát chuyển khoản** (`/Provider/Payments`), đối chiếu sao kê, bấm **Đã nhận tiền**. Đơn chuyển `Confirmed`, yêu cầu chuyển `Paid`.
4. Nếu chủ sân bấm **Duyệt** trong `/Provider/Bookings` thì yêu cầu chuyển khoản cũng được đánh dấu `Paid`. Nếu hủy đơn thì yêu cầu chuyển `Expired`.
5. Quá 15 phút chưa xác nhận: job nền hủy đơn giữ chỗ và đánh dấu yêu cầu `Expired`.

Quyền xác nhận: nạp ví chỉ **Admin**; đơn đặt sân là **Admin** hoặc **Provider của đúng cơ sở**. Người chơi không xác nhận được.

### 1.3. Thanh toán đơn đặt sân bằng ví

1. Người chơi chọn ca trên ma trận, bấm **Tiến hành Đặt sân**, chọn phương thức **Ví CourtSide**.
2. Trang Checkout kiểm tra số dư (lấy từ `GET /api/wallet/me`). Nếu không đủ, không tạo đơn.
3. Đơn được tạo ở trạng thái `Pending` (`POST /api/bookings`), sau đó gọi `POST /api/wallet/pay-booking`.
4. Khi thanh toán thành công: số dư trừ đúng `TotalPrice`, tạo giao dịch `BookingPayment` (`ReferenceCode = BK_<bookingId>`) và yêu cầu thanh toán `PaymentOrder` ở trạng thái **`Held`** (tiền đã giữ). Đơn vẫn ở `Pending`, **chờ chủ sân duyệt**.
5. Chủ sân (hoặc Admin) mở **Duyệt đơn đặt sân** (`/Provider/Payments`):
   - **Duyệt đơn**: đơn chuyển `Confirmed`, yêu cầu chuyển `Paid`.
   - **Từ chối**: đơn chuyển `Cancelled`, yêu cầu chuyển `Expired`, số tiền hoàn đủ về ví (giao dịch `BookingRefund`).
   - Hủy đơn qua trang "Quản lý Đơn đặt" cũng hoàn tiền ví đúng một lần.
6. Đơn `Pending` đã có yêu cầu `Held` không bị job hết hạn hủy; người chơi không thể trả lần hai (API trả 400).
7. Nếu bước thanh toán lỗi (không đủ số dư, lỗi mạng), đơn vẫn ở `Pending` và không có tiền bị trừ.

### 1.4. Tiền mặt tại quầy

- Đơn tạo ở `Pending`, chủ sân xác nhận sau khi nhận tiền bằng nút **Duyệt** trong `/Provider/Bookings`.

1. Người chơi chọn ca trên ma trận, bấm **Tiến hành Đặt sân**, chọn phương thức **Ví CourtSide**.
2. Trang Checkout kiểm tra số dư (lấy từ `GET /api/wallet/me`). Nếu không đủ, không tạo đơn.
3. Đơn được tạo ở trạng thái `Pending` (`POST /api/bookings`), sau đó gọi `POST /api/wallet/pay-booking`.
4. Khi thanh toán thành công: số dư trừ đúng `TotalPrice` của đơn, tạo giao dịch `BookingPayment` (`ReferenceCode = BK_<bookingId>`), đơn chuyển sang `Confirmed`.
5. Nếu bước thanh toán lỗi, đơn vẫn ở `Pending` và người chơi thấy thông báo để thanh toán lại trong "Lịch đặt của tôi".

### 1.5. Hoàn tiền

- Hoàn tiền chỉ xảy ra khi **hủy đơn đang ở trạng thái `Confirmed`** và đơn đã được thanh toán bằng ví (có giao dịch `BK_<bookingId>` loại `BookingPayment`).
- Giao dịch hoàn có mã `REFUND_<bookingId>` loại `BookingRefund`. Mỗi đơn chỉ được hoàn **một lần**; nếu đã có `REFUND_<bookingId>` thì không hoàn thêm.
- Đơn chưa thanh toán bằng ví (QR, tiền mặt, hoặc Admin đặt `Confirmed` thủ công) khi bị hủy **không** được hoàn tiền.
- Người chơi chỉ hủy được đơn `Pending` của chính mình. Đơn `Confirmed` do Provider hoặc Admin hủy.

### 1.7. Doanh thu chủ sân và phí nền tảng

- Đơn thanh toán online (ví giữ tiền hoặc QR đã đối chiếu) khi được **duyệt** thì ghi doanh thu vào ví chủ sân: `giá sân − phí nền tảng`. Giao dịch `BookingIncome`, mã `INCOME_<bookingId>`.
- Phí nền tảng mặc định **5%**, cấu hình bằng `Payments:PlatformFeePercent` và do chủ sân chịu. Ví dụ đơn 90.000đ: phí 4.500đ, chủ sân nhận 85.500đ.
- Hủy đơn **đã duyệt**: thu hồi đúng khoản doanh thu đã ghi (`BookingIncomeReversal`, mã `REVERSE_<bookingId>`) và hoàn tiền cho người chơi. Số dư ví chủ sân có thể âm nếu đã dùng hết; hệ thống chưa có chức năng rút tiền.
- Đơn tiền mặt tại quầy không có yêu cầu thanh toán nên không ghi doanh thu qua ví.
- Ghi nhận chỉ xảy ra một lần cho mỗi đơn (chống trùng theo mã `INCOME_<bookingId>`).

### 1.6. Quy tắc trạng thái đơn

| Từ | Đến | Ai được thực hiện |
| :--- | :--- | :--- |
| `Pending` | `Confirmed`, `Cancelled` | Provider (cơ sở của mình), Admin |
| `Confirmed` | `Completed`, `Cancelled` | Provider (cơ sở của mình), Admin |
| `Pending` | `Cancelled` | Player (đơn của mình) |
| `Cancelled`, `Completed` | — | Không đổi được |

Trạng thái gửi lên phải thuộc `Pending`, `Confirmed`, `Cancelled`, `Completed`; giá trị khác bị từ chối.

---

## 2. Bảo mật đã áp dụng

| Hạng mục | Biện pháp | Ghi chú |
| :--- | :--- | :--- |
| Lưu token | JWT nằm trong cookie `courtside_token`: `HttpOnly`, `Secure`, `SameSite=Lax`, hết hạn 24 giờ | JavaScript không đọc được token. `localStorage` chỉ lưu thông tin hiển thị. |
| Đăng xuất | `POST /api/auth/logout` xóa cookie | Client xóa thông tin hiển thị và về trang đăng nhập. |
| Khóa ký JWT | `Jwt:Key` bắt buộc, ≥ 32 ký tự, qua biến môi trường `Jwt__Key` | API không khởi động nếu thiếu. |
| Mật khẩu demo | Lấy từ `Seed__AdminPassword`, `Seed__ProviderPassword`, `Seed__PlayerPassword`; nếu không đặt thì sinh ngẫu nhiên và in một lần khi seed | Không còn mật khẩu cố định trong mã nguồn. |
| Mật khẩu người dùng | Tối thiểu 8 ký tự, băm bằng BCrypt | |
| Chống CSRF | Mọi `POST`, `PUT`, `PATCH`, `DELETE` phải có header `X-Requested-With: CourtSideWeb` | Thiếu header trả `403`. |
| CORS | Chỉ origin trong `Cors:AllowedOrigins` (mặc định `http://localhost:3456`), có `AllowCredentials` | |
| Giới hạn tốc độ | 300 request/phút/IP toàn API; 10 lần/phút/IP cho đăng nhập | Lưu trong bộ nhớ, chỉ đúng với một instance. |
| Chống XSS | `escapeHtml()` cho mọi dữ liệu người dùng đưa vào `innerHTML`; tên dùng trong `onclick` chuyển sang `data-*` | Đã kiểm tra bằng tài khoản có tên chứa thẻ HTML. |
| Upload ảnh | Chỉ Provider và Admin; kiểm tra chữ ký file (JPEG, PNG, GIF, WEBP); tối đa 5MB mỗi ảnh; `RequestSizeLimit` | Chưa quét malware. |
| Swagger | Chỉ bật khi `ASPNETCORE_ENVIRONMENT=Development` | |
| Phân quyền | `[Authorize(Roles=...)]` ở controller và kiểm tra chủ sở hữu trong service | Xem `docs/02` mục 4.4. |

### 2.1. Giới hạn đã biết

- **CSRF** chỉ dựa vào header tùy chỉnh và CORS, chưa có anti-forgery token đầy đủ.
- **Giới hạn tốc độ** không phân tán; cần Redis hoặc gateway nếu triển khai nhiều instance.
- **Upload** không quét malware.
- **Đối soát nạp ví** thủ công, không có webhook ngân hàng.
- **Quên mật khẩu** chưa có chức năng.

---

## 3. Cấu hình khi chạy

Các giá trị bí mật đặt qua biến môi trường, không ghi vào `appsettings.json`:

```bash
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=BadmintonBookingDB;User Id=sa;Password=<mật khẩu>;TrustServerCertificate=True;MultipleActiveResultSets=true"
Jwt__Key="<chuỗi ngẫu nhiên >= 32 ký tự>"
Seed__AdminPassword="<tối thiểu 8 ký tự>"
Seed__ProviderPassword="<tối thiểu 8 ký tự>"
Seed__PlayerPassword="<tối thiểu 8 ký tự>"
```

Cấu hình không bí mật trong `appsettings.json`: `Cors:AllowedOrigins`, `VietQr` (ngân hàng và số tài khoản nhận tiền), `CloudflareR2` (bucket, domain công khai; khóa truy cập để trống hoặc đặt qua biến môi trường).

---

## 4. Kiểm thử đã thực hiện

Các luồng sau đã được kiểm tra end-to-end trên trình duyệt hoặc API:

- Nạp ví qua VietQR → Admin đối soát → số dư tăng.
- Thanh toán đơn bằng ví → đơn `Confirmed` → số dư giảm đúng.
- Hủy đơn đã thanh toán bằng ví → hoàn tiền đúng một lần; hủy đơn chưa thanh toán → không hoàn.
- Người chơi bị `403` khi xác nhận nạp ví và khi upload ảnh.
- Đăng xuất xóa cookie, API trả `401` sau đó.
- Thiếu header `X-Requested-With` bị `403`.
- Đăng nhập sai quá 10 lần/phút bị `429`.
- Tên người dùng chứa HTML hiển thị dạng chữ, không thực thi.

Chưa kiểm thử: hai phiên trình duyệt đồng thời (cần hai trình duyệt chạy được trên cùng `localhost`), và lỗi "bị đưa về trang đăng nhập" trên máy người dùng.
