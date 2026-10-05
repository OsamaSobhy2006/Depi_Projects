using System.Diagnostics;
using Stripe;
using Stripe.Checkout;

namespace LibraryManagementSystem.Services;

public class PaymentService
{
    private readonly SessionService _sessionService;

    public PaymentService()
    {
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
            throw new InvalidOperationException("Stripe secret key is not configured.");

        _sessionService = new SessionService();
    }

    public async Task<(string Url, string SessionId)> CreatePaymentSessionAsync(decimal amount, int bookId)
    {
        long amountInCents = (long)(amount * 100);

        var options = new SessionCreateOptions
        {
            Mode = "payment",

            SuccessUrl = "https://example.com/payment-success",
            CancelUrl = "https://example.com/payment-cancelled",

            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Quantity = 1,

                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        UnitAmount = amountInCents,

                        ProductData =
                            new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Library Late Fee - Book {bookId}"
                            }
                    }
                }
            }
        };

        var session = await _sessionService.CreateAsync(options);

        return (session.Url, session.Id);
    }

    public async Task<bool> IsPaymentSuccessfulAsync(string sessionId)
    {
        var session = await _sessionService.GetAsync(sessionId);

        return session.PaymentStatus == "paid";
    }

    public void OpenPaymentPage(string paymentUrl)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = paymentUrl,
            UseShellExecute = true
        });
    }
}