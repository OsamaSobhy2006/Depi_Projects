
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusOne.Domain.Entities;
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

        // GET: api/books/featured
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedBooks()
        {
            var books = await _context.Books
                .AsNoTracking()
                .ToListAsync();

            // Remove duplicate books by title and author
            var uniqueBooks = books
                .GroupBy(b => new
                {
                    Title = b.Title.Trim().ToLower(),
                    Author = b.Author.Trim().ToLower()
                })
                .Select(g => g.First())
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Cover,
                    b.IsAvailable
                })
                .ToList();

            return Ok(uniqueBooks);
        }

        // GET: api/books/search?q=clean
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search query cannot be empty.");
            }

            q = q.Trim();

            var books = await _context.Books
                .AsNoTracking()
                .Where(b =>
                    b.Title.Contains(q) ||
                    b.Author.Contains(q))
                .ToListAsync();

            // Remove duplicate search results
            var uniqueBooks = books
                .GroupBy(b => new
                {
                    Title = b.Title.Trim().ToLower(),
                    Author = b.Author.Trim().ToLower()
                })
                .Select(g => g.First())
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Cover,
                    b.IsAvailable
                })
                .ToList();

            return Ok(uniqueBooks);
        }


        // GET: api/books/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Cover,
                    b.Description,
                    b.FileUrl,
                    b.IsAvailable
                })
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            return Ok(book);
        }

    }
}
