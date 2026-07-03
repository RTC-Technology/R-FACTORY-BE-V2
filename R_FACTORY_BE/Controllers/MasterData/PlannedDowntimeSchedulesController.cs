using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers.MasterData;

[ApiController]
[Route("api/planned-downtime-schedules")]
public class PlannedDowntimeSchedulesController(IGenericRepo repo) : CrudControllerBase<PlannedDowntimeSchedule>(repo) { }
