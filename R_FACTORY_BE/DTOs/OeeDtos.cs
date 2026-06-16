namespace R_FACTORY_BE.DTOs;

public sealed record OeeFilterDto(
    int FactoryId,
    int? AreaId,
    int? LineId,
    int? MachineId,
    int? ShiftId,
    DateTime From,
    DateTime To);

public sealed record OeeSummaryDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    int FactoryId,
    int AreaId,
    int LineId,
    decimal Oee,
    decimal Availability,
    decimal Performance,
    decimal Quality,
    int GoodQty,
    int NgQty,
    int TotalQty,
    decimal PlannedProductionSeconds,
    decimal ActualRunSeconds,
    decimal PlannedDowntimeSeconds,
    decimal UnplannedDowntimeSeconds,
    string CurrentStatus,
    DateTime? LastStatusTime);

public sealed record TimelineSegmentDto(
    int MachineId,
    string Status,
    DateTime StartTime,
    DateTime? EndTime,
    decimal DurationSeconds,
    string? DowntimeType,
    string? ReasonName);

public sealed record LayoutMachineDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    decimal? LayoutX,
    decimal? LayoutY,
    decimal? LayoutWidth,
    decimal? LayoutHeight,
    string CurrentStatus,
    decimal Oee,
    decimal Availability,
    decimal Performance,
    decimal Quality,
    int GoodQty,
    int NgQty,
    int TotalQty);

public sealed record LayoutDto(
    int AreaId,
    string AreaCode,
    string AreaName,
    string? LayoutImagePath,
    IReadOnlyList<LayoutMachineDto> Machines);

public sealed record MachineDetailDto(
    OeeSummaryDto Summary,
    IReadOnlyList<TimelineSegmentDto> Timeline);

public sealed record ProductionOutputRequest(
    int FactoryId,
    int LineId,
    int MachineId,
    int GoodQty,
    int NgQty,
    DateTime LogTime,
    int? ModelId,
    long? ProductionPlanId,
    int? ShiftId,
    string? SourceType,
    string? RawData,
    int? CreatedBy);

public sealed record MachineStatusRequest(
    int FactoryId,
    int LineId,
    int MachineId,
    string Status,
    DateTime StartTime,
    DateTime? EndTime,
    long? ProductionPlanId,
    int? ShiftId,
    string? SourceType,
    string? RawData);

public sealed record DowntimeEventRequest(
    int FactoryId,
    int LineId,
    int MachineId,
    DateTime StartTime,
    DateTime? EndTime,
    string DowntimeType,
    int? PlannedDowntimeTypeId,
    int? UnplannedDowntimeReasonId,
    long? ProductionPlanId,
    long? MachineStatusLogId,
    int? ShiftId,
    string? RootCause,
    string? CorrectiveAction,
    string? Note,
    int? CreatedBy);
