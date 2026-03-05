using Api.Data.Db;
using Microsoft.EntityFrameworkCore;
using Api.Models.User;
using Api.Filters.Validation;
using Api.Utils;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
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
app.Run();