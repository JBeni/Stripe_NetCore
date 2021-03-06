using Newtonsoft.Json;

namespace Stripe_NetCore.Models
{
    public class CreatePaymentIntentRequest
    {
        [JsonProperty("paymentMethodType")]
        public string PaymentMethodType { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
