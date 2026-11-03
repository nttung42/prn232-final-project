using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerPhone { get; set; } = string.Empty;

    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;

    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;

    public int TimeSlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public DateTime PlayDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingDto
{
    [Required]
    public int CourtId { get; set; }

    [Required]
    public int TimeSlotId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PlayDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class UpdateBookingStatusDto
{
    [Required]
    [RegularExpression("^(Confirmed|Cancelled|Completed)$", ErrorMessage = "Trạng thái mới phải là Confirmed, Cancelled hoặc Completed")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }
}

