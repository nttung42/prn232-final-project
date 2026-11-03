using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.Facilities;

public class FacilityDto
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> GalleryImages { get; set; } = new();
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public int TotalCourts { get; set; }
    public int TotalTimeSlots { get; set; }
}

public class CreateFacilityDto
{
    [Required(ErrorMessage = "Tên cơ sở không được để trống")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Tên cơ sở phải từ 5 đến 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ không được để trống")]
    [StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quận/Huyện không được để trống")]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public List<string> GalleryImages { get; set; } = new();

    [Required]
    public TimeSpan OpenTime { get; set; }

    [Required]
    public TimeSpan CloseTime { get; set; }
}

public class UpdateFacilityDto : CreateFacilityDto
{
}

public class UpdateFacilityStatusDto
{
    [Required]
    [RegularExpression("^(Approved|Rejected|Suspended)$", ErrorMessage = "Trạng thái phê duyệt phải là Approved, Rejected hoặc Suspended")]
    public string Status { get; set; } = string.Empty;
}
