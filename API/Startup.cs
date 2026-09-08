using API.Infrastructure.Identity;
using API.Infrastructure.Persistence;
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
            services.AddFirestore(config);
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
                    // WithOrigins() treats "*" as a literal origin string to
                    // match, not a wildcard — it would never match a real
                    // browser Origin header, silently blocking everything.
                    // AllowAnyOrigin() is the actual wildcard. Safe to combine
                    // with AllowAnyHeader/AllowAnyMethod here since this API
                    // never uses AllowCredentials (auth is a Bearer token in
                    // the Authorization header, not cookies).
                    if (allowedOrigins.Contains("*"))
                    {
                        policy.AllowAnyOrigin();
                    }
                    else
                    {
                        policy.WithOrigins(allowedOrigins);
                    }

                    policy.AllowAnyHeader().AllowAnyMethod();
                });
            });

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

        // No EF Core store here — Firestore has no EF Core provider, so
        // Identity is backed by the hand-written FirestoreUserStore/
        // FirestoreRoleStore instead (see Infrastructure/Identity/). Those
        // implement only the store interfaces this app's UserManager usage
        // actually needs (password, role, security-stamp — no email/claims/
        // lockout, since AuthService never touches those).
        private static IServiceCollection AddIdentityConfig(this IServiceCollection services)
        {
            services.AddIdentityCore<AppUser>()
                .AddRoles<IdentityRole>()
                .AddUserStore<FirestoreUserStore>()
                .AddRoleStore<FirestoreRoleStore>()
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
