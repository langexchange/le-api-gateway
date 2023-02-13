using LE.ApiGateway.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace LE.ApiGateway.Extensions
{
    public static class ApiGatewayExtension
    {
        public static IApplicationBuilder RunSwaggerUI(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<SwaggerConfig>();
            if (config?.Enable == true)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {

                    foreach (var swagger in config.Services)
                        if (swagger.Versions?.Count > 0)
                            foreach (var version in swagger.Versions)
                                c.SwaggerEndpoint($"/swagger_v{version}/{swagger.Name}", $"{swagger.Name.ToUpper()} V{version}");
                });
                app.Use(async (ctx, next) =>
                {
                    var path = ctx.Request.Path.ToString();
                    if (path.StartsWith("/swagger_v") &&
                    string.Equals(ctx.Request.Method, "GET", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var orgin = ctx.Response.Body;
                        var newContent = string.Empty;
                        using (var newBody = new MemoryStream())
                        {
                            ctx.Response.Body = newBody;
                            await next.Invoke();
                            ctx.Response.Body = orgin;
                            newBody.Seek(0, SeekOrigin.Begin);
                            newContent = new StreamReader(newBody).ReadToEnd();
                            newContent = newContent.Replace("/api/v", "/v");
                        }
                        ctx.Response.Clear();
                        await ctx.Response.WriteAsync(newContent);
                        return;
                    }
                    await next.Invoke();
                });
            }
            return app;
        }


        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration config)
        {
            var swaggerConfig = config.GetSection(nameof(SwaggerConfig)).Get<SwaggerConfig>();
            if (swaggerConfig?.Enable == true)
            {
                services.AddSingleton(swaggerConfig);
                services.AddSwashbuckle();
            }

            return services;
        }
        public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder app, string path)
        {
            app.Use((ctx, next) =>
            {
                var requestPath = ctx.Request.Path.ToString();
                if (requestPath.Equals(path))
                {
                    return ctx.Response.WriteAsync("ok");
                }
                return next.Invoke();
            });

            return app;
        }
    }
}
