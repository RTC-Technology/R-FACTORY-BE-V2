using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/master-data/{resource}")]
public sealed class MasterDataController(IGenericRepo repo) : ControllerBase
{
    private static readonly Dictionary<string, Type> Resources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["factories"] = typeof(Factory),
        ["areas"] = typeof(Area),
        ["lines"] = typeof(Line),
        ["machines"] = typeof(Machine),
        ["machine-types"] = typeof(MachineType),
        ["models"] = typeof(Model),
        ["model-machine-cycle-times"] = typeof(ModelMachineCycleTime),
        ["shifts"] = typeof(Shift),
        ["planned-downtime-types"] = typeof(PlannedDowntimeType),
        ["planned-downtime-schedules"] = typeof(PlannedDowntimeSchedule),
        ["unplanned-downtime-reasons"] = typeof(UnplannedDowntimeReason),
        ["machine-status-logs"] = typeof(MachineStatusLog),
        ["departments"] = typeof(Department),
        ["users"] = typeof(User),
        ["roles"] = typeof(Role),
        ["permissions"] = typeof(Permission)
    };

    [HttpGet]
    public async Task<IActionResult> GetAll(string resource)
    {
        if (!TryResource(resource, out var type)) return NotFound("Unknown master data resource.");
        return Ok(await InvokeRepoList(nameof(IGenericRepo.GetAll), type));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string resource, long id)
    {
        if (!TryResource(resource, out var type)) return NotFound("Unknown master data resource.");
        var item = await InvokeRepoItem(nameof(IGenericRepo.GetById), type, NormalizeId(type, id));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string resource, [FromBody] JsonElement body)
    {
        if (!TryResource(resource, out var type)) return NotFound("Unknown master data resource.");
        var entity = Deserialize(body, type);
        SetCreatedTimestamp(entity);
        var saved = await InvokeRepoItem(nameof(IGenericRepo.Insert), type, entity);
        return Ok(saved);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string resource, long id, [FromBody] JsonElement body)
    {
        if (!TryResource(resource, out var type)) return NotFound("Unknown master data resource.");
        var entity = Deserialize(body, type);
        SetId(entity, id);
        SetUpdatedTimestamp(entity);
        var saved = await InvokeRepoItem(nameof(IGenericRepo.Update), type, entity);
        return Ok(saved);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string resource, long id)
    {
        if (!TryResource(resource, out var type)) return NotFound("Unknown master data resource.");
        var deleted = await InvokeRepoBool(nameof(IGenericRepo.DeleteById), type, NormalizeId(type, id));
        return deleted ? NoContent() : NotFound();
    }

    private static bool TryResource(string resource, out Type type) => Resources.TryGetValue(resource, out type!);

    private static object Deserialize(JsonElement body, Type type) =>
        JsonSerializer.Deserialize(body.GetRawText(), type, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Invalid request body.");

    private async Task<object?> InvokeRepoItem(string methodName, Type type, params object[] args)
    {
        var method = typeof(IGenericRepo).GetMethods().Single(m => m.Name == methodName && m.GetGenericArguments().Length == 1);
        return await (dynamic)method.MakeGenericMethod(type).Invoke(repo, args)!;
    }

    private async Task<object> InvokeRepoList(string methodName, Type type)
    {
        var method = typeof(IGenericRepo).GetMethods().Single(m => m.Name == methodName && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 0);
        return await (dynamic)method.MakeGenericMethod(type).Invoke(repo, Array.Empty<object>())!;
    }

    private async Task<bool> InvokeRepoBool(string methodName, Type type, params object[] args)
    {
        var method = typeof(IGenericRepo).GetMethods().Single(m => m.Name == methodName && m.GetGenericArguments().Length == 1);
        return await (dynamic)method.MakeGenericMethod(type).Invoke(repo, args)!;
    }

    private static void SetId(object entity, long id)
    {
        var prop = entity.GetType().GetProperty("Id");
        if (prop is null || !prop.CanWrite) return;
        prop.SetValue(entity, ConvertIdValue(prop.PropertyType, id));
    }

    private static object NormalizeId(Type type, long id)
    {
        var prop = type.GetProperty("Id");
        return prop is null ? id : ConvertIdValue(prop.PropertyType, id);
    }

    private static object ConvertIdValue(Type propertyType, long id)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (targetType == typeof(int)) return Convert.ToInt32(id);
        if (targetType == typeof(long)) return id;
        return Convert.ChangeType(id, targetType);
    }

    private static void SetCreatedTimestamp(object entity)
    {
        var prop = entity.GetType().GetProperty("CreatedDate");
        if (prop?.CanWrite == true && prop.PropertyType == typeof(DateTime) && (DateTime)prop.GetValue(entity)! == default)
        {
            prop.SetValue(entity, DateTime.Now);
        }
    }

    private static void SetUpdatedTimestamp(object entity)
    {
        var prop = entity.GetType().GetProperty("UpdatedDate");
        if (prop?.CanWrite == true && (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime)))
        {
            prop.SetValue(entity, DateTime.Now);
        }
    }
}
