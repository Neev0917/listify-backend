namespace WebApplication3.Models
{
	public class TodoApp
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public bool IsDone { get; set; } = false;
		public string UserId { get; set; } = string.Empty; // Supabase user ID
	}
}