using System.Text;
using R_FACTORY_BE.Models.Models;
using RepoDb;

namespace R_FACTORY_BE;

public static class RepoDbMappings
{
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;

        Map<Department>("departments");
        Map<Area>("areas");
        Map<Factory>("factories");
        Map<Line>("lines");
        Map<Machine>("machines");
        Map<MachineDowntimeEvent>("machine_downtime_events");
        Map<MachineStatusLog>("machine_status_logs");
        Map<MachineType>("machine_types");
        Map<Model>("models");
        Map<ModelMachineCycleTime>("model_machine_cycle_times");
        Map<PlannedDowntimeSchedule>("planned_downtime_schedules");
        Map<PlannedDowntimeType>("planned_downtime_types");
        Map<ProductionOutputLog>("production_output_logs");
        Map<ProductionPlan>("production_plans");
        Map<Shift>("shifts");
        Map<UnplannedDowntimeReason>("unplanned_downtime_reasons");
        Map<User>("users");

        initialized = true;
    }

    private static void Map<T>(string tableName) where T : class
    {
        var map = FluentMapper.Entity<T>()
            .Table(tableName)
            .Primary(nameof(Factory.Id))
            .Identity(nameof(Factory.Id));

        foreach (var property in typeof(T).GetProperties())
        {
            map.Column(property.Name, ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0)
            {
                var previous = value[i - 1];
                var hasNext = i + 1 < value.Length;
                if (!char.IsUpper(previous) || hasNext && char.IsLower(value[i + 1]))
                {
                    builder.Append('_');
                }
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
