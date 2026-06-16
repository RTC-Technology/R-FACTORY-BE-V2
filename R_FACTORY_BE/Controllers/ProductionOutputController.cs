using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/production-output")]
public sealed class ProductionOutputController(IGenericRepo repo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(ProductionOutputRequest request)
    {
        if (request.GoodQty < 0 || request.NgQty < 0) return BadRequest("Quantities must be greater than or equal to zero.");
        if (request.LogTime == default) return BadRequest("LogTime is required.");

        var entity = new ProductionOutputLog
        {
            FactoryId = request.FactoryId,
            LineId = request.LineId,
            MachineId = request.MachineId,
            ModelId = request.ModelId,
            ProductionPlanId = request.ProductionPlanId,
            ShiftId = request.ShiftId,
            GoodQty = request.GoodQty,
            NgQty = request.NgQty,
            LogTime = request.LogTime,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "api" : request.SourceType,
            RawData = request.RawData,
            CreatedBy = request.CreatedBy,
            CreatedDate = DateTime.Now
        };

        return Ok(await repo.Insert(entity));
    }
}
