using R_FACTORY_BE.Models.DTOs;
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
    private static readonly string[] FilterParameterNames =
    [
        "p_factory_id",
        "p_area_id",
        "p_line_id",
        "p_machine_id",
        "p_shift_id",
        "p_from",
        "p_to"
    ];

    public async Task<IReadOnlyList<OeeSummaryDto>> GetSummary(OeeFilterDto filter)
    {
        var rows = await repo.ProcedureToList<OeeSummaryRow>("sp_oee_summary", FilterParameterNames, FilterParameterValues(Normalize(filter)));
        return rows.Select(row => new OeeSummaryDto(
            row.MachineId,
            row.MachineCode,
            row.MachineName,
            row.FactoryId,
            row.AreaId,
            row.LineId,
            row.Oee,
            row.Availability,
            row.Performance,
            row.Quality,
            row.GoodQty,
            row.NgQty,
            row.TotalQty,
            row.PlannedProductionSeconds,
            row.ActualRunSeconds,
            row.PlannedDowntimeSeconds,
            row.UnplannedDowntimeSeconds,
            row.CurrentStatus,
            row.LastStatusTime)).ToList();
    }

    public async Task<IReadOnlyList<TimelineSegmentDto>> GetTimeline(OeeFilterDto filter)
    {
        var rows = await repo.ProcedureToList<TimelineSegmentRow>("sp_oee_timeline", FilterParameterNames, FilterParameterValues(Normalize(filter)));
        return rows.Select(row => new TimelineSegmentDto(
            row.MachineId,
            row.Status,
            row.StartTime,
            row.EndTime,
            row.DurationSeconds,
            row.DowntimeType,
            row.ReasonName)).ToList();
    }

    public async Task<IReadOnlyList<LayoutDto>> GetLayout(OeeFilterDto filter)
    {
        var rows = await repo.ProcedureToList<LayoutRow>("sp_oee_layout", FilterParameterNames, FilterParameterValues(Normalize(filter)));
        return rows
            .GroupBy(row => new { row.AreaId, row.AreaCode, row.AreaName, row.LayoutImagePath })
            .Select(group => new LayoutDto(
                group.Key.AreaId,
                group.Key.AreaCode,
                group.Key.AreaName,
                group.Key.LayoutImagePath,
                group.Where(row => row.MachineId.HasValue)
                    .Select(row => new LayoutMachineDto(
                        row.MachineId!.Value,
                        row.MachineCode ?? string.Empty,
                        row.MachineName ?? string.Empty,
                        row.LayoutX,
                        row.LayoutY,
                        row.LayoutWidth,
                        row.LayoutHeight,
                        row.CurrentStatus ?? "offline",
                        row.Oee,
                        row.Availability,
                        row.Performance,
                        row.Quality,
                        row.GoodQty,
                        row.NgQty,
                        row.TotalQty))
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

    private static object[] FilterParameterValues(OeeFilterDto filter) =>
    [
        filter.FactoryId,
        filter.AreaId.HasValue ? filter.AreaId.Value : DBNull.Value,
        filter.LineId.HasValue ? filter.LineId.Value : DBNull.Value,
        filter.MachineId.HasValue ? filter.MachineId.Value : DBNull.Value,
        filter.ShiftId.HasValue ? filter.ShiftId.Value : DBNull.Value,
        filter.From,
        filter.To
    ];

    public sealed class OeeSummaryRow
    {
        public int MachineId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public int FactoryId { get; set; }
        public int AreaId { get; set; }
        public int LineId { get; set; }
        public decimal Oee { get; set; }
        public decimal Availability { get; set; }
        public decimal Performance { get; set; }
        public decimal Quality { get; set; }
        public int GoodQty { get; set; }
        public int NgQty { get; set; }
        public int TotalQty { get; set; }
        public decimal PlannedProductionSeconds { get; set; }
        public decimal ActualRunSeconds { get; set; }
        public decimal PlannedDowntimeSeconds { get; set; }
        public decimal UnplannedDowntimeSeconds { get; set; }
        public string CurrentStatus { get; set; } = "offline";
        public DateTime? LastStatusTime { get; set; }
    }

    public sealed class TimelineSegmentRow
    {
        public int MachineId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal DurationSeconds { get; set; }
        public string? DowntimeType { get; set; }
        public string? ReasonName { get; set; }
    }

    public sealed class LayoutRow
    {
        public int AreaId { get; set; }
        public string AreaCode { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string? LayoutImagePath { get; set; }
        public int? MachineId { get; set; }
        public string? MachineCode { get; set; }
        public string? MachineName { get; set; }
        public decimal? LayoutX { get; set; }
        public decimal? LayoutY { get; set; }
        public decimal? LayoutWidth { get; set; }
        public decimal? LayoutHeight { get; set; }
        public string? CurrentStatus { get; set; }
        public decimal Oee { get; set; }
        public decimal Availability { get; set; }
        public decimal Performance { get; set; }
        public decimal Quality { get; set; }
        public int GoodQty { get; set; }
        public int NgQty { get; set; }
        public int TotalQty { get; set; }
    }
}