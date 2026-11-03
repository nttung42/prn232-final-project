using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.Courts;

public class CourtDto
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public string CourtType { get; set; } = "PVC";
    public string Status { get; set; } = "Active";
}

public class SaveCourtDto
{
    [Required]
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
}

