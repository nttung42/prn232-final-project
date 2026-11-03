using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [Phone]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ (10 chữ số đầu 03, 05, 07, 08, 09)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Provider|Player)$", ErrorMessage = "Role phải là Admin, Provider hoặc Player")]
    public string Role { get; set; } = "Player";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Wallet? Wallet { get; set; }
    public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

