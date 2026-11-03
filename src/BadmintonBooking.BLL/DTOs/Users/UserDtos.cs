using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Provider|Player)$")]
    public string Role { get; set; } = "Player";

    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Provider|Player)$")]
    public string Role { get; set; } = "Player";

    public bool IsActive { get; set; } = true;
}

