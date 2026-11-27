
using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Suppliers
{
    public class CreateSupplierDto
    {
        [Required]
        [StringLength(50)]
        public string SupplierCode { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string SupplierName { get; set; } = null!;

        [StringLength(150)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }
    }
}
