using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe_NetCore.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Stripe_NetCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [AllowAnonymous]
    public class PaymentsController : Controller
    {
        public readonly IOptions<StripeOptions> options;
        private readonly IStripeClient stripeClient;

        public PaymentsController(IOptions<StripeOptions> options)
        {
            this.options = options;
            this.stripeClient = new StripeClient(this.options.Value.SecretKey);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    this.options.Value.WebhookSecret
                );
                Console.WriteLine($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something failed {ex}");
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                Console.WriteLine($"Session ID: {session.Id}");
                // Take some action based on session.
            }

            return Ok();
        }
    }
}
