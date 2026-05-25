using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MainServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var resourceBuilder = ResourceBuilder.CreateDefault().AddService(Telemetry.ServiceName);


            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
               .SetResourceBuilder(resourceBuilder)
               .AddSource(Telemetry.ServiceName) 
               .AddConsoleExporter()            
               .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter(Telemetry.ServiceName)
                 .AddView(
                instrumentName: "commands.processing.duration", 
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = new double[] { 25, 50, 100, 500, 1000, 2000, 5000, 10000, 20000 }
                })
                    .AddConsoleExporter()              
                    .Build();

            var counter = Telemetry.Meter.CreateCounter<long>("app.loops.count");

            

            TcpServer server = new TcpServer(new OtusCSharpModels.SimpleStore());
            _ = server.StartAsync(new byte[] { 127, 0, 0, 1 }, 8888);
            Console.ReadLine();
        }
    }
}
