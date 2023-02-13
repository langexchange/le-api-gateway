using LE.ApiGateway.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LE.ApiGateway.Extensions
{
    public static class SwashbuckleExtension
    {
        public static IServiceCollection AddSwashbuckle(this IServiceCollection services, Action<SwaggerGenOptions> customOptions = null)
        {
            IApiVersionDescriptionProvider apiProvider = null;
            try
            {
                apiProvider = services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();
            }
            catch (Exception ex)
            {
                throw;
            }


            services.AddSwaggerGen(c =>
            {
                foreach (var description in apiProvider.ApiVersionDescriptions)
                {
                    c.SwaggerDoc(
                      description.GroupName,
                        new OpenApiInfo()
                        {
                            Title = $"API {description.ApiVersion}",
                            Version = description.ApiVersion.ToString(),
                        });
                }

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    }
                });

                c.OperationFilter<AddRequiredHeaderParameter>();

                if (customOptions != null) customOptions.Invoke(c);
            });

            return services;
        }

        public static IServiceCollection AddSwashbuckle(this IServiceCollection services, IConfiguration Configuration, string ServiceName = "", Action<SwaggerGenOptions> customOptions = null)
        {
            var config = Configuration.GetSection(nameof(SwaggerConfig)).Get<SwaggerConfig>();
            if (config?.Enable == true && config.Services?.Count > 0)
            {
                var serviceConfig = config.Services.FirstOrDefault(e => e.Name.ToLower().Equals(ServiceName.ToLower()));
                if (serviceConfig?.Versions?.Count > 0)
                {
                    services.AddSingleton(serviceConfig);
                    return services.AddSwashbuckle(customOptions);
                }
            }
            return services;
        }
        public static IApplicationBuilder UseSwashbuckle(this IApplicationBuilder app, IApiVersionDescriptionProvider apiProvider)
        {
            var serviceConfig = app.ApplicationServices.GetService<SwaggerServiceConfig>();
            if (serviceConfig?.Versions?.Count > 0)
            {
                app.UseSwagger(c => c.SerializeAsV2 = true);

                app.UseSwaggerUI(c =>
                {
                    foreach (var description in apiProvider.ApiVersionDescriptions)
                    {
                        if (serviceConfig.Versions.Contains(description.ApiVersion.MajorVersion.ToString()))
                            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    }
                });
            }
            return app;
        }
    }
    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "CountryCode",
                In = ParameterLocation.Header,
                Required = false
            });
        }
    }
}
