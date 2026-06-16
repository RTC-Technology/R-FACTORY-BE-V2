using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/downtime-events")]
public sealed class DowntimeEventsController(IGenericRepo repo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(DowntimeEventRequest request)
    {
        var downtimeType = request.DowntimeType?.ToLowerInvariant();
        if (downtimeType is not ("planned" or "unplanned")) return BadRequest("DowntimeType must be planned or unplanned.");
        if (request.StartTime == default) return BadRequest("StartTime is required.");
        if (request.EndTime.HasValue && request.EndTime <= request.StartTime) return BadRequest("EndTime must be after StartTime.");
        if (downtimeType == "planned" && !request.PlannedDowntimeTypeId.HasValue) return BadRequest("PlannedDowntimeTypeId is required for planned downtime.");
        if (downtimeType == "unplanned" && !request.UnplannedDowntimeReasonId.HasValue) return BadRequest("UnplannedDowntimeReasonId is required for unplanned downtime.");

        var entity = new MachineDowntimeEvent
        {
            FactoryId = request.FactoryId,
            LineId = request.LineId,
            MachineId = request.MachineId,
            ProductionPlanId = request.ProductionPlanId,
            MachineStatusLogId = request.MachineStatusLogId,
            ShiftId = request.ShiftId,
            DowntimeType = downtimeType,
            PlannedDowntimeTypeId = downtimeType == "planned" ? request.PlannedDowntimeTypeId : null,
            UnplannedDowntimeReasonId = downtimeType == "unplanned" ? request.UnplannedDowntimeReasonId : null,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RootCause = request.RootCause,
            CorrectiveAction = request.CorrectiveAction,
            Note = request.Note,
            CreatedBy = request.CreatedBy,
            CreatedDate = DateTime.Now
        };

        return Ok(await repo.Insert(entity));
    }
}
