using Api.Data.Db;
using Api.Models.User;
using Api.Filters.Validation;
using Api.Utils.Password;
using Api.Utils.JWT;
using Api.Service.AuthService;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterHandler)
             .AddEndpointFilter<DataAnnotationFilter<UserRegisterRequest>>();

        group.MapPost("/login", LoginHandler);

        group.MapGet("/me", GetMeHandler)
             .RequireAuthorization();

        group.MapPost("/logout", LogoutHandler);
    }


    private static async Task<IResult> RegisterHandler(UserRegisterRequest request, AppDbContext dbContext)
    {
        var exsitingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
        if (exsitingUser is not null) return Results.BadRequest("Username or Email already existed.");

        string hash_password = PasswordUtils.HashPassword(request.Password);
        User newUser = new User { Username = request.Username, HashPassword = hash_password, Email = request.Email };
        
        await dbContext.Users.AddAsync(newUser);
        await dbContext.SaveChangesAsync();
        return Results.Created();
    }

    private static async Task<IResult> LoginHandler(UserLoginRequest request, AppDbContext dbContext, JwtUtils jwtUtils, HttpContext httpContext)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is not null && PasswordUtils.VerifyPassword(request.Password, user.HashPassword))
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
        return Results.Unauthorized();
    }

    private static async Task<IResult> GetMeHandler(IAuthService authService)
    {
        var user = await authService.GetCurrentUserAsync();
        return Results.Ok(user);
    }

    private static IResult LogoutHandler(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("X-Access-Token", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Path = "/"
        });
        return Results.Ok(new { message = "Logged out successfully" });
    }
}