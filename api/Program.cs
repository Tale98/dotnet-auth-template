using Api.Data.Db;
using Microsoft.EntityFrameworkCore;
using Api.Models.User;
using Api.Filters.Validation;
using Api.Utils.Password;
using Api.Utils.JWT;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Api.Service.AuthService;
using Api.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
// add db
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// jwt auth
builder.Services.AddScoped<JwtUtils>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,   
        ValidateAudience = false, 
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["X-Access-Token"];
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthorization();
// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "X-Access-Token",
        Description = "Authentication via HttpOnly Cookie"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("CookieAuth"), 
            new List<string>()
        }
    });
});

var app = builder.Build();
// global exception //
app.UseExceptionHandler(handler => {
    handler.Run(async context => {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception is UnauthorizedException) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = exception.Message });
        }
    });
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// apply auth middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.Run();