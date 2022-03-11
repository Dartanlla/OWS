using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OWSShared.Extensions
{
    public static class CustomHealthCheck
    {
        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            var OWSStorageConfig = configuration.GetSection("OWSStorageConfig");
            if (OWSStorageConfig.Exists())
            {
                hcBuilder.AddSqlServer(connectionString: OWSStorageConfig.GetValue<string>("OWSDBConnectionString"),
                    healthQuery: "SELECT 1;",
                    failureStatus: HealthStatus.Degraded,
                    name: "MSSQL",
                    tags: new string[] { "db", "sql", "sqlserver" });
            }

            var RabbitMQOptions = configuration.GetSection("RabbitMQOptions");
            if (RabbitMQOptions.Exists())
            {
                hcBuilder.AddRabbitMQ($"amqp://{RabbitMQOptions.GetValue<string>("RabbitMQUserName")}:{RabbitMQOptions.GetValue<string>("RabbitMQPassword")}@{RabbitMQOptions.GetValue<string>("RabbitMQHostName")}:{RabbitMQOptions.GetValue<int>("RabbitMQPort")}",
                    name: "RabbitMQ",
                    tags: new string[] { "rabbitmq" });
            }
            
            return services;
        }

        public static IApplicationBuilder UseCustomHealthCheck(this IApplicationBuilder builder)
        {
            builder.UseHealthChecks("/hc", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            builder.UseHealthChecks("/liveness", new HealthCheckOptions()
            {
                Predicate = r => r.Name.Contains("self")
            });
            return builder;
        }
    }
}
