using System.ComponentModel.DataAnnotations;

namespace UnaProject.Web.Configuration
{
    public class AbacatePayOptions
    {
        public const string SectionName = "AbacatePay";

        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string WebhookSecret { get; set; } = string.Empty;

        [Required]
        public string Environment { get; set; } = string.Empty;

        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 30;

        [Range(1, 10)]
        public int RetryAttempts { get; set; } = 3;

        public bool EnableLogging { get; set; } = true;

        public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
        public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

        public void Validate()
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(this);

            if (!Validator.TryValidateObject(this, context, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
                throw new InvalidOperationException($"AbacatePay configuration is invalid: {errors}");
            }

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException($"AbacatePay BaseUrl '{BaseUrl}' is not a valid URL.");
        }
    }
}