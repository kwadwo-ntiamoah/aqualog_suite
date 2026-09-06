using API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using API.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Infrastructure.Services;

namespace API
{
    public static class Startup
    {
        public static IServiceCollection RegisterStartup(this IServiceCollection services, IConfiguration config)
        {
            services.AddPersistence(config);
            services.AddJwtConfig(config);
            services.AddIdentityConfig();
            services.AddCorsConfig(config);
            services.AddServices();

            return services;
        }

        private static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration config)
        {
            var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy("AdminPortal", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            return services;
        }

        private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DbConnection");
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

            return services;
        }

        private static IServiceCollection AddJwtConfig(this IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
            {
                var key = config.GetValue<string>("JWT:Key")!;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = config.GetValue<string>("JWT:Issuer"),
                    ValidAudience = config.GetValue<string>("JWT:Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }

        private static IServiceCollection AddIdentityConfig(this IServiceCollection services)
        {
            services.AddIdentityCore<AppUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        private static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<TokenService>();
            services.AddScoped<AuthService>();
            services.AddScoped<ContainerService>();
            services.AddScoped<DriverService>();
            services.AddScoped<ShopService>();
            services.AddScoped<TransactionService>();
            services.AddScoped<AnalyticsService>();

            return services;
        }
    }
}