using FastEndpoints;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Wisp.ServiceDefaults;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Wires up what every Wisp service shares: OpenTelemetry logs, metrics and traces (exported over OTLP when
        /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set), service discovery, resilient HTTP clients, and health checks on
        /// <c>/healthz</c> (liveness) and <c>/readyz</c> (readiness).
        /// </summary>
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.Services.AddServiceDiscovery();
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });

            return builder;
        }

        private TBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();
            builder.AddDefaultHealthChecks();

            return builder;
        }
        
        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        private TBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddServiceHealthChecks(
                configureOptions: options =>
                {
                    options.LivePath = "/healthz";
                    options.ReadyPath = "/readyz";

                    options.UseJsonResponse = builder.Environment.IsDevelopment();
                });
            
            return builder;
        }
    }
}