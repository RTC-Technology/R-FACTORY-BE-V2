using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Services;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/oee")]
public sealed class OeeController(IOeeCalculationService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] OeeFilterDto filter) => Ok(await service.GetSummary(filter));

    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] OeeFilterDto filter) => Ok(await service.GetTimeline(filter));

    [HttpGet("layout")]
    public async Task<IActionResult> Layout([FromQuery] OeeFilterDto filter) => Ok(await service.GetLayout(filter));

    [HttpGet("machine/{machineId:int}")]
    public async Task<IActionResult> Machine(int machineId, [FromQuery] OeeFilterDto filter)
    {
        var detail = await service.GetMachineDetail(machineId, filter);
        return detail is null ? NotFound() : Ok(detail);
    }
}
