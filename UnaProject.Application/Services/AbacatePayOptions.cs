namespace UnaProject.Application.Services
{
    public class AbacatePayOptions
    {
        public const string SectionName = "AbacatePay";
        public string BaseUrl { get; set; } = "https://api.abacatepay.com";
        public string? DevApiKey { get; set; }
        public string? ProductionApiKey { get; set; }
        public bool IsDevMode { get; set; } = true;
        public string? WebhookSecret { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public string? WebhookCallbackUrl { get; set; }
        public string? GetApiKey() => IsDevMode ? DevApiKey : ProductionApiKey;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(BaseUrl))
                return false;

            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
                return false;

            return true;
        }
    }
}