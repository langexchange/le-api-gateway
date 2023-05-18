using LE.ApiGateway.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

namespace LE.ApiGateway
{
    public class Startup
    {
        private IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                var allowedOrigins = Configuration.GetSection("GatewayOptions:AllowedOrigins").Get<string[]>();
                var isAllowedAll = Configuration.GetSection("GatewayOptions:AllowedAll").Get<bool>();
                options.AddPolicy("CorsPolicy", builder =>
                {
                    var corsPolicy = isAllowedAll ? builder.SetIsOriginAllowed(x => _ = true) :
                    allowedOrigins?.Length > 0 ? builder.WithOrigins(allowedOrigins) : null;
                    corsPolicy?.AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                });
            });

            services.AddCustomAuthorization(Configuration);
            services.AddOcelot(Configuration)
                .AddConsul();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseCustomAuthorization();
            app.UseHealthCheck("/");
            //app.RunSwaggerUI();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");
            app.UseWebSockets();
            app.UseOcelot().Wait();
        }
    }
}
