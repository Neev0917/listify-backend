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

		// Supabase puts the user ID in the "sub" claim
		private string GetUserId()
		{
			return User.FindFirstValue("sub")
				?? User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? string.Empty;
		}

		[HttpGet]
		public async Task<IActionResult> GetItems()
		{
			var userId = GetUserId();
			Console.WriteLine($"GET tasks for user: {userId}");

			var items = await _context.TodoItems
				.Where(t => t.UserId == userId)
				.ToListAsync();
			return Ok(items);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] TodoApp item)
		{
			if (item == null || string.IsNullOrEmpty(item.Title))
				return BadRequest();

			item.UserId = GetUserId();
			Console.WriteLine($"CREATE task for user: {item.UserId}");

			_context.TodoItems.Add(item);
			await _context.SaveChangesAsync();
			return Ok(item);
		}

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
}