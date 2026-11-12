using BadmintonBooking.DAL;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Data;

public static class DbInitializer
{
    /// <summary>
    /// Seed dữ liệu demo cho dự án local: tài khoản, 18 cơ sở quanh Hà Nội, sân con và khung giờ.
    /// Không tạo đơn đặt sân nào (DB sạch để người dùng tự test).
    /// Mật khẩu demo lấy từ Seed:AdminPassword / ProviderPassword / PlayerPassword; nếu thiếu dùng mặc định.
    /// Chỉ seed khi bảng Users trống.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, Microsoft.Extensions.Configuration.IConfiguration? config = null)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        if (await context.Users.AnyAsync())
        {
            return; // Đã có dữ liệu
        }

        string Pwd(string key, string fallback)
        {
            var configured = config?[$"Seed:{key}"];
            return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        }

        var adminPwd = Pwd("AdminPassword", "Admin@123");
        var providerPwd = Pwd("ProviderPassword", "Provider@123");
        var playerPwd = Pwd("PlayerPassword", "Player@123");

        // 1. Tài khoản
        var admin = NewUser("Hệ Thống Admin", "admin@badminton.com", "0988888888", adminPwd, AppRoles.Admin);
        var providers = new[]
        {
            NewUser("Nguyễn Minh Tuấn", "provider1@badminton.com", "0977000001", providerPwd, AppRoles.Provider),
            NewUser("Trần Thị Hương", "provider2@badminton.com", "0977000002", providerPwd, AppRoles.Provider),
            NewUser("Lê Quốc Bảo", "provider3@badminton.com", "0977000003", providerPwd, AppRoles.Provider),
            NewUser("Phạm Đức Long", "provider4@badminton.com", "0977000004", providerPwd, AppRoles.Provider),
        };
        var players = new[]
        {
            NewUser("Lê Hoàng Player", "player1@badminton.com", "0912345678", playerPwd, AppRoles.Player),
            NewUser("Phạm Thúy Hằng", "player2@badminton.com", "0923456789", playerPwd, AppRoles.Player),
            NewUser("Đặng Minh Khoa", "player3@badminton.com", "0934567890", playerPwd, AppRoles.Player),
        };
        context.Users.Add(admin);
        context.Users.AddRange(providers);
        context.Users.AddRange(players);
        await context.SaveChangesAsync();

        // 2. Cơ sở. Hai nhóm: (a) địa chỉ lấy từ nguồn công khai (nhà thi đấu/trường/CLB đã nêu tên);
        // (b) cơ sở demo, tên và số điện thoại là giả, cần thay bằng thông tin thật trước khi dùng thương mại.
        var venues = VenueSeed.All;
        var facilities = new List<Facility>();
        for (int i = 0; i < venues.Count; i++)
        {
            var v = venues[i];
            facilities.Add(new Facility
            {
                ProviderId = providers[v.ProviderIndex].Id,
                Name = v.Name,
                Address = v.Address,
                District = v.District,
                Phone = v.Phone,
                Description = v.Description,
                OpenTime = new TimeSpan(v.OpenHour, 0, 0),
                CloseTime = new TimeSpan(v.CloseHour, 0, 0),
                Status = v.Pending ? FacilityStatuses.Pending : FacilityStatuses.Approved,
                CreatedAt = DateTime.UtcNow
            });
        }
        context.Facilities.AddRange(facilities);
        await context.SaveChangesAsync();

        // 3. Sân con: số sân và loại thảm theo từng cơ sở
        var courtTypes = new[] { CourtTypes.PVC, CourtTypes.PVC, CourtTypes.Wooden, CourtTypes.Acrylic };
        var courts = new List<Court>();
        for (int i = 0; i < facilities.Count; i++)
        {
            for (int c = 0; c < venues[i].CourtCount; c++)
            {
                courts.Add(new Court
                {
                    FacilityId = facilities[i].Id,
                    CourtName = $"Sân {c + 1}",
                    CourtType = courtTypes[c % courtTypes.Length],
                    Status = CourtStatuses.Active
                });
            }
        }
        context.Courts.AddRange(courts);
        await context.SaveChangesAsync();

        // 4. Khung giờ & bảng giá riêng cho từng sân (mỗi sân một bộ ca)
        var slotTemplates = new (string Name, int Start, int End, decimal Price)[]
        {
            ("Ca sáng sớm", 6, 8, 70000),
            ("Ca sáng", 8, 10, 90000),
            ("Ca chiều", 15, 17, 100000),
            ("Ca tối giờ vàng 1", 18, 20, 150000),
            ("Ca tối giờ vàng 2", 20, 22, 140000),
        };
        var slots = new List<TimeSlot>();
        foreach (var court in courts)
        {
            var facilityIndex = facilities.FindIndex(f => f.Id == court.FacilityId);
            var adjust = (facilityIndex % 3) * 10000m; // giá khác nhau giữa các cơ sở
            foreach (var t in slotTemplates)
            {
                slots.Add(new TimeSlot
                {
                    FacilityId = court.FacilityId,
                    CourtId = court.Id,
                    SlotName = $"{t.Name} ({t.Start:00}:00 - {t.End:00}:00)",
                    StartTime = new TimeSpan(t.Start, 0, 0),
                    EndTime = new TimeSpan(t.End, 0, 0),
                    Price = t.Price + adjust,
                    IsActive = true
                });
            }
        }
        context.TimeSlots.AddRange(slots);
        await context.SaveChangesAsync();
    }

    private static User NewUser(string fullName, string email, string phone, string password, string role) => new()
    {
        FullName = fullName,
        Email = email,
        PhoneNumber = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private sealed record Venue(
        string Name, string Address, string District, string Phone, string Description,
        int ProviderIndex, bool Pending, int CourtCount, int OpenHour, int CloseHour);

    private static class VenueSeed
    {
        public static readonly IReadOnlyList<Venue> All = new[]
        {
            // (a) Địa chỉ lấy từ nguồn công khai
            new Venue("Nhà thi đấu Phương Liệt", "41 Phương Liệt", "Thanh Xuân", "0977001001",
                "Nhà thi đấu có 4 sân cầu lông, trải thảm thi đấu, hệ thống đèn chiếu sáng đầy đủ.", 0, false, 4, 6, 22),
            new Venue("Nhà thi đấu Đại học Sư phạm Hà Nội", "136 Xuân Thủy", "Cầu Giấy", "0977001002",
                "Sân trong khuôn viên nhà thi đấu Đại học Sư phạm Hà Nội, phù hợp thi đấu và tập luyện.", 1, false, 3, 6, 22),
            new Venue("Sân cầu lông THCS Thanh Xuân Nam", "214 Nguyễn Xiển", "Thanh Xuân", "0977001003",
                "Sân cầu lông tại trường THCS Thanh Xuân Nam, giờ thuê ngoài giờ học.", 2, false, 2, 17, 22),
            new Venue("JQK Badminton", "Tân Triều, Thanh Liệt", "Thanh Trì", "0977001004",
                "Mặt sân thảm PVC chắc chắn, không trơn trượt. Địa chỉ cần xác nhận lại trước khi sử dụng.", 3, false, 3, 6, 22),

            // (b) Cơ sở demo (tên và số điện thoại giả)
            new Venue("Cầu lông Trung Hòa Sport (demo)", "Trung Hòa, Nhân Chính", "Cầu Giấy", "0977002001",
                "[Dữ liệu demo] Sân có máy lạnh, bãi đỗ xe.", 0, false, 3, 6, 22),
            new Venue("Cầu lông Láng Hạ Center (demo)", "Láng Hạ", "Đống Đa", "0977002002",
                "[Dữ liệu demo] Sân trong nhà, có phòng thay đồ.", 1, false, 3, 6, 23),
            new Venue("Cầu lông Kim Liên Arena (demo)", "Kim Liên", "Đống Đa", "0977002003",
                "[Dữ liệu demo] Sân gần ga Hà Nội, dịch vụ căng vợt.", 2, false, 2, 6, 22),
            new Venue("Cầu lông Đại La Star (demo)", "Đại La", "Hai Bà Trưng", "0977002004",
                "[Dữ liệu demo] Sân rộng, khu vực khán đài nhỏ.", 3, false, 2, 6, 22),
            new Venue("Cầu lông Bạch Mai Sport (demo)", "Bạch Mai", "Hai Bà Trưng", "0977002005",
                "[Dữ liệu demo] Gần các trường đại học, giá sinh viên.", 0, false, 2, 5, 22),
            new Venue("Cầu lông Hoàng Mai Plaza (demo)", "Đại Kim", "Hoàng Mai", "0977002006",
                "[Dữ liệu demo] Có sân VIP sàn gỗ.", 1, false, 3, 6, 23),
            new Venue("Cầu lông Yên Sở Club (demo)", "Yên Sở", "Hoàng Mai", "0977002007",
                "[Dữ liệu demo] Sân ngoài trời có mái che.", 2, false, 2, 6, 21),
            new Venue("Cầu lông Long Biên Riverside (demo)", "Gia Thụy", "Long Biên", "0977002008",
                "[Dữ liệu demo] Sân view sông, thoáng gió.", 3, false, 3, 6, 22),
            new Venue("Cầu lông Ngọc Lâm Arena (demo)", "Ngọc Lâm", "Long Biên", "0977002009",
                "[Dữ liệu demo] Bãi đỗ xe rộng.", 0, false, 2, 6, 22),
            new Venue("Cầu lông Tây Hồ Lakeside (demo)", "Quảng An", "Tây Hồ", "0977002010",
                "[Dữ liệu demo] Sân gần hồ Tây, có cà phê nghỉ chân.", 1, false, 2, 6, 23),
            new Venue("Cầu lông Nhật Tân Sport (demo)", "Nhật Tân", "Tây Hồ", "0977002011",
                "[Dữ liệu demo] Sân mới khai trương, thảm PVC cao cấp.", 2, false, 2, 6, 22),
            new Venue("Cầu lông Mỹ Đình Arena (demo)", "Mỹ Đình", "Nam Từ Liêm", "0977002012",
                "[Dữ liệu demo] Gần sân vận động Mỹ Đình, có khu vực ăn uống.", 3, false, 3, 6, 22),
            new Venue("Cầu lông Phú Diễn Star (demo)", "Phú Diễn", "Bắc Từ Liêm", "0977002013",
                "[Dữ liệu demo] Sân ở khu đô thị, nhiều chỗ đỗ xe.", 0, false, 2, 6, 22),
            new Venue("Cầu lông Định Công Central (demo)", "Định Công", "Hoàng Mai", "0977002014",
                "[Dữ liệu demo] Cơ sở đang chờ quản trị viên duyệt, để kiểm thử luồng duyệt cơ sở.", 1, true, 2, 6, 22),
        };
    }
}
