using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace LE.ApiGateway.Extensions
{
    public static class ApiGatewayExtension
    {
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
