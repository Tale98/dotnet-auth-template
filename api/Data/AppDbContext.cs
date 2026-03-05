using Microsoft.EntityFrameworkCore;
using Api.Models.User;

namespace Api.Data.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
}