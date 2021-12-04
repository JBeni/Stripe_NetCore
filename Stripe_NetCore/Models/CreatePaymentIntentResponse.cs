using Newtonsoft.Json;

namespace Stripe_NetCore.Models
{
    public class CreatePaymentIntentResponse
    {
        [JsonProperty("clientSecret")]
        public string ClientSecret { get; set; }
    }
}
