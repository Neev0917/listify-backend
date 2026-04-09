using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        // READ
        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var userId = GetUserId();
            var items = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .ToListAsync();
            return Ok(items);
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TodoApp item)
        {
            if (item == null || string.IsNullOrEmpty(item.Title))
                return BadRequest();

            item.UserId = GetUserId();
            _context.TodoItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // TOGGLE done status
        [HttpPut("{id}")]
        public async Task<IActionResult> Toggle(int id)
        {
            var userId = GetUserId();
            var item = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item == null) return NotFound();

            item.IsDone = !item.IsDone;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // UPDATE title
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTitle(int id, [FromBody] UpdateTitleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Title))
                return BadRequest();

            var userId = GetUserId();
            var item = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item == null) return NotFound();

            item.Title = request.Title;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var item = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item == null) return NotFound();

            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class UpdateTitleRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
