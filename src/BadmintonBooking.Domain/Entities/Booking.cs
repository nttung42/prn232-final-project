using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class Booking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Player))]
    public int PlayerId { get; set; }

    [Required]
    [ForeignKey(nameof(Court))]
    public int CourtId { get; set; }

    [Required]
    [ForeignKey(nameof(TimeSlot))]
    public int TimeSlotId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PlayDate { get; set; }

    [Range(0, 10000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    [RegularExpression("^(Pending|Confirmed|Cancelled|Completed)$")]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? Player { get; set; }
    public virtual Court? Court { get; set; }
    public virtual TimeSlot? TimeSlot { get; set; }
}

