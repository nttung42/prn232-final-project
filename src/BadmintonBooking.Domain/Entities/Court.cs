using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class Court
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Facility))]
    public int FacilityId { get; set; }

    [Required(ErrorMessage = "Tên sân không được để trống")]
    [StringLength(50)]
    public string CourtName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(PVC|Wooden|Acrylic)$", ErrorMessage = "Loại sân phải là PVC, Wooden hoặc Acrylic")]
    public string CourtType { get; set; } = "PVC";

    [Required]
    [RegularExpression("^(Active|Maintenance)$", ErrorMessage = "Trạng thái sân phải là Active hoặc Maintenance")]
    public string Status { get; set; } = "Active";

    // Navigation properties
    public virtual Facility? Facility { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

