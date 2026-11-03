using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class Facility
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Provider))]
    public int ProviderId { get; set; }

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

    public string? GalleryImagesJson { get; set; }

    [NotMapped]
    public List<string> GalleryImages
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GalleryImagesJson)) return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(GalleryImagesJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set => GalleryImagesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan OpenTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan CloseTime { get; set; }

    [Required]
    [RegularExpression("^(Pending|Approved|Rejected|Suspended)$")]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? Provider { get; set; }
    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();
    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
}

