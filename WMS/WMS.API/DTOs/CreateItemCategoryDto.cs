using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class CreateItemCategoryDto
{
    [Required]
    [StringLength(30)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
