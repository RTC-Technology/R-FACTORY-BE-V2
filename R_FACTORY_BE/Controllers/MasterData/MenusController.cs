using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Common;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers.MasterData;

[ApiController]
[Route("api/menus")]
public class MenusController(IGenericRepo repo) : CrudControllerBase<Menu>(repo)
{
    [HttpGet]
    public override async Task<IActionResult> GetAll()
    {
        var rows = await Repo.GetAll<Menu>();
        return ApiResponseFactory.Success(rows);
    }

    [HttpGet("{id:int}")]
    public override async Task<IActionResult> GetById(int id)
    {
        var item = await Repo.GetById<Menu>(id);
        return item is null ? ApiResponseFactory.Fail("Menu not found", System.Net.HttpStatusCode.NotFound) : ApiResponseFactory.Success(item);
    }

    [HttpPost]
    public override async Task<IActionResult> Create([FromBody] Menu entity)
    {
        SetCreatedTimestamp(entity);
        var saved = await Repo.Insert(entity);
        return ApiResponseFactory.Success(saved, "Menu created successfully");
    }

    [HttpPut("{id:int}")]
    public override async Task<IActionResult> Update(int id, [FromBody] Menu entity)
    {
        SetId(entity, id);
        SetUpdatedTimestamp(entity);
        var saved = await Repo.Update(entity);
        return ApiResponseFactory.Success(saved, "Menu updated successfully");
    }

    [HttpDelete("{id:int}")]
    public override async Task<IActionResult> Delete(int id)
    {
        var deleted = await Repo.DeleteById<Menu>(id);
        return deleted ? ApiResponseFactory.Success<object>(null!, "Menu deleted successfully") : ApiResponseFactory.Fail("Menu not found", System.Net.HttpStatusCode.NotFound);
    }
}
