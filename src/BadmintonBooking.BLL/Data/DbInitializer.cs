using BadmintonBooking.DAL;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Automatically apply any pending migrations
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        if (await context.Users.AnyAsync())
        {
            return; // Data already seeded
        }

        // 1. Seed Users
        var admin = new User
        {
            FullName = "Hệ Thống Admin",
            Email = "admin@badminton.com",
            PhoneNumber = "0988888888",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = AppRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var provider1 = new User
        {
            FullName = "Nguyễn Văn Chủ Sân",
            Email = "provider1@badminton.com",
            PhoneNumber = "0977777777",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider@123"),
            Role = AppRoles.Provider,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var provider2 = new User
        {
            FullName = "Trần Văn Quản Lý",
            Email = "provider2@badminton.com",
            PhoneNumber = "0966666666",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider@123"),
            Role = AppRoles.Provider,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var player1 = new User
        {
            FullName = "Lê Hoàng Player",
            Email = "player1@badminton.com",
            PhoneNumber = "0912345678",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Player@123"),
            Role = AppRoles.Player,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var player2 = new User
        {
            FullName = "Phạm Thúy Hằng",
            Email = "player2@badminton.com",
            PhoneNumber = "0923456789",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Player@123"),
            Role = AppRoles.Player,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin, provider1, provider2, player1, player2);
        await context.SaveChangesAsync();

        // 2. Seed Facilities
        var facility1 = new Facility
        {
            ProviderId = provider1.Id,
            Name = "Cụm Sân Cầu Lông Cầu Giấy Pro",
            Address = "Số 10 Dịch Vọng Hậu",
            District = "Cầu Giấy",
            Phone = "0977777777",
            Description = "Cụm sân tiêu chuẩn thi đấu quốc tế, thảm Yonex cao cấp 5 sao, máy lạnh và khán đài.",
            OpenTime = new TimeSpan(6, 0, 0),
            CloseTime = new TimeSpan(23, 0, 0),
            Status = FacilityStatuses.Approved,
            CreatedAt = DateTime.UtcNow
        };

        var facility2 = new Facility
        {
            ProviderId = provider1.Id,
            Name = "Sân Cầu Lông Thanh Xuân Star",
            Address = "Ngõ 190 Nguyễn Trãi",
            District = "Thanh Xuân",
            Phone = "0977777778",
            Description = "Sân rộng rãi thoáng mát, có bãi đậu xe ô tô, dịch vụ căng vợt và nước uống giải khát.",
            OpenTime = new TimeSpan(5, 30, 0),
            CloseTime = new TimeSpan(22, 30, 0),
            Status = FacilityStatuses.Approved,
            CreatedAt = DateTime.UtcNow
        };

        var facility3 = new Facility
        {
            ProviderId = provider2.Id,
            Name = "Badminton Arena Nam Từ Liêm",
            Address = "Đường Lê Đức Thọ, Mỹ Đình",
            District = "Nam Từ Liêm",
            Phone = "0966666666",
            Description = "Khu phức hợp thể thao hiện đại, hệ thống ánh sáng chống lóa tiêu chuẩn BWF.",
            OpenTime = new TimeSpan(6, 0, 0),
            CloseTime = new TimeSpan(22, 0, 0),
            Status = FacilityStatuses.Approved,
            CreatedAt = DateTime.UtcNow
        };

        var facility4 = new Facility
        {
            ProviderId = provider2.Id,
            Name = "Sân Cầu Lông Đống Đa Mới",
            Address = "Chùa Bộc, Trung Tự",
            District = "Đống Đa",
            Phone = "0966666667",
            Description = "Cơ sở mới hoàn thiện đang đăng ký kiểm duyệt.",
            OpenTime = new TimeSpan(6, 0, 0),
            CloseTime = new TimeSpan(22, 0, 0),
            Status = FacilityStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.Facilities.AddRange(facility1, facility2, facility3, facility4);
        await context.SaveChangesAsync();

        // 3. Seed Courts
        var c1 = new Court { FacilityId = facility1.Id, CourtName = "Sân 1 - Thảm PVC Yonex", CourtType = CourtTypes.PVC, Status = CourtStatuses.Active };
        var c2 = new Court { FacilityId = facility1.Id, CourtName = "Sân 2 - Thảm PVC Victor", CourtType = CourtTypes.PVC, Status = CourtStatuses.Active };
        var c3 = new Court { FacilityId = facility1.Id, CourtName = "Sân 3 VIP - Sàn Gỗ Thi Đấu", CourtType = CourtTypes.Wooden, Status = CourtStatuses.Active };

        var c4 = new Court { FacilityId = facility2.Id, CourtName = "Sân Star A", CourtType = CourtTypes.PVC, Status = CourtStatuses.Active };
        var c5 = new Court { FacilityId = facility2.Id, CourtName = "Sân Star B", CourtType = CourtTypes.Acrylic, Status = CourtStatuses.Active };

        var c6 = new Court { FacilityId = facility3.Id, CourtName = "Sân Arena 1", CourtType = CourtTypes.PVC, Status = CourtStatuses.Active };
        var c7 = new Court { FacilityId = facility3.Id, CourtName = "Sân Arena 2", CourtType = CourtTypes.PVC, Status = CourtStatuses.Active };

        context.Courts.AddRange(c1, c2, c3, c4, c5, c6, c7);
        await context.SaveChangesAsync();

        // 4. Seed TimeSlots
        var ts1 = new TimeSlot { FacilityId = facility1.Id, SlotName = "Ca sáng sớm (06:00 - 08:00)", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(8, 0, 0), Price = 70000, IsActive = true };
        var ts2 = new TimeSlot { FacilityId = facility1.Id, SlotName = "Ca sáng (08:00 - 10:00)", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(10, 0, 0), Price = 90000, IsActive = true };
        var ts3 = new TimeSlot { FacilityId = facility1.Id, SlotName = "Ca chiều (15:00 - 17:00)", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(17, 0, 0), Price = 100000, IsActive = true };
        var ts4 = new TimeSlot { FacilityId = facility1.Id, SlotName = "Ca tối giờ vàng 1 (18:00 - 20:00)", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(20, 0, 0), Price = 150000, IsActive = true };
        var ts5 = new TimeSlot { FacilityId = facility1.Id, SlotName = "Ca tối giờ vàng 2 (20:00 - 22:00)", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(22, 0, 0), Price = 140000, IsActive = true };

        var ts6 = new TimeSlot { FacilityId = facility2.Id, SlotName = "Ca sáng (06:00 - 08:00)", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(8, 0, 0), Price = 80000, IsActive = true };
        var ts7 = new TimeSlot { FacilityId = facility2.Id, SlotName = "Ca tối (18:00 - 20:00)", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(20, 0, 0), Price = 140000, IsActive = true };

        var ts8 = new TimeSlot { FacilityId = facility3.Id, SlotName = "Ca sáng (06:00 - 08:00)", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(8, 0, 0), Price = 75000, IsActive = true };
        var ts9 = new TimeSlot { FacilityId = facility3.Id, SlotName = "Ca tối (18:00 - 20:00)", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(20, 0, 0), Price = 135000, IsActive = true };

        context.TimeSlots.AddRange(ts1, ts2, ts3, ts4, ts5, ts6, ts7, ts8, ts9);
        await context.SaveChangesAsync();

        // 5. Seed Bookings
        var today = DateTime.UtcNow.Date;
        var b1 = new Booking
        {
            PlayerId = player1.Id,
            CourtId = c1.Id,
            TimeSlotId = ts4.Id,
            PlayDate = today,
            TotalPrice = ts4.Price,
            Status = BookingStatuses.Confirmed,
            Note = "Đặt ca tối thi đấu giao lưu CLB",
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        };

        var b2 = new Booking
        {
            PlayerId = player2.Id,
            CourtId = c2.Id,
            TimeSlotId = ts5.Id,
            PlayDate = today.AddDays(1),
            TotalPrice = ts5.Price,
            Status = BookingStatuses.Pending,
            Note = "Đặt trước tập huấn",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        context.Bookings.AddRange(b1, b2);
        await context.SaveChangesAsync();
    }
}

