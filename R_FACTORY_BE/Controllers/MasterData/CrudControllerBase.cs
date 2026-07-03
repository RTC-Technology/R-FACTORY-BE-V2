using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers.MasterData;

public abstract class CrudControllerBase<T> : ControllerBase where T : class, new()
{
    protected readonly IGenericRepo Repo;

    protected CrudControllerBase(IGenericRepo repo) => Repo = repo;

    [HttpGet]
    public virtual async Task<IActionResult> GetAll()
    {
        var rows = await Repo.GetAll<T>();
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public virtual async Task<IActionResult> GetById(int id)
    {
        var item = await Repo.GetById<T>(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] T entity)
    {
        SetCreatedTimestamp(entity);
        var saved = await Repo.Insert(entity);
        return Ok(saved);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<IActionResult> Update(int id, [FromBody] T entity)
    {
        SetId(entity, id);
        SetUpdatedTimestamp(entity);
        var saved = await Repo.Update(entity);
        return Ok(saved);
    }

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var deleted = await Repo.DeleteById<T>(id);
        return deleted ? NoContent() : NotFound();
    }

    protected static void SetId(object entity, int id)
    {
        var prop = entity.GetType().GetProperty("Id");
        if (prop is null || !prop.CanWrite) return;
        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        prop.SetValue(entity, Convert.ChangeType(id, targetType));
    }

    protected static void SetCreatedTimestamp(object entity)
    {
        var prop = entity.GetType().GetProperty("CreatedDate");
        if (prop?.CanWrite != true) return;
        if (prop.PropertyType != typeof(DateTime)) return;
        if ((DateTime)prop.GetValue(entity)! != default) return;
        prop.SetValue(entity, DateTime.Now);
    }

    protected static void SetUpdatedTimestamp(object entity)
    {
        var prop = entity.GetType().GetProperty("UpdatedDate");
        if (prop?.CanWrite != true) return;
        if (prop.PropertyType != typeof(DateTime?) && prop.PropertyType != typeof(DateTime)) return;
        prop.SetValue(entity, DateTime.Now);
    }
}
