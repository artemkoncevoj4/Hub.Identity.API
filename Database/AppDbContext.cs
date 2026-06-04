using Microsoft.EntityFrameworkCore;

namespace Identity.Database;

public class AppDbContext : DbContext
{
    public DbSet<Users> Users {get;set;} = null!;
    public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
            
        }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}