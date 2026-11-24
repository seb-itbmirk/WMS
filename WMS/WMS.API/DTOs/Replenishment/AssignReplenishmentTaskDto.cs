using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Replenishment;

public class AssignReplenishmentTaskDto
{
    [Range(1, long.MaxValue)]
    public long AssignedTo { get; set; }
}