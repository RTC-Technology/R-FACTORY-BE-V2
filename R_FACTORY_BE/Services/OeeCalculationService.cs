using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Services;

public interface IOeeCalculationService
{
    Task<IReadOnlyList<OeeSummaryDto>> GetSummary(OeeFilterDto filter);
    Task<IReadOnlyList<TimelineSegmentDto>> GetTimeline(OeeFilterDto filter);
    Task<IReadOnlyList<LayoutDto>> GetLayout(OeeFilterDto filter);
    Task<MachineDetailDto?> GetMachineDetail(int machineId, OeeFilterDto filter);
}

public sealed class OeeCalculationService(IGenericRepo repo) : IOeeCalculationService
{
    private static readonly HashSet<string> RunningStatuses = new(StringComparer.OrdinalIgnoreCase) { "running" };
    private static readonly HashSet<string> StoppedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "stopped", "offline", "maintenance"
    };

    public async Task<IReadOnlyList<OeeSummaryDto>> GetSummary(OeeFilterDto filter)
    {
        var normalized = Normalize(filter);
        var lines = await repo.GetAll<Line>();
        var lineIdsInArea = normalized.AreaId.HasValue
            ? lines.Where(line => line.AreaId == normalized.AreaId.Value).Select(line => line.Id).ToHashSet()
            : null;
        var machines = (await repo.GetAll<Machine>())
            .Where(m => m.FactoryId == normalized.FactoryId)
            .Where(m => !normalized.AreaId.HasValue || lineIdsInArea!.Contains(m.LineId))
            .Where(m => !normalized.LineId.HasValue || m.LineId == normalized.LineId.Value)
            .Where(m => !normalized.MachineId.HasValue || m.Id == normalized.MachineId.Value)
            .Where(m => m.IsActive != false)
            .OrderBy(m => m.MachineCode)
            .ToList();

        var outputs = (await repo.GetAll<ProductionOutputLog>())
            .Where(x => InScope(x.FactoryId, x.LineId, x.MachineId, x.ShiftId, x.LogTime, normalized, lineIdsInArea))
            .ToList();
        var statuses = (await repo.GetAll<MachineStatusLog>())
            .Where(x => x.FactoryId == normalized.FactoryId)
            .Where(x => !normalized.AreaId.HasValue || lineIdsInArea!.Contains(x.LineId))
            .Where(x => !normalized.LineId.HasValue || x.LineId == normalized.LineId.Value)
            .Where(x => !normalized.MachineId.HasValue || x.MachineId == normalized.MachineId.Value)
            .Where(x => !normalized.ShiftId.HasValue || x.ShiftId == normalized.ShiftId.Value)
            .Where(x => Overlaps(x.StartTime, x.EndTime, normalized.From, normalized.To))
            .ToList();
        var plannedDowntime = (await repo.GetAll<PlannedDowntimeSchedule>())
            .Where(x => x.FactoryId == normalized.FactoryId && x.IsActive != false)
            .Where(x => !normalized.AreaId.HasValue || !x.LineId.HasValue || lineIdsInArea!.Contains(x.LineId.Value))
            .Where(x => !normalized.LineId.HasValue || !x.LineId.HasValue || x.LineId == normalized.LineId.Value)
            .Where(x => !normalized.MachineId.HasValue || !x.MachineId.HasValue || x.MachineId == normalized.MachineId.Value)
            .Where(x => !normalized.ShiftId.HasValue || !x.ShiftId.HasValue || x.ShiftId == normalized.ShiftId.Value)
            .Where(x => Overlaps(x.StartTime, x.EndTime, normalized.From, normalized.To))
            .ToList();
        var downtimeEvents = (await repo.GetAll<MachineDowntimeEvent>())
            .Where(x => x.FactoryId == normalized.FactoryId)
            .Where(x => !normalized.AreaId.HasValue || lineIdsInArea!.Contains(x.LineId))
            .Where(x => !normalized.LineId.HasValue || x.LineId == normalized.LineId.Value)
            .Where(x => !normalized.MachineId.HasValue || x.MachineId == normalized.MachineId.Value)
            .Where(x => !normalized.ShiftId.HasValue || x.ShiftId == normalized.ShiftId.Value)
            .Where(x => Overlaps(x.StartTime, x.EndTime, normalized.From, normalized.To))
            .ToList();
        var cycleTimes = (await repo.GetAll<ModelMachineCycleTime>())
            .Where(x => x.IsActive != false)
            .ToList();

        return machines.Select(machine =>
        {
            var machineOutputs = outputs.Where(x => x.MachineId == machine.Id).ToList();
            var machineStatuses = statuses.Where(x => x.MachineId == machine.Id).ToList();
            var machineEvents = downtimeEvents.Where(x => x.MachineId == machine.Id).ToList();

            var goodQty = machineOutputs.Sum(x => x.GoodQty);
            var ngQty = machineOutputs.Sum(x => x.NgQty);
            var totalQty = goodQty + ngQty;
            var plannedDowntimeSeconds = plannedDowntime
                .Where(x => AppliesToMachine(x, machine))
                .Sum(x => OverlapSeconds(x.StartTime, x.EndTime, normalized.From, normalized.To));
            var plannedProductionSeconds = Math.Max(0, (decimal)(normalized.To - normalized.From).TotalSeconds - plannedDowntimeSeconds);
            var actualRunSeconds = machineStatuses
                .Where(x => RunningStatuses.Contains(x.Status))
                .Sum(x => OverlapSeconds(x.StartTime, x.EndTime, normalized.From, normalized.To));
            var unplannedDowntimeSeconds = machineEvents
                .Where(x => string.Equals(x.DowntimeType, "unplanned", StringComparison.OrdinalIgnoreCase))
                .Sum(x => OverlapSeconds(x.StartTime, x.EndTime, normalized.From, normalized.To));

            if (unplannedDowntimeSeconds == 0)
            {
                unplannedDowntimeSeconds = machineStatuses
                    .Where(x => StoppedStatuses.Contains(x.Status))
                    .Sum(x => OverlapSeconds(x.StartTime, x.EndTime, normalized.From, normalized.To));
            }

            var idealWorkSeconds = machineOutputs.Sum(output =>
            {
                if (!output.ModelId.HasValue) return 0m;
                var cycleTime = cycleTimes.FirstOrDefault(x => x.MachineId == machine.Id && x.ModelId == output.ModelId.Value);
                return (output.GoodQty + output.NgQty) * (cycleTime?.IdealCycleTimeSeconds ?? 0m);
            });

            var availability = Divide(actualRunSeconds, plannedProductionSeconds);
            var performance = Divide(idealWorkSeconds, actualRunSeconds);
            var quality = Divide(goodQty, totalQty);
            var oee = availability * performance * quality;
            var current = machineStatuses
                .OrderByDescending(x => x.StartTime)
                .FirstOrDefault();

            return new OeeSummaryDto(
                machine.Id,
                machine.MachineCode,
                machine.MachineName,
                machine.FactoryId,
                lines.FirstOrDefault(line => line.Id == machine.LineId)?.AreaId ?? 0,
                machine.LineId,
                Decimal.Round(oee, 4),
                Decimal.Round(availability, 4),
                Decimal.Round(performance, 4),
                Decimal.Round(quality, 4),
                goodQty,
                ngQty,
                totalQty,
                Decimal.Round(plannedProductionSeconds, 2),
                Decimal.Round(actualRunSeconds, 2),
                Decimal.Round(plannedDowntimeSeconds, 2),
                Decimal.Round(unplannedDowntimeSeconds, 2),
                current?.Status ?? "offline",
                current?.StartTime);
        }).ToList();
    }

    public async Task<IReadOnlyList<TimelineSegmentDto>> GetTimeline(OeeFilterDto filter)
    {
        var normalized = Normalize(filter);
        var lines = await repo.GetAll<Line>();
        var lineIdsInArea = normalized.AreaId.HasValue
            ? lines.Where(line => line.AreaId == normalized.AreaId.Value).Select(line => line.Id).ToHashSet()
            : null;
        var events = await repo.GetAll<MachineDowntimeEvent>();
        var reasons = await repo.GetAll<UnplannedDowntimeReason>();
        var plannedTypes = await repo.GetAll<PlannedDowntimeType>();

        return (await repo.GetAll<MachineStatusLog>())
            .Where(x => x.FactoryId == normalized.FactoryId)
            .Where(x => !normalized.AreaId.HasValue || lineIdsInArea!.Contains(x.LineId))
            .Where(x => !normalized.LineId.HasValue || x.LineId == normalized.LineId.Value)
            .Where(x => !normalized.MachineId.HasValue || x.MachineId == normalized.MachineId.Value)
            .Where(x => !normalized.ShiftId.HasValue || x.ShiftId == normalized.ShiftId.Value)
            .Where(x => Overlaps(x.StartTime, x.EndTime, normalized.From, normalized.To))
            .OrderBy(x => x.MachineId)
            .ThenBy(x => x.StartTime)
            .Select(x =>
            {
                var downtime = events.FirstOrDefault(e =>
                    e.MachineStatusLogId == x.Id ||
                    (e.MachineId == x.MachineId && Overlaps(e.StartTime, e.EndTime, x.StartTime, x.EndTime ?? normalized.To)));
                var reasonName = downtime?.DowntimeType?.Equals("planned", StringComparison.OrdinalIgnoreCase) == true
                    ? plannedTypes.FirstOrDefault(r => r.Id == downtime.PlannedDowntimeTypeId)?.Name
                    : reasons.FirstOrDefault(r => r.Id == downtime?.UnplannedDowntimeReasonId)?.Name;

                return new TimelineSegmentDto(
                    x.MachineId,
                    x.Status,
                    Max(x.StartTime, normalized.From),
                    Min(x.EndTime ?? normalized.To, normalized.To),
                    Decimal.Round(OverlapSeconds(x.StartTime, x.EndTime, normalized.From, normalized.To), 2),
                    downtime?.DowntimeType,
                    reasonName);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LayoutDto>> GetLayout(OeeFilterDto filter)
    {
        var normalized = Normalize(filter);
        var lines = (await repo.GetAll<Line>())
            .Where(x => x.FactoryId == normalized.FactoryId)
            .Where(x => !normalized.LineId.HasValue || x.Id == normalized.LineId.Value)
            .ToList();
        var areas = (await repo.GetAll<Area>())
            .Where(x => x.FactoryId == normalized.FactoryId)
            .Where(x => !normalized.AreaId.HasValue || x.Id == normalized.AreaId.Value)
            .Where(x => !normalized.LineId.HasValue || lines.Any(line => line.AreaId == x.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.AreaCode)
            .ToList();
        var machines = (await repo.GetAll<Machine>()).ToDictionary(x => x.Id);
        var summaries = await GetSummary(normalized);

        return areas.Select(area => new LayoutDto(
            area.Id,
            area.AreaCode,
            area.AreaName,
            area.LayoutImagePath,
            summaries.Where(x => x.AreaId == area.Id)
                .Select(x => new LayoutMachineDto(
                    x.MachineId,
                    x.MachineCode,
                    x.MachineName,
                    machines.TryGetValue(x.MachineId, out var machine) ? machine.LayoutX : null,
                    machines.TryGetValue(x.MachineId, out machine) ? machine.LayoutY : null,
                    machines.TryGetValue(x.MachineId, out machine) ? machine.LayoutWidth : null,
                    machines.TryGetValue(x.MachineId, out machine) ? machine.LayoutHeight : null,
                    x.CurrentStatus,
                    x.Oee,
                    x.Availability,
                    x.Performance,
                    x.Quality,
                    x.GoodQty,
                    x.NgQty,
                    x.TotalQty))
                .ToList()))
            .ToList();
    }

    public async Task<MachineDetailDto?> GetMachineDetail(int machineId, OeeFilterDto filter)
    {
        var machineFilter = filter with { MachineId = machineId };
        var summary = (await GetSummary(machineFilter)).FirstOrDefault();
        if (summary is null) return null;
        return new MachineDetailDto(summary, await GetTimeline(machineFilter));
    }

    private static OeeFilterDto Normalize(OeeFilterDto filter)
    {
        var now = DateTime.Now;
        var to = filter.To == default ? now : filter.To;
        if (to > now) to = now;
        var from = filter.From == default ? to.Date : filter.From;
        if (from > to) (from, to) = (to, from);
        return filter with { From = from, To = to };
    }

    private static bool InScope(int factoryId, int lineId, int machineId, int? shiftId, DateTime time, OeeFilterDto filter, HashSet<int>? lineIdsInArea) =>
        factoryId == filter.FactoryId &&
        (!filter.AreaId.HasValue || lineIdsInArea?.Contains(lineId) == true) &&
        (!filter.LineId.HasValue || lineId == filter.LineId.Value) &&
        (!filter.MachineId.HasValue || machineId == filter.MachineId.Value) &&
        (!filter.ShiftId.HasValue || shiftId == filter.ShiftId.Value) &&
        time >= filter.From &&
        time <= filter.To;

    private static bool AppliesToMachine(PlannedDowntimeSchedule schedule, Machine machine) =>
        (!schedule.LineId.HasValue || schedule.LineId == machine.LineId) &&
        (!schedule.MachineId.HasValue || schedule.MachineId == machine.Id);

    private static decimal Divide(decimal numerator, decimal denominator) => denominator <= 0 ? 0 : numerator / denominator;
    private static decimal Divide(int numerator, int denominator) => denominator <= 0 ? 0 : (decimal)numerator / denominator;

    private static bool Overlaps(DateTime start, DateTime? end, DateTime from, DateTime to) =>
        start < to && (end ?? to) > from;

    private static decimal OverlapSeconds(DateTime start, DateTime? end, DateTime from, DateTime to)
    {
        var clippedStart = Max(start, from);
        var clippedEnd = Min(end ?? to, to);
        return clippedEnd <= clippedStart ? 0 : (decimal)(clippedEnd - clippedStart).TotalSeconds;
    }

    private static DateTime Max(DateTime left, DateTime right) => left > right ? left : right;
    private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
}
