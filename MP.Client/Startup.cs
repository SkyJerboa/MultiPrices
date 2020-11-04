﻿using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using MP.Client.Common.Configuration;
using MP.Client.Common.DapperMapper;
using MP.Client.Contexts;
using MP.Core.Common.Configuration;
using MP.Core.Contexts.Games;

namespace MP.Client
{
    public class Startup
    {
        public IWebHostEnvironment HostingEnvironment { get; private set; }

        IConfiguration _configuration;
        SiteConfigurationManager _siteConfigurationManager;
        byte[] _configHash;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            this.HostingEnvironment = env;
            this._configuration = configuration;

            SqlMapper.AddTypeHandler(new LanguagesHandler());
            SqlMapper.AddTypeHandler(new StringArrayHandler());
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _siteConfigurationManager
                = ConfigurationLoader.LoadConfiguration<SiteConfigurationManager>(_configuration, out _configHash);
            
            string conn = SiteConfigurationManager.DefaultConnection;

            services.AddDbContext<GameContext>(options => options.UseNpgsql(conn));
            services.AddDbContext<MainContext>(options => options.UseNpgsql(conn));

            //удалить при релизе
            services.AddCors();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = SiteConfigurationManager.Config.AuthOptions.Issuer,

                            ValidateAudience = true,
                            ValidAudience = SiteConfigurationManager.Config.AuthOptions.Audience,
                            ValidateLifetime = true,

                            IssuerSigningKey = SiteConfigurationManager.Config.AuthOptions.GetSymmetricSecurityKey(),
                            ValidateIssuerSigningKey = true,
                        };
                    });

            services.AddMvc()
                .AddNewtonsoftJson(o =>
                {
                    o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    o.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "mp-client/build";
            });
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            //app.UseCookiePolicy();

            app.UseRouting();

            //удалить при релизе
            app.UseCors(builder => builder.AllowAnyOrigin());

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStatusCodePages(async context =>
            {
                if (context.HttpContext.Response.StatusCode == 403)
                {
                    await context.HttpContext.Response.WriteAsync(
                        "Status code page, status code: " +
                        context.HttpContext.Response.StatusCode);
                }

            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "games",
                    pattern: "{controller=Games}/{action=Index}"
                );
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "mp-client";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

            ChangeToken.OnChange(() => _configuration.GetReloadToken(), ReloadConfig);
        }

        private void ReloadConfig()
            => ConfigurationLoader.ReloadConfiguration(_siteConfigurationManager, ref _configHash);
    }
}
