using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Auth;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;
using System.Security.Claims;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/{controller}")]
[Authorize]
[HasPermission("FORM_FUNCTION_VIEW")]
public class FormAndFunctionGroupController(IGenericRepo repo) : ControllerBase
{
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

    [HttpGet]
    [HasPermission("FORM_FUNCTION_VIEW")]
    public async Task<IActionResult> GetAll()
    {
        var items = await repo.GetAll<FormAndFunctionGroup>();
        return Ok(items.Where(x => !x.IsDeleted).ToList());
    }

    [HttpGet("{id}")]
    [HasPermission("FORM_FUNCTION_VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await repo.GetById<FormAndFunctionGroup>(id);
        if (item is null || item.IsDeleted) return NotFound();
        return Ok(item);
    }

    [HttpGet("{id}/functions")]
    [HasPermission("FORM_FUNCTION_VIEW")]
    public async Task<IActionResult> GetFunctionsByGroup(int id)
    {
        var group = await repo.GetById<FormAndFunctionGroup>(id);
        if (group is null || group.IsDeleted) return NotFound("Group not found");

        var functions = await repo.GetAll<FormAndFunction>();
        return Ok(functions.Where(x => x.FormAndFunctionGroupId == id && !x.IsDeleted).ToList());
    }

    [HttpPost]
    [HasPermission("FORM_FUNCTION_EDIT")]
    public async Task<IActionResult> Create([FromBody] FormAndFunctionGroup model)
    {
        model.CreatedBy = GetUserId();
        model.CreatedDate = DateTime.Now;
        model.IsDeleted = false;
        var saved = await repo.Insert(model);
        return Ok(saved);
    }

    [HttpPut("{id}")]
    [HasPermission("FORM_FUNCTION_EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] FormAndFunctionGroup model)
    {
        var existing = await repo.GetById<FormAndFunctionGroup>(id);
        if (existing is null || existing.IsDeleted) return NotFound();

        existing.Code = model.Code;
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.ParentId = model.ParentId;
        existing.IsHide = model.IsHide;
        existing.UpdatedBy = GetUserId();
        existing.UpdatedDate = DateTime.Now;

        await repo.Update(existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [HasPermission("FORM_FUNCTION_DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await repo.GetById<FormAndFunctionGroup>(id);
        if (existing is null || existing.IsDeleted) return NotFound();

        existing.IsDeleted = true;
        existing.UpdatedBy = GetUserId();
        existing.UpdatedDate = DateTime.Now;
        await repo.Update(existing);

        var functions = await repo.GetAll<FormAndFunction>();
        foreach (var func in functions.Where(x => x.FormAndFunctionGroupId == id && !x.IsDeleted))
        {
            func.IsDeleted = true;
            func.UpdatedBy = GetUserId();
            func.UpdatedDate = DateTime.Now;
            await repo.Update(func);
        }

        return NoContent();
    }
}
