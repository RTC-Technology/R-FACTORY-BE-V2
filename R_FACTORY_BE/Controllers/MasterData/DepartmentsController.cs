using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers.MasterData;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : CrudControllerBase<Department>
{
    public DepartmentsController(IGenericRepo repo) : base(repo) { }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var all = await Repo.GetAll<Department>();
        var ordered = all.OrderBy(d => d.ParentId ?? 0).ThenBy(d => d.DepartmentCode).ToList();
        var allNodes = ordered.Select(d => new DepartmentNode
        {
            Id = d.Id,
            DepartmentCode = d.DepartmentCode,
            DepartmentName = d.DepartmentName,
            Description = d.Description,
            IsActive = d.IsActive,
            ParentId = d.ParentId,
            CreatedDate = d.CreatedDate,
            UpdatedDate = d.UpdatedDate,
            Children = new List<DepartmentNode>()
        }).ToList();

        var nodeMap = allNodes.ToDictionary(n => n.Id);
        var roots = new List<DepartmentNode>();

        foreach (var node in allNodes)
        {
            if (node.ParentId.HasValue && nodeMap.TryGetValue(node.ParentId.Value, out var parentNode))
            {
                parentNode.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return Ok(roots);
    }

    [HttpPost]
    public override async Task<IActionResult> Create([FromBody] Department entity)
    {
        if (entity.ParentId.HasValue)
        {
            var parent = await Repo.GetById<Department>(entity.ParentId.Value);
            if (parent is null)
                return BadRequest(new { Message = "Parent department not found." });
        }
        return await base.Create(entity);
    }

    [HttpPut("{id:int}")]
    public override async Task<IActionResult> Update(int id, [FromBody] Department entity)
    {
        if (entity.ParentId.HasValue)
        {
            if (entity.ParentId.Value == id)
                return BadRequest(new { Message = "A department cannot be its own parent." });

            var parent = await Repo.GetById<Department>(entity.ParentId.Value);
            if (parent is null)
                return BadRequest(new { Message = "Parent department not found." });

            var all = await Repo.GetAll<Department>();
            if (IsAncestorOf(all, id, entity.ParentId.Value))
                return BadRequest(new { Message = "Cannot set a descendant as parent (circular reference)." });
        }
        return await base.Update(id, entity);
    }

    [HttpDelete("{id:int}")]
    public override async Task<IActionResult> Delete(int id)
    {
        var children = await Repo.FindByExpression<Department>(d => d.ParentId == id);
        if (children.Count > 0)
            return BadRequest(new { Message = $"Cannot delete department with {children.Count} child department(s)." });

        return await base.Delete(id);
    }

    private static bool IsAncestorOf(List<Department> all, int ancestorId, int targetId)
    {
        var current = all.FirstOrDefault(d => d.Id == targetId);
        while (current?.ParentId.HasValue == true)
        {
            if (current.ParentId.Value == ancestorId) return true;
            current = all.FirstOrDefault(d => d.Id == current.ParentId.Value);
        }
        return false;
    }
}

public class DepartmentNode
{
    public int Id { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? ParentId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<DepartmentNode> Children { get; set; } = new();
}
