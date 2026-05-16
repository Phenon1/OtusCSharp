using System.Diagnostics;
using System.Diagnostics.Metrics;

public static class Telemetry
{
    public const string ServiceName = nameof(MainServer);
    public static readonly ActivitySource Source = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> CommandCounter = Meter.CreateCounter<long>(
        name: "commands.processed.count",
        unit: "1",
        description: "Количество обработанных команд");

    public static readonly Histogram<double> CommandDurationHistogram = Meter.CreateHistogram<double>(
        name: "commands.processing.duration",
        unit: "ms",
        description: "Время выполнения команды");
}
