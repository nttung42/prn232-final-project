using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.TimeSlots;

public class TimeSlotDto
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class SaveTimeSlotDto
{
    [Required]
    public int FacilityId { get; set; }

    [Required(ErrorMessage = "Tên khung giờ không được để trống")]
    [StringLength(50)]
    public string SlotName { get; set; } = string.Empty;

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Range(10000, 5000000, ErrorMessage = "Giá ca phải từ 10.000đ đến 5.000.000đ")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AvailableTimeSlotDto
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public int? ExistingBookingId { get; set; }
}

