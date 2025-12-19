namespace Final_VS1.Services
{
    public interface IGoogleAnalyticsService
    {
        string GetMeasurementId();
        bool IsEnabled();
        string GenerateEcommerceScript(string eventName, object data);
    }

    public class GoogleAnalyticsService : IGoogleAnalyticsService
    {
        private readonly IConfiguration _configuration;

        public GoogleAnalyticsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetMeasurementId()
        {
            return _configuration["GoogleAnalytics:MeasurementId"] ?? string.Empty;
        }

        public bool IsEnabled()
        {
            var enabled = _configuration["GoogleAnalytics:Enabled"];
            return !string.IsNullOrEmpty(enabled) && enabled.ToLower() == "true";
        }

        public string GenerateEcommerceScript(string eventName, object data)
        {
            if (!IsEnabled()) return string.Empty;

            var json = System.Text.Json.JsonSerializer.Serialize(data);
            return $"gtag('event', '{eventName}', {json});";
        }
    }
}
