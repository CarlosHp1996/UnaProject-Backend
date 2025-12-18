using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnaProject.Application.Services;
using UnaProject.Application.Services.Interfaces;

namespace UnaProject.Application.Extensions
{
    public static class AbacatePayServiceExtensions
    {
        public static IServiceCollection AddAbacatePay(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure options
            services.Configure<AbacatePayOptions>(configuration.GetSection(AbacatePayOptions.SectionName));

            // Configure HttpClient for AbacatePay
            services.AddHttpClient("AbacatePay", (serviceProvider, client) =>
            {
                var options = configuration.GetSection(AbacatePayOptions.SectionName).Get<AbacatePayOptions>()
                              ?? new AbacatePayOptions();

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                var apiKey = options.GetApiKey();
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "UnaProject/1.0");
            });

            // Register service
            services.AddScoped<IAbacatePayService, AbacatePayService>();

            return services;
        }
    }
}