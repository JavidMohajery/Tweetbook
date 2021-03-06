using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Text;
using Tweetbook.Options;
using Tweetbook.Services;

namespace Tweetbook.Installers
{
    public class MvcInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = new JwtSettings();
            configuration.Bind(nameof(JwtSettings), jwtSettings);
            services.AddSingleton(jwtSettings);

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddMvc(options => { options.EnableEndpointRouting = false; }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false,
                ValidateLifetime = true
            };
            services.AddSingleton(tokenValidationParameters);
            services.AddAuthentication(
                    option =>
                    {
                        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddJwtBearer(x =>
                {
                    x.SaveToken = true;
                    x.TokenValidationParameters = tokenValidationParameters;
                });
            services.AddAuthorization();
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "Tweetbook API", Version = "v1" });
                var security = new Dictionary<string, IEnumerable<string>>()
                {
                    {"Bearer", new string[0]}
                };
                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Jwt Authorization header using bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                x.AddSecurityRequirement(new OpenApiSecurityRequirement 
                {
                    {new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    }, new List<string>{} }
                });
            });
        }
    }
}