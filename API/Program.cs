using API;
using API.Infrastructure.Persistence;
using API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.RegisterStartup(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("AquaLog API")
            .WithTheme(ScalarTheme.Moon);
    });
}

// Render (and most PaaS hosts) terminate TLS at their edge and forward
// plain HTTP to the container, tagging the original scheme via
// X-Forwarded-Proto. Without this, UseHttpsRedirection() can't see that the
// original request was already HTTPS and would send the client into a
// redirect loop.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// Render's edge proxy IP isn't fixed/known ahead of time, unlike the
// default loopback-only trust list — clear it so forwarded headers from
// the platform's proxy are actually honored.
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

app.UseCors("AdminPortal");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Firestore is schemaless — no migration step needed, just seed the default
// roles/users (idempotent: IdentitySeeder skips anything that already exists).
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await IdentitySeeder.CreateDefaultUsers(userManager, roleManager, builder.Configuration);
}

app.Run();
