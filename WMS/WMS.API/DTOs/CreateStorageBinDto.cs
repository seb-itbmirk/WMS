
using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class CreateStorageBinDto
{
    [Required]
    public string BinCode { get; set; } = string.Empty;

    [Required]
    public string BinType { get; set; } = string.Empty;
}
