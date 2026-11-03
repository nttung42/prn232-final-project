using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class TimeSlot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Facility))]
    public int FacilityId { get; set; }

    [Required(ErrorMessage = "Tên khung giờ không được để trống")]
    [StringLength(50)]
    public string SlotName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Range(10000, 5000000, ErrorMessage = "Giá ca phải từ 10.000đ đến 5.000.000đ")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Facility? Facility { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

