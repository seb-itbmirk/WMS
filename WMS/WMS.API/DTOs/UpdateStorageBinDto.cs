using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class UpdateStorageBinDto
{
    [Required]
    public string BinType { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
