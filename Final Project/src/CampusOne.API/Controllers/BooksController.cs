
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace CampusOne.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Trim().ToLower();
        }

        // GET: /Books/featured
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedBooks()
        {
            var books = await _context.Books
                .AsNoTracking()
                .ToListAsync();

            var uniqueBooks = books
                .GroupBy(b => new
                {
                    Title = Normalize(b.Title),
                    Author = Normalize(b.Author)
                })
                .Select(g =>
                {
                    var book = g.First();

                    return new
                    {
                        book.Id,
                        book.Title,
                        book.Author,
                        book.Cover,
                        IsAvailable = g.Any(b => b.IsAvailable)
                    };
                })
                .ToList();

            return Ok(uniqueBooks);
        }

        // GET: /Books/search?q=clean
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query cannot be empty.");

            q = q.Trim();

            var books = await _context.Books
                .AsNoTracking()
                .Where(b => b.Title.Contains(q) || b.Author.Contains(q))
                .ToListAsync();

            var uniqueBooks = books
                .GroupBy(b => new
                {
                    Title = Normalize(b.Title),
                    Author = Normalize(b.Author)
                })
                .Select(g =>
                {
                    var book = g.First();

                    return new
                    {
                        book.Id,
                        book.Title,
                        book.Author,
                        book.Cover,
                        IsAvailable = g.Any(b => b.IsAvailable)
                    };
                })
                .ToList();

            return Ok(uniqueBooks);
        }

        // GET: /Books/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound("Book not found.");

            var isAvailable = await _context.Books
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Title == book.Title &&
                    b.Author == book.Author &&
                    b.IsAvailable);

            return Ok(new
            {
                book.Id,
                book.Title,
                book.Author,
                book.Cover,
                book.Description,
                book.FileUrl,
                IsAvailable = isAvailable
            });
        }
    }
}
