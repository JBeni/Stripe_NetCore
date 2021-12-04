using Newtonsoft.Json;

namespace Stripe_NetCore.Models
{
    public class ConfigResponse
    {
        [JsonProperty("publishableKey")]
        public string PublishableKey { get; set; }
    }
}
