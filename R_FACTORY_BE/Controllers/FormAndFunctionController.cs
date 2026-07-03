using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Auth;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;
using System.Security.Claims;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/form-and-functions")]
[Authorize]
[HasPermission("FORM_FUNCTION_VIEW")]
public class FormAndFunctionController(IGenericRepo repo) : ControllerBase
{
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

    [HttpGet]
    [HasPermission("FORM_FUNCTION_VIEW")]
    public async Task<IActionResult> GetAll()
    {
        var items = await repo.GetAll<FormAndFunction>();
        return Ok(items.Where(x => !x.IsDeleted).ToList());
    }

    [HttpGet("{id}")]
    [HasPermission("FORM_FUNCTION_VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await repo.GetById<FormAndFunction>(id);
        if (item is null || item.IsDeleted) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [HasPermission("FORM_FUNCTION_EDIT")]
    public async Task<IActionResult> Create([FromBody] FormAndFunction model)
    {
        model.CreatedBy = GetUserId();
        model.CreatedDate = DateTime.Now;
        model.IsDeleted = false;
        var saved = await repo.Insert(model);
        return Ok(saved);
    }

    [HttpPut("{id}")]
    [HasPermission("FORM_FUNCTION_EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] FormAndFunction model)
    {
        var existing = await repo.GetById<FormAndFunction>(id);
        if (existing is null || existing.IsDeleted) return NotFound();

        existing.Code = model.Code;
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.ShiftKey = model.ShiftKey;
        existing.CtrlKey = model.CtrlKey;
        existing.AltKey = model.AltKey;
        existing.ShortcutKey = model.ShortcutKey;
        existing.FormAndFunctionGroupId = model.FormAndFunctionGroupId;
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
        var existing = await repo.GetById<FormAndFunction>(id);
        if (existing is null || existing.IsDeleted) return NotFound();

        existing.IsDeleted = true;
        existing.UpdatedBy = GetUserId();
        existing.UpdatedDate = DateTime.Now;
        await repo.Update(existing);

        return NoContent();
    }
}
