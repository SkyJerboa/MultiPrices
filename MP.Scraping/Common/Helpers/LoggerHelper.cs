using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace MP.Scraping.Common.Heplers
{
    public static class LoggerHelper
    {
        public static void ConfigureLogger(WebHostBuilderContext context, LoggerConfiguration loggerConfig)
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console()
                .WriteTo.File(
                    path: @"Logs\log-.txt",
                    rollingInterval: RollingInterval.Month,
                    retainedFileCountLimit: null,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning
                );
        }
    }
}
