# TÀI LIỆU ĐẶC TẢ KỸ THUẬT (TECHNICAL DEFINITION)
**Dự án:** Hệ thống Đặt sân Cầu lông Đa nhà cung cấp (Multi-Provider Badminton Court Booking System)  
**Môn học:** PRN232 - Building Cross-Platform Back-End Application With .NET

---

## 1. Kiến trúc Hệ thống & Công nghệ Sử dụng (System Architecture & Tech Stack)

### 1.1. Công nghệ sử dụng
* **Framework Backend:** ASP.NET Core Web API (.NET 8.0)
* **ORM & Database:** Entity Framework Core (Code First) + SQL Server
* **Truy vấn nâng cao:** `Microsoft.AspNetCore.OData` (v8.x)
* **Ánh xạ đối tượng:** `AutoMapper`
* **Xác thực & Phân quyền:** `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT Role-based Authorization: `Admin`, `Provider`, `Player`)
* **Định dạng dữ liệu (Content Negotiation):** JSON (mặc định), XML (`AddXmlSerializerFormatters()`), và Custom CSV Media Formatter (`TextOutputFormatter`).
* **Frontend (3 Sites gọi API bằng AJAX):** ASP.NET Core Razor Pages / MVC (hoặc HTML5 + Bootstrap 5 + jQuery AJAX / Fetch API) chia thành 3 khu vực: `/player`, `/provider`, `/admin`.

### 1.2. Sơ đồ Kiến trúc Tổng thể

```mermaid
flowchart LR
    subgraph Clients["Frontend Web Client (Gọi API bằng AJAX)"]
        P["Player Site\n(Tìm sân, Đặt lịch)"]
        V["Provider Site\n(Quản lý Cơ sở, Sân, Đơn)"]
        A["Admin Site\n(Quản lý User, Duyệt Cơ sở)"]
    end

    subgraph API["ASP.NET Core Web API"]
        Auth["JWT Auth Middleware\n(Admin / Provider / Player)"]
        CN["Content Negotiation\n(JSON / XML / CSV Formatter)"]
        OData["OData Query Layer\n($filter, $orderby, $top, $skip)"]
        Controllers["API Controllers\n(RESTful CRUD Endpoints)"]
        Mapper["AutoMapper\n(Entity <-> DTO)"]
    end

    subgraph Data["Data Access & Persistence"]
        Services["Repositories / Services\n(Business Logic)"]
        EF["EF Core DbContext\n(Code First + Data Annotations)"]
        DB[("SQL Server\nDatabase")]
    end

    Clients <-->|"HTTPS + AJAX (JSON/XML/CSV)\nHeader: Authorization Bearer <JWT>"| Auth
    Auth --> CN --> OData --> Controllers
    Controllers <--> Mapper
    Controllers --> Services --> EF --> DB
```

### 1.3. Cấu trúc Solution (Clean N-Tier Architecture)
```text
BadmintonBookingSystem.sln
├── src/
│   ├── BadmintonBooking.Domain/          # Entities, Enums, Data Annotations
│   ├── BadmintonBooking.DAL/             # AppDbContext, Migrations, Repositories
│   ├── BadmintonBooking.BLL/             # Services, DTOs, AutoMapper Profiles, Custom Validations
│   ├── BadmintonBooking.WebAPI/          # Controllers, OData Config, JWT Config, Custom CSV Formatter
│   └── BadmintonBooking.WebClient/       # Giao diện 3 Sites (Player, Provider, Admin) gọi API bằng AJAX
└── docs/
    ├── 01-business-definition.md
    └── 02-technical-definition.md
```

---

## 2. Thiết kế Cơ sở dữ liệu (EF Core Code First & ERD)

### 2.1. Sơ đồ Quan hệ Thực thể (ERD)

```mermaid
erDiagram
    USERS ||--o{ FACILITIES : "sở hữu (ProviderId)"
    USERS ||--o{ BOOKINGS : "đặt sân (PlayerId)"
    FACILITIES ||--o{ COURTS : "bao gồm (FacilityId)"
    FACILITIES ||--o{ TIMESLOTS : "cấu hình (FacilityId)"
    COURTS ||--o{ BOOKINGS : "được đặt (CourtId)"
    TIMESLOTS ||--o{ BOOKINGS : "theo ca (TimeSlotId)"

    USERS {
        int Id PK
        string FullName
        string Email UK
        string PhoneNumber
        string PasswordHash
        string Role "Admin | Provider | Player"
        bool IsActive
        datetime CreatedAt
    }

    FACILITIES {
        int Id PK
        int ProviderId FK
        string Name
        string Address
        string District
        string Phone
        string Description
        time OpenTime
        time CloseTime
        string Status "Pending | Approved | Rejected | Suspended"
        datetime CreatedAt
    }

    COURTS {
        int Id PK
        int FacilityId FK
        string CourtName
        string CourtType "PVC | Wooden | Acrylic"
        string Status "Active | Maintenance"
    }

    TIMESLOTS {
        int Id PK
        int FacilityId FK
        string SlotName
        time StartTime
        time EndTime
        decimal Price
        bool IsActive
    }

    BOOKINGS {
        int Id PK
        int PlayerId FK
        int CourtId FK
        int TimeSlotId FK
        date PlayDate
        decimal TotalPrice
        string Status "Pending | Confirmed | Cancelled | Completed"
        string Note
        datetime CreatedAt
    }
```

### 2.2. Thiết kế Entities & Data Annotations (Code First)

Sử dụng đầy đủ các `Data Annotations` trong namespace `System.ComponentModel.DataAnnotations` và `System.ComponentModel.DataAnnotations.Schema` theo đúng yêu cầu đề bài:

* **`User.cs`**
  * `[Key]`, `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` cho `Id`.
  * `[Required(ErrorMessage = "Họ tên không được để trống")]`, `[StringLength(100, MinimumLength = 2)]` cho `FullName`.
  * `[Required]`, `[EmailAddress]`, `[MaxLength(150)]` cho `Email`.
  * `[Required]`, `[Phone]`, `[RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$")]` cho `PhoneNumber`.
  * `[Required]`, `[RegularExpression("^(Admin|Provider|Player)$")]` cho `Role`.

* **`Facility.cs`**
  * `[Key]` cho `Id`.
  * `[Required]`, `[ForeignKey(nameof(Provider))]` cho `ProviderId`.
  * `[Required]`, `[StringLength(150, MinimumLength = 5)]` cho `Name`.
  * `[Required]`, `[StringLength(250)]` cho `Address`.
  * `[Required]`, `[StringLength(100)]` cho `District`.
  * `[Required]`, `[Phone]` cho `Phone`.

* **`Court.cs`**
  * `[Key]` cho `Id`.
  * `[Required]`, `[ForeignKey(nameof(Facility))]` cho `FacilityId`.
  * `[Required]`, `[StringLength(50)]` cho `CourtName`.
  * `[Required]`, `[RegularExpression("^(PVC|Wooden|Acrylic)$")]` cho `CourtType`.
  * `[Required]`, `[RegularExpression("^(Active|Maintenance)$")]` cho `Status`.

* **`TimeSlot.cs`**
  * `[Key]` cho `Id`.
  * `[Required]`, `[ForeignKey(nameof(Facility))]` cho `FacilityId`.
  * `[Required]`, `[StringLength(50)]` cho `SlotName`.
  * `[Required]`, `[DataType(DataType.Time)]` cho `StartTime` và `EndTime`.
  * `[Required]`, `[Range(10000, 5000000, ErrorMessage = "Giá ca phải từ 10.000đ đến 5.000.000đ")]`, `[Column(TypeName = "decimal(18,2)")]` cho `Price`.

* **`Booking.cs`**
  * `[Key]` cho `Id`.
  * `[Required]` cho `PlayerId`, `CourtId`, `TimeSlotId`.
  * `[Required]`, `[DataType(DataType.Date)]` cho `PlayDate`.
  * `[Range(0, 10000000)]`, `[Column(TypeName = "decimal(18,2)")]` cho `TotalPrice`.
  * `[MaxLength(500)]` cho `Note`.

* **Lưu ý cấu hình Fluent API trong `AppDbContext.OnModelCreating`:**
  * Thiết lập `DeleteBehavior.Restrict` cho các khóa ngoại dẫn tới `Booking` (`PlayerId`, `CourtId`, `TimeSlotId`) để tránh lỗi *Multiple Cascade Paths* trên SQL Server.
  * Tạo Unique Index chống đặt trùng lịch hoặc kiểm tra ở tầng Business Service.

---

## 3. Thiết kế RESTful Web API (Đáp ứng > 5 API chuẩn CRUD)

Mỗi Resource chính (`Facilities`, `Courts`, `TimeSlots`, `Bookings`, `Users`) đều cung cấp đầy đủ 5 phương thức chuẩn: **`GET all`**, **`GET by id`**, **`POST`**, **`PUT`**, **`DELETE`**.

### 3.1. Nhóm API Xác thực (`AuthController`)
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | Đăng ký tài khoản (`Player` hoặc `Provider`). |
| `POST` | `/api/auth/login` | Public | Đăng nhập, trả về `AccessToken` (JWT), `Role`, `UserId`, `FullName`. |
| `GET` | `/api/auth/me` | Authenticated | Lấy thông tin người dùng hiện tại từ JWT Claims. |

### 3.2. Nhóm API Cơ sở cầu lông (`FacilitiesController`) — *Hỗ trợ OData*
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/facilities` | Public / All | **[GET ALL + OData]** Lấy danh sách cơ sở (`$filter`, `$orderby`, `$top`, `$skip`). |
| `GET` | `/api/facilities/{id}` | Public / All | **[GET BY ID]** Lấy chi tiết 1 cơ sở kèm danh sách sân & khung giờ. |
| `POST` | `/api/facilities` | `Provider` | **[POST]** Provider tạo cơ sở mới (`Status = Pending`). |
| `PUT` | `/api/facilities/{id}` | `Provider`, `Admin` | **[PUT]** Provider cập nhật thông tin cơ sở của mình. |
| `PATCH` | `/api/facilities/{id}/status` | `Admin` | Admin duyệt (`Approved`) hoặc đình chỉ (`Suspended`) cơ sở. |
| `DELETE` | `/api/facilities/{id}` | `Provider`, `Admin` | **[DELETE]** Xóa cơ sở (nếu chưa phát sinh đơn đặt đang hoạt động). |

### 3.3. Nhóm API Sân con (`CourtsController`) — *Hỗ trợ OData*
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/courts` | Public / All | **[GET ALL + OData]** Lấy danh sách sân (lọc theo `FacilityId`, `Status`, `CourtType`). |
| `GET` | `/api/courts/{id}` | Public / All | **[GET BY ID]** Xem chi tiết 1 sân cầu lông. |
| `POST` | `/api/courts` | `Provider` | **[POST]** Thêm sân con mới vào cơ sở của Provider. |
| `PUT` | `/api/courts/{id}` | `Provider` | **[PUT]** Cập nhật tên sân, loại sân, trạng thái bảo trì. |
| `DELETE` | `/api/courts/{id}` | `Provider` | **[DELETE]** Xóa sân con. |

### 3.4. Nhóm API Khung giờ & Giá (`TimeSlotsController`) — *Hỗ trợ OData*
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/timeslots` | Public / All | **[GET ALL + OData]** Lấy danh sách khung giờ (lọc theo `FacilityId`, khoảng `Price`). |
| `GET` | `/api/timeslots/{id}` | Public / All | **[GET BY ID]** Lấy chi tiết 1 khung giờ. |
| `GET` | `/api/timeslots/available` | Public / All | Kiểm tra các khung giờ còn trống theo `facilityId`, `courtId`, `playDate`. |
| `POST` | `/api/timeslots` | `Provider` | **[POST]** Tạo khung giờ & giá mới cho cơ sở. |
| `PUT` | `/api/timeslots/{id}` | `Provider` | **[PUT]** Cập nhật giờ bắt đầu/kết thúc hoặc giá ca. |
| `DELETE` | `/api/timeslots/{id}` | `Provider` | **[DELETE]** Xóa khung giờ. |

### 3.5. Nhóm API Đặt sân (`BookingsController`) — *Hỗ trợ OData & Content Negotiation (JSON/XML/CSV)*
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/bookings` | `Admin`, `Provider`, `Player` | **[GET ALL + OData + CSV/XML/JSON]** Lấy danh sách đơn (Player thấy đơn của mình; Provider thấy đơn tại cơ sở của mình; Admin thấy toàn bộ). |
| `GET` | `/api/bookings/{id}` | `Admin`, `Provider`, `Player` | **[GET BY ID]** Xem chi tiết 1 đơn đặt sân. |
| `POST` | `/api/bookings` | `Player` | **[POST]** Đặt sân mới (Validate trùng lịch, tính `TotalPrice`). |
| `PUT` | `/api/bookings/{id}` | `Provider`, `Player` | **[PUT]** Cập nhật ghi chú hoặc đổi trạng thái đơn (`Confirmed`, `Cancelled`, `Completed`). |
| `DELETE` | `/api/bookings/{id}` | `Admin`, `Player` | **[DELETE]** Xóa đơn đặt sân (chỉ cho phép khi đơn ở trạng thái `Pending` hoặc `Cancelled`). |

### 3.6. Nhóm API Quản trị Người dùng (`UsersController`) — *Hỗ trợ OData*
| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/users` | `Admin` | **[GET ALL + OData]** Lấy danh sách người dùng toàn hệ thống. |
| `GET` | `/api/users/{id}` | `Admin` | **[GET BY ID]** Xem chi tiết tài khoản. |
| `POST` | `/api/users` | `Admin` | **[POST]** Admin tạo tài khoản mới. |
| `PUT` | `/api/users/{id}` | `Admin` | **[PUT]** Cập nhật Role hoặc trạng thái `IsActive`. |
| `DELETE` | `/api/users/{id}` | `Admin` | **[DELETE]** Xóa/vô hiệu hóa tài khoản. |

---

## 4. Đặc tả Triển khai Các Yêu cầu Kỹ thuật Bắt buộc

### 4.1. Tích hợp OData (`Microsoft.AspNetCore.OData`)
* Cấu hình trong `Program.cs`:
  ```csharp
  builder.Services.AddControllers()
      .AddOData(options => options
          .Select()
          .Filter()
          .OrderBy()
          .Expand()
          .Count()
          .SetMaxTop(100));
  ```
* Gắn `[EnableQuery]` lên các action `GET` của `FacilitiesController`, `CourtsController`, `TimeSlotsController`, `BookingsController`, `UsersController`.
* **Ví dụ các truy vấn OData thực tế trên Frontend AJAX:**
  * Lọc cơ sở đã duyệt tại Quận Cầu Giấy:
    `GET /api/facilities?$filter=Status eq 'Approved' and contains(District, 'Cầu Giấy')&$orderby=Name asc`
  * Lọc khung giờ có giá dưới 120.000đ:
    `GET /api/timeslots?$filter=FacilityId eq 2 and Price le 120000&$orderby=StartTime asc`
  * Lọc đơn đặt sân của Provider theo ngày chơi:
    `GET /api/bookings?$filter=PlayDate eq 2026-11-10 and Status eq 'Confirmed'&$orderby=CreatedAt desc`

### 4.2. Tích hợp AutoMapper
* Tạo lớp `MappingProfile : Profile` để ánh xạ 2 chiều giữa Domain Entities và DTOs, tuyệt đối không trả trực tiếp Entity ra API (tránh lỗi Circular Reference và lộ `PasswordHash`):
  * `User` $\leftrightarrow$ `UserDto`, `RegisterRequestDto`, `UpdateUserDto`
  * `Facility` $\leftrightarrow$ `FacilityDto` (map thêm `ProviderName` từ `Facility.Provider.FullName`), `CreateFacilityDto`, `UpdateFacilityDto`
  * `Court` $\leftrightarrow$ `CourtDto`, `SaveCourtDto`
  * `TimeSlot` $\leftrightarrow$ `TimeSlotDto`, `SaveTimeSlotDto`
  * `Booking` $\leftrightarrow$ `BookingDto` (map phẳng `FacilityName`, `CourtName`, `SlotName`, `StartTime`, `EndTime`, `PlayerName`, `PlayerPhone`), `CreateBookingDto`, `UpdateBookingStatusDto`

### 4.3. Media Formatter & Content Negotiation
* Cấu hình `Program.cs` để hỗ trợ Content Negotiation dựa trên HTTP Header `Accept`:
  1. **JSON (`application/json`):** Mặc định.
  2. **XML (`application/xml`):** Thêm `.AddXmlSerializerFormatters()`.
  3. **Custom CSV Formatter (`text/csv`):** Xây dựng lớp `CsvOutputFormatter : TextOutputFormatter` kế thừa từ `Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter`.
* **Ứng dụng thực tế:**
  * Trên **Provider Site** và **Admin Site**, tại màn hình Quản lý Đơn đặt sân (`Bookings`), có 3 nút Xuất dữ liệu:
    * **Export JSON** (Gửi AJAX với header `Accept: application/json`)
    * **Export XML** (Gửi AJAX với header `Accept: application/xml`)
    * **Export CSV** (Gửi AJAX với header `Accept: text/csv`) $\rightarrow$ Trình duyệt tải về file `bookings.csv` để mở bằng Excel.

### 4.4. Xác thực & Phân quyền bằng JWT (Authentication & Authorization)
* Khi đăng nhập thành công (`POST /api/auth/login`), Server tạo JWT Token chứa các Claims:
  * `ClaimTypes.NameIdentifier`: `User.Id`
  * `ClaimTypes.Email`: `User.Email`
  * `ClaimTypes.Name`: `User.FullName`
  * `ClaimTypes.Role`: `User.Role` (`Admin`, `Provider`, hoặc `Player`)
* Bảo vệ Endpoints bằng Attribute:
  * `[Authorize(Roles = "Admin")]` cho các tác vụ duyệt cơ sở, quản lý users.
  * `[Authorize(Roles = "Provider")]` cho các tác vụ thêm/sửa/xóa cơ sở, sân, khung giờ.
  * `[Authorize(Roles = "Player")]` cho tác vụ đặt sân.
  * `[Authorize(Roles = "Admin,Provider,Player")]` cho tác vụ xem/cập nhật đơn đặt sân (kèm kiểm tra `UserId` trong Controller/Service để đảm bảo đúng chủ sở hữu).

---

## 5. Thiết kế Giao diện 3 Sites Gọi API Bằng AJAX

Client được tổ chức thành 3 phân hệ rõ ràng, sử dụng `jQuery AJAX` / `Fetch API` đính kèm `Authorization: Bearer <token>` từ `localStorage`:

### 5.1. Danh sách Màn hình theo 3 Sites
1. **Common / Auth Pages:**
   * `/Auth/Login`: Đăng nhập, nhận JWT lưu vào `localStorage`, tự động điều hướng về đúng Site theo `Role` (`Admin` $\rightarrow$ `/Admin`, `Provider` $\rightarrow$ `/Provider`, `Player` $\rightarrow$ `/Player`).
   * `/Auth/Register`: Đăng ký tài khoản mới (chọn đăng ký làm `Player` hoặc `Provider`).
2. **Player Site (`/Player/...`):**
   * `/Player/Index`: Trang chủ tìm kiếm & lọc cơ sở cầu lông (gọi AJAX OData `/api/facilities?$filter=...`).
   * `/Player/FacilityDetail?id={id}`: Xem chi tiết cơ sở, chọn ngày `PlayDate`, xem lưới Sân (`Court`) $\times$ Khung giờ (`TimeSlot`) và bấm **Đặt sân** (`POST /api/bookings` qua AJAX).
   * `/Player/MyBookings`: Xem danh sách lịch sử đặt sân của bản thân và nút **Hủy đặt sân** (`PUT /api/bookings/{id}` qua AJAX).
3. **Provider Site (`/Provider/...`):**
   * `/Provider/Facilities`: Quản lý danh sách cơ sở cầu lông của mình (Thêm/Sửa/Xóa bằng Modal + AJAX CRUD).
   * `/Provider/CourtsAndSlots?facilityId={id}`: Quản lý Sân con (`Courts`) và Khung giờ giá (`TimeSlots`) trên cùng giao diện trực quan bằng AJAX.
   * `/Provider/Bookings`: Quản lý đơn đặt sân của khách hàng, lọc theo ngày/trạng thái bằng OData, bấm Duyệt/Hủy/Hoàn thành đơn và **Xuất báo cáo CSV/XML/JSON** (Demo Content Negotiation).
4. **Admin Site (`/Admin/...`):**
   * `/Admin/Dashboard`: Thống kê tổng quan toàn sàn.
   * `/Admin/Facilities`: Kiểm duyệt các cơ sở cầu lông (`Approve` / `Reject` / `Suspend`) bằng AJAX.
   * `/Admin/Users`: Quản lý người dùng toàn hệ thống (Lọc OData, Thêm/Sửa/Khóa tài khoản bằng AJAX).

### 5.2. Chuẩn hóa Hàm Gọi AJAX Chung (`site-api.js`)
```javascript
function callApi(endpoint, method = "GET", data = null, acceptHeader = "application/json") {
    const token = localStorage.getItem("jwt_token");
    return $.ajax({
        url: `${API_BASE_URL}${endpoint}`,
        type: method,
        contentType: "application/json",
        headers: {
            "Accept": acceptHeader,
            ...(token ? { "Authorization": `Bearer ${token}` } : {})
        },
        data: data ? JSON.stringify(data) : null
    });
}
```

