
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusOne.Domain.Entities;
using CampusOne.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Authorization;

namespace CampusOne.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class BorrowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private const decimal FinePerDay = 50.00m;
        private const int BorrowingDays = 2;

        public BorrowController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? string.Empty;
        }

        // Calculate fine based on calendar days
        private static decimal CalculateFine(
            DateTime dueDate,
            DateTime currentDate)
        {
            var overdueDays = (currentDate.Date - dueDate.Date).Days;

            if (overdueDays <= 0)
                return 0;

            return overdueDays * FinePerDay;
        }

        // 1. Borrow Book
        [HttpPost("request/{bookId}")]
        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            // Serializable transaction protects availability
            // when multiple users try to borrow at the same time.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var selectedBook = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (selectedBook == null)
                return NotFound("Book not found.");

            // Do not allow the same user to borrow
            // another copy of the same title simultaneously.
            var alreadyBorrowed = await _context.BorrowRequests
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.Status != "Returned" &&
                    r.Book != null &&
                    r.Book.Title == selectedBook.Title &&
                    r.Book.Author == selectedBook.Author);

            if (alreadyBorrowed)
            {
                return BadRequest(new
                {
                    message = "You have already borrowed this book."
                });
            }

            // Find any available physical copy of the same book.
            var availableCopy = await _context.Books
                .Where(b =>
                    b.Title == selectedBook.Title &&
                    b.Author == selectedBook.Author &&
                    b.IsAvailable)
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync();

            if (availableCopy == null)
            {
                return BadRequest(new
                {
                    message = "Book is not available for borrowing."
                });
            }

            var borrowDate = DateTime.Now;

            var request = new BorrowRequest
            {
                BookId = availableCopy.Id,
                UserId = userId,
                BorrowDate = borrowDate,
                DueDate = borrowDate.AddDays(BorrowingDays),
                Status = "Borrowed",
                FineAmount = 0,
                IsFinePaid = false
            };

            availableCopy.IsAvailable = false;

            _context.BorrowRequests.Add(request);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Book borrowed successfully for 2 days.",
                request = new
                {
                    request.Id,
                    request.BookId,
                    request.BorrowDate,
                    request.DueDate,
                    request.Status,
                    request.FineAmount,
                    request.IsFinePaid
                }
            });
        }

        // 2. Get Global Availability + Current User Borrow Status
        [HttpGet("status/{bookId}")]
        public async Task<IActionResult> GetBorrowStatus(int bookId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound("Book not found.");

            // Availability is based on ALL copies of this title.
            var bookIsAvailable = await _context.Books
                .AnyAsync(b =>
                    b.Title == book.Title &&
                    b.Author == book.Author &&
                    b.IsAvailable);

            // Retrieve only this user's records for all copies
            // of the same title. Prefer an active record.
            var request = await _context.BorrowRequests
                .Where(r =>
                    r.UserId == userId &&
                    r.Book != null &&
                    r.Book.Title == book.Title &&
                    r.Book.Author == book.Author)
                .OrderBy(r => r.Status == "Returned" ? 1 : 0)
                .ThenByDescending(r => r.BorrowDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return Ok(new
                {
                    bookIsAvailable,
                    isBorrowed = false,
                    id = (int?)null,
                    borrowDate = (DateTime?)null,
                    dueDate = (DateTime?)null,
                    returnDate = (DateTime?)null,
                    status = bookIsAvailable
                        ? "Available"
                        : "CurrentlyBorrowed",
                    fineAmount = 0m,
                    isFinePaid = false
                });
            }

            var isBorrowed = request.Status != "Returned";

            // Calculate overdue fine for this user only.
            if (isBorrowed &&
                DateTime.Now.Date > request.DueDate.Date)
            {
                request.Status = "Overdue";

                if (!request.IsFinePaid)
                {
                    request.FineAmount = CalculateFine(
                        request.DueDate,
                        DateTime.Now);
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                bookIsAvailable,
                isBorrowed,
                request.Id,
                borrowDate = request.BorrowDate,
                dueDate = request.DueDate,
                returnDate = request.ReturnDate,
                status = request.Status,
                fineAmount = request.FineAmount,
                isFinePaid = request.IsFinePaid
            });
        }

        // 3. Get All Borrowing Records for Current User
        [HttpGet("my-borrowed-books")]
        public async Task<IActionResult> GetMyBorrowedBooks()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            var requests = await _context.BorrowRequests
                .Include(r => r.Book)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.BorrowDate)
                .ToListAsync();

            var currentDate = DateTime.Now;
            var hasChanges = false;

            foreach (var request in requests)
            {
                if (request.Status != "Returned" &&
                    currentDate.Date > request.DueDate.Date)
                {
                    request.Status = "Overdue";

                    if (!request.IsFinePaid)
                    {
                        request.FineAmount = CalculateFine(
                            request.DueDate,
                            currentDate);
                    }

                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _context.SaveChangesAsync();

            var result = requests.Select(request => new
            {
                borrowRequestId = request.Id,
                bookId = request.BookId,
                title = request.Book?.Title ?? "Unknown Book",
                author = request.Book?.Author ?? "Unknown Author",
                cover = request.Book?.Cover ?? "navy",
                description = request.Book?.Description,
                fileUrl = request.Book?.FileUrl,
                borrowDate = request.BorrowDate,
                dueDate = request.DueDate,
                returnDate = request.ReturnDate,
                status = request.Status,
                fineAmount = request.FineAmount,
                isFinePaid = request.IsFinePaid,
                paidAt = request.PaidAt
            });

            return Ok(result);
        }

        // 4. Return Book
        [HttpPost("return/{borrowRequestId}")]
        public async Task<IActionResult> ReturnBook(int borrowRequestId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            var request = await _context.BorrowRequests
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r =>
                    r.Id == borrowRequestId &&
                    r.UserId == userId);

            if (request == null)
                return NotFound("Borrow request not found.");

            if (request.Status == "Returned")
                return BadRequest("Book has already been returned.");

            var returnDate = DateTime.Now;

            // Save the fine before marking the book as returned.
            if (returnDate.Date > request.DueDate.Date &&
                !request.IsFinePaid)
            {
                request.FineAmount = CalculateFine(
                    request.DueDate,
                    returnDate);
            }

            request.ReturnDate = returnDate;
            request.Status = "Returned";

            // Make ONLY the borrowed physical copy available again.
            if (request.Book != null)
            {
                request.Book.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book returned successfully!",
                request.Id,
                request.BookId,
                request.ReturnDate,
                request.Status,
                request.FineAmount,
                request.IsFinePaid
            });
        }

        // 5. Create Stripe Checkout Session
        [HttpPost("create-checkout-session/{borrowRequestId}")]
        public async Task<IActionResult> CreateCheckoutSession(
            int borrowRequestId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            // A user can only pay their own fine.
            var request = await _context.BorrowRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == borrowRequestId &&
                    r.UserId == userId);

            if (request == null)
                return NotFound("Borrow request not found.");

            if (request.IsFinePaid)
                return BadRequest("Fine has already been paid.");

            // If the book has not been returned,
            // calculate the current overdue fine.
            if (request.Status != "Returned")
            {
                if (DateTime.Now.Date <= request.DueDate.Date)
                    return BadRequest("There is no overdue fine yet.");

                request.Status = "Overdue";

                request.FineAmount = CalculateFine(
                    request.DueDate,
                    DateTime.Now);
            }

            if (request.FineAmount <= 0)
                return BadRequest("Fine amount must be greater than zero.");

            var secretKey = _configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                return StatusCode(500, "Stripe secret key is missing.");

            StripeConfiguration.ApiKey = secretKey;

            var options = new SessionCreateOptions
            {
                Mode = "payment",

                SuccessUrl =
                    $"http://localhost:5173/library/book/{request.BookId}?payment=success",

                CancelUrl =
                    $"http://localhost:5173/library/book/{request.BookId}?payment=cancelled",

                ClientReferenceId = request.Id.ToString(),

                Metadata = new Dictionary<string, string>
                {
                    { "BorrowRequestId", request.Id.ToString() }
                },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "egp",

                            UnitAmount = (long)(
                                request.FineAmount * 100),

                            ProductData =
                                new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Library Late Return Fine"
                                }
                        },

                        Quantity = 1
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            request.StripeCheckoutSessionId = session.Id;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                checkoutUrl = session.Url,
                sessionId = session.Id
            });
        }

        // 6. Stripe Webhook
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(
                HttpContext.Request.Body).ReadToEndAsync();

            var stripeSignature = Request.Headers["Stripe-Signature"];

            var webhookSecret =
                _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(webhookSecret))
                return BadRequest("Stripe webhook secret is missing.");

            if (string.IsNullOrWhiteSpace(stripeSignature))
                return BadRequest("Stripe signature is missing.");

            Stripe.Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    webhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                Console.WriteLine(
                    "STRIPE WEBHOOK ERROR: " + ex.Message);

                return BadRequest(
                    "Invalid Stripe signature: " + ex.Message);
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session == null || session.PaymentStatus != "paid")
                    return Ok();

                var borrowRequestId = session.ClientReferenceId;

                if (!int.TryParse(borrowRequestId, out var id))
                    return BadRequest("Invalid borrow request ID.");

                var request = await _context.BorrowRequests
                    .FirstOrDefaultAsync(r =>
                        r.Id == id &&
                        r.StripeCheckoutSessionId == session.Id);

                if (request == null)
                    return Ok();

                if (!request.IsFinePaid)
                {
                    request.IsFinePaid = true;
                    request.PaidAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
            }

            return Ok();
        }
    }
}
