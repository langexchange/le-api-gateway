using LE.ApiGateway.Constants;
using LE.ApiGateway.Enums;
using LE.ApiGateway.JwtHelpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LE.ApiGateway.Extensions
{
    public static class AuthorizationExtensions
    {
        private const string Authorization = "Authorization";
        private static string GetAuthorizationValue(IHeaderDictionary Headers)
        {
            var key = Authorization;
            if (!Headers.ContainsKey(key)) return null;
            if (!Headers.TryGetValue(key, out var Value)) return null;
            if (Value.Count < 1) return null;
            var result = Value.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(result)) return null;
            return result;
        }
        private static void ModifyPayloadJwt(IHeaderDictionary Headers, IConfiguration Configuration)
        {
            var rubyToken = GetAuthorizationValue(Headers);
            if (string.IsNullOrWhiteSpace(rubyToken))
                return;
            var newToken = string.Empty;
            var accessToken = rubyToken.Split(" ")[1];
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(accessToken) as JwtSecurityToken;

            var claimRole = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimsConstant.ADMIN_ID ||
                                                                    claim.Type == ClaimsConstant.DRIVER_ID ||
                                                                    claim.Type == ClaimsConstant.CUSTOMER_ID);
            var tokenBuilder = new JwtTokenBuilder()
                                   .AddSecurityKey(JwtSecurityKey.Create(Configuration.GetValue<string>("JwtSettings:Secret")))
                                   .AddIssuer(Configuration.GetValue<string>("JwtSettings:IdentityUrl"))
                                   .AddAudience(Configuration.GetValue<string>("JwtSettings:IdentityUrl"))
                                   .AddClaims(jwtToken.Claims.ToList());
            switch (claimRole.Type)
            {
                case ClaimsConstant.ADMIN_ID:
                    tokenBuilder.AddRole(UserRole.Admin.ToString());
                    break;
                default:
                    tokenBuilder.AddRole(UserRole.Customer.ToString());
                    break;
            }

            Headers[Authorization] = $"{JwtBearerDefaults.AuthenticationScheme} {tokenBuilder.Build().Value}";
        }
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services, IConfiguration Configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration.GetValue<string>("JwtSettings:IdentityUrl"),
                        ValidAudience = Configuration.GetValue<string>("JwtSettings:IdentityUrl"),
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetValue<string>("JwtSettings:Secret")))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var authorizationValue = GetAuthorizationValue(context.HttpContext.Request.Headers);
                            var isNotContainBearer = authorizationValue != null && !authorizationValue.Contains("Bearer");
                            if (isNotContainBearer)
                            {
                                var authorization = string.Format("{0} {1}", JwtBearerDefaults.AuthenticationScheme, authorizationValue);
                                context.HttpContext.Request.Headers.Remove("Authorization");
                                context.HttpContext.Request.Headers.Add("Authorization", authorization);
                            }
                            if (string.IsNullOrEmpty(accessToken) || GetAuthorizationValue(context.HttpContext.Request.Headers) != null) return Task.CompletedTask;
                            var path = context.HttpContext.Request.Path.Value ?? "";
                            if (path.StartsWith("/realtime/"))
                            {
                                try
                                {
                                    context.Token = accessToken;
                                    var authorization = string.Format("{0} {1}", JwtBearerDefaults.AuthenticationScheme, accessToken);
                                    context.HttpContext.Request.Headers.Add("Authorization", authorization);
                                }
                                catch { }
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            ModifyPayloadJwt(context.HttpContext.Request.Headers, Configuration);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            context.NoResult();
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            context.Response.ContentType = "text/plain";
                            return context.Response.WriteAsync(context.Exception.ToString());
                        }
                    };
                });
            return services;
        }

        public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.Use((ctx, next) =>
            {
                if (ctx.Response.StatusCode != (int)HttpStatusCode.OK)
                    return Task.CompletedTask;
                if (!ctx.User.Identity.IsAuthenticated && GetAuthorizationValue(ctx.Request.Headers) != null)
                {
                    ctx.Response.Clear();
                    ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                }
                return next.Invoke();
            });
            return app;
        }
    }
}
