using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/machine-status")]
public sealed class MachineStatusController(IGenericRepo repo) : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "running", "stopped", "idle", "offline", "setup", "maintenance"
    };

    [HttpPost]
    public async Task<IActionResult> Create(MachineStatusRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status)) return BadRequest("Invalid machine status.");
        if (request.StartTime == default) return BadRequest("StartTime is required.");
        if (request.EndTime.HasValue && request.EndTime <= request.StartTime) return BadRequest("EndTime must be after StartTime.");

        var entity = new MachineStatusLog
        {
            FactoryId = request.FactoryId,
            LineId = request.LineId,
            MachineId = request.MachineId,
            ProductionPlanId = request.ProductionPlanId,
            ShiftId = request.ShiftId,
            Status = request.Status.ToLowerInvariant(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "api" : request.SourceType,
            RawData = request.RawData,
            CreatedDate = DateTime.Now
        };

        return Ok(await repo.Insert(entity));
    }
}
