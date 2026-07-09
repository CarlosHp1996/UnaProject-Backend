namespace UnaProject.Application.Services
{
    public class StripeOptions
    {
        public const string SectionName = "Stripe";

        public string? STRIPE_PUBLIC_KEY { get; set; }
        public string? STRIPE_SECRET_KEY { get; set; }
        public string? STRIPE_WEBHOOK_SECRET { get; set; }
    }
}
