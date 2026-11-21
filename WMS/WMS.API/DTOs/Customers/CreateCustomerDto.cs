using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Customers;

public class CreateCustomerDto
{
    [Required]
    [MaxLength(40)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    public string? Address { get; set; }
}