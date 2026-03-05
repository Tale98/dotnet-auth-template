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


app.MapPost("/auth/register", async (UserRegisterRequest request, AppDbContext dbContext) =>
{
    var exsitingUser = await dbContext.Users.FirstOrDefaultAsync((user) => user.Username == request.Username || user.Email == request.Email);
    if (exsitingUser is null)
    {
        // Hash password //
        string hash_password = PasswordUtils.HashPassword(request.Password);
        User newUser = new User
        {
            Username = request.Username,
            HashPassword = hash_password,
            Email = request.Email
        };
        await dbContext.Users.AddAsync(newUser);
        await dbContext.SaveChangesAsync();
        return Results.Created();
    }
    else
    {
        return Results.BadRequest($"Username or Email already existed.");
    }
}).AddEndpointFilter<DataAnnotationFilter<UserRegisterRequest>>();

app.MapPost("/auth/login", async (UserLoginRequest requet, AppDbContext appDbContext, JwtUtils jwtUtils, HttpContext httpContext) =>
{
    var user = await appDbContext.Users.FirstOrDefaultAsync((user) => user.Username == requet.Username);
    if (user is null)
    {

    }
    else
    {
        bool valid = PasswordUtils.VerifyPassword(requet.Password, user.HashPassword);
        if (valid)
        {
            string token = jwtUtils.GenerateUserToken(user);
            httpContext.Response.Cookies.Append("X-Access-Token", token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            });
            return Results.Ok(new { message = "Login successful" });
        }
    }
    return Results.Unauthorized();
});
app.MapGet("/auth/me/", async (AppDbContext dbContext, HttpContext httpContext, JwtUtils jwtUtils, IAuthService authService) =>
{
    var user = await authService.GetCurrentUserAsync();
    return Results.Ok(user);
}).RequireAuthorization();

app.MapPost("/auth/logout", (HttpContext httpContext) =>
{
    httpContext.Response.Cookies.Delete("X-Access-Token", new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = true,
        Path = "/"
    });

    return Results.Ok(new { message = "Logged out successfully" });
});

app.Run();