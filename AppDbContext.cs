using Microsoft.EntityFrameworkCore;

namespace WebApplication3.Models
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
		public DbSet<TodoApp> TodoItems { get; set; }
	}
}