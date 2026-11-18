
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusOne.Domain.Entities;
using CampusOne.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;

namespace CampusOne.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private const decimal FinePerDay = 10.00m;
        private const int BorrowingDays = 14;
        private const string TestUserId = "student-123";

        public BorrowController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Calculate fine: 10 EGP for each started overdue day
        private static decimal CalculateFine(
            DateTime dueDate,
            DateTime currentDate)
        {
            if (currentDate <= dueDate)
                return 0;

            var overdueDays = (int)Math.Ceiling(
                (currentDate - dueDate).TotalDays
            );

            return overdueDays * FinePerDay;
        }

        // 1. Borrow Book
        [HttpPost("request/{bookId}")]
        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
                return NotFound("Book not found.");

            if (!book.IsAvailable)
                return BadRequest("Book is not available for borrowing.");

            var borrowDate = DateTime.Now;

            var request = new BorrowRequest
            {
                BookId = bookId,
                UserId = TestUserId,
                BorrowDate = borrowDate,
                DueDate = borrowDate.AddDays(BorrowingDays),
                Status = "Borrowed",
                FineAmount = 0,
                IsFinePaid = false
            };

            book.IsAvailable = false;

            _context.BorrowRequests.Add(request);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book borrowed successfully!",
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

        // 2. Get Latest Borrow Status for a Book
        [HttpGet("status/{bookId}")]
        public async Task<IActionResult> GetBorrowStatus(int bookId)
        {
            var request = await _context.BorrowRequests
                .Where(r =>
                    r.BookId == bookId &&
                    r.UserId == TestUserId)
                .OrderByDescending(r => r.BorrowDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return Ok(new
                {
                    isBorrowed = false,
                    id = (int?)null,
                    borrowDate = (DateTime?)null,
                    dueDate = (DateTime?)null,
                    returnDate = (DateTime?)null,
                    status = "Available",
                    fineAmount = 0m,
                    isFinePaid = false
                });
            }

            var isBorrowed = request.Status != "Returned";

            if (isBorrowed && DateTime.Now > request.DueDate)
            {
                request.Status = "Overdue";

                if (!request.IsFinePaid)
                {
                    request.FineAmount = CalculateFine(
                        request.DueDate,
                        DateTime.Now
                    );
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                isBorrowed,
                request.Id,
                borrowDate = request.BorrowDate,
                dueDate = request.DueDate,
                returnDate = request.ReturnDate,
                request.Status,
                fineAmount = request.FineAmount,
                request.IsFinePaid
            });
        }

        // 3. Get All Borrowed Books for Current Test User
        [HttpGet("my-borrowed-books")]
        public async Task<IActionResult> GetMyBorrowedBooks()
        {
            var requests = await _context.BorrowRequests
                .Include(r => r.Book)
                .Where(r => r.UserId == TestUserId)
                .OrderByDescending(r => r.BorrowDate)
                .ToListAsync();

            var currentDate = DateTime.Now;
            var hasChanges = false;

            foreach (var request in requests)
            {
                if (request.Status != "Returned" &&
                    currentDate > request.DueDate)
                {
                    request.Status = "Overdue";

                    if (!request.IsFinePaid)
                    {
                        request.FineAmount = CalculateFine(
                            request.DueDate,
                            currentDate
                        );
                    }

                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

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
            var request = await _context.BorrowRequests
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r =>
                    r.Id == borrowRequestId &&
                    r.UserId == TestUserId);

            if (request == null)
                return NotFound("Borrow request not found.");

            if (request.Status == "Returned")
                return BadRequest("Book has already been returned.");

            var returnDate = DateTime.Now;

            if (returnDate > request.DueDate && !request.IsFinePaid)
            {
                request.FineAmount = CalculateFine(
                    request.DueDate,
                    returnDate
                );
            }

            request.ReturnDate = returnDate;
            request.Status = "Returned";

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
            var request = await _context.BorrowRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == borrowRequestId &&
                    r.UserId == TestUserId);

            if (request == null)
                return NotFound("Borrow request not found.");

            if (request.IsFinePaid)
                return BadRequest("Fine has already been paid.");

            if (request.Status != "Returned")
            {
                if (DateTime.Now <= request.DueDate)
                    return BadRequest("There is no overdue fine yet.");

                request.Status = "Overdue";

                request.FineAmount = CalculateFine(
                    request.DueDate,
                    DateTime.Now
                );
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
                                request.FineAmount * 100
                            ),

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
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(
                HttpContext.Request.Body
            ).ReadToEndAsync();

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
                    throwOnApiVersionMismatch: false
                );
            }
            catch (StripeException ex)
            {
                Console.WriteLine(
                    "STRIPE WEBHOOK ERROR: " + ex.Message
                );

                return BadRequest(
                    "Invalid Stripe signature: " + ex.Message
                );
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
