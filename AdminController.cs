using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const string AdminPassword = "Neev0917";

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/admin/stats?password=Neev0917
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] string password)
        {
            if (password != AdminPassword)
                return Unauthorized(new { message = "Invalid admin password" });

            var tasks = await _context.TodoItems.ToListAsync();

            var tasksByUser = tasks
                .GroupBy(t => t.UserId)
                .Select(g => new {
                    UserId = g.Key,
                    Total = g.Count(),
                    Done = g.Count(t => t.IsDone),
                    Pending = g.Count(t => !t.IsDone)
                })
                .OrderByDescending(u => u.Total)
                .ToList();

            return Ok(new {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.IsDone),
                TotalUsers = tasksByUser.Count,
                TasksByUser = tasksByUser
            });
        }
    }
}
