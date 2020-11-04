using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using MP.Core.Common.Configuration;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Models.Games;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Models.Services;
using MP.Scraping.Models.Users;
using System.IO;

namespace MP.Scraping
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        IConfiguration _configuration;
        byte[] _configHash;
        ScrapingConfigurationManager _scrapingConfigurationManager;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _scrapingConfigurationManager 
                = ConfigurationLoader.LoadConfiguration<ScrapingConfigurationManager>(_configuration, out _configHash);
            
            string servicesConnection = ScrapingConfigurationManager.Config.DefaultConnection;
            string siteConnection = ScrapingConfigurationManager.Config.SiteConnection;

            services.AddDbContext<GameWithHistoryContext>(options => options.UseNpgsql(siteConnection));
            services.AddDbContext<ServiceContext>(options => options.UseNpgsql(servicesConnection));
            services.AddDbContext<ServiceGameContext>(options => options.UseNpgsql(servicesConnection));
            services.AddDbContext<HistoryContext>(options => options.UseNpgsql(servicesConnection));
            services.AddDbContext<UserContext>(options => options.UseNpgsql(servicesConnection));

            services.AddRazorPages()
                .AddRazorPagesOptions(o => 
                {
                    o.Conventions.AuthorizeFolder("/");
                    o.Conventions.AllowAnonymousToPage("/Index");
                    o.Conventions.AllowAnonymousToPage("/Account");
                    o.Conventions.AllowAnonymousToPage("/Privacy");
                    o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
                    o.Conventions.AddPageRoute("/ServiceGame", "ServiceGame/{serviceCode:alpha}/{id:int}");
                    o.Conventions.AddPageRoute("/ServiceGame", "ServiceGame/{id:int}");
                })
                .AddRazorRuntimeCompilation();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => //CookieAuthenticationOptions
                {
                    options.Cookie.Name = "scr";
                    options.LoginPath = new PathString("/account/login");
                });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            ChangeToken.OnChange(() => _configuration.GetReloadToken(), ReloadConfig);

            ConfigureLocalTasks();
        }

        private void ReloadConfig() 
            => ConfigurationLoader.ReloadConfiguration(_scrapingConfigurationManager, ref _configHash);

        private void ConfigureLocalTasks()
        {
            string imgMapDir = Path.Combine("Temp", "ImageMap");
            if (!Directory.Exists(imgMapDir))
                Directory.CreateDirectory(imgMapDir);

            ServiceScraper.ConfigureScraping();
        }
    }
}
