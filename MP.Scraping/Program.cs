using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using MP.Scraping.Common.Heplers;
using Serilog;

namespace MP.Scraping
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseSerilog(LoggerHelper.ConfigureLogger)
                .UseUrls("http://*:5000")
                .UseStartup<Startup>();
        }
    }
}
