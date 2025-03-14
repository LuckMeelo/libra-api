using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;

namespace ApiLib.Config
{
    public static class LoggingConfig
    {
        public static void AddLogging(WebApplicationBuilder builder)
        {
            // Create and configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Logging.ClearProviders();
            builder.Host.UseSerilog();
        }

        public static void UseLogging(WebApplication app)
        {
            app.UseSerilogRequestLogging(); // Logs HTTP requests
        }

    }
}
