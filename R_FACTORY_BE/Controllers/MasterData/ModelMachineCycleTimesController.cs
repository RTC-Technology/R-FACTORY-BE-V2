using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers.MasterData;

[ApiController]
[Route("api/model-machine-cycle-times")]
public class ModelMachineCycleTimesController(IGenericRepo repo) : CrudControllerBase<ModelMachineCycleTime>(repo) { }
