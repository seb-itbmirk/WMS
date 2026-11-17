using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class UpdateItemCategoryDto
{
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
