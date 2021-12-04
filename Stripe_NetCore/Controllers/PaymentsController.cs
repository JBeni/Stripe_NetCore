using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Stripe_NetCore.Configuration;
using Stripe_NetCore.Models;

namespace Stripe_NetCore.Controllers
{
    //[ApiController]
    //[Route("api/[controller]")]
    //[Authorize]
    [AllowAnonymous]
    public class PaymentsController : Controller
    {
        public readonly IOptions<StripeOptions> options;
        private readonly IStripeClient client;

        public PaymentsController(IOptions<StripeOptions> options)
        {
            this.options = options;
            this.client = new StripeClient(this.options.Value.SecretKey);
        }

        [HttpGet("config")]
        public ConfigResponse GetConfig()
        {
            // return json: publishableKey (.env)
            return new ConfigResponse
            {
                PublishableKey = this.options.Value.PublishableKey,
            };
        }


        [HttpPost("create-checkout-session")]
        public ActionResult Create()
        {
            var domain = Environment.GetEnvironmentVariable("DOMAIN");
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        // Provide the exact Price ID (for example, pr_1234) of the product you want to sell
                        //Price = "{{PRICE_ID}}",

                        Name = "Stubborn Attachments",
                        Images = new List<string>
                        {
                            "https://i.imgur.com/EHyR2nP.png"
                        },

                        Currency = "usd",
                        Amount = 2000,
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/success.html",
                CancelUrl = domain + "/cancel.html",
            };
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }


        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest req)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = 1999,
                Currency = req.Currency,
                PaymentMethodTypes = new List<string>
            {
              req.PaymentMethodType,
            },
            };

            // If this is for an ACSS payment, we add payment_method_options to create
            // the Mandate.
            if (req.PaymentMethodType == "acss_debit")
            {
                options.PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                {
                    AcssDebit = new PaymentIntentPaymentMethodOptionsAcssDebitOptions
                    {
                        MandateOptions = new PaymentIntentPaymentMethodOptionsAcssDebitMandateOptionsOptions
                        {
                            PaymentSchedule = "sporadic",
                            TransactionType = "personal",
                        },
                    }
                };
            }

            var service = new PaymentIntentService(this.client);

            try
            {
                var paymentIntent = await service.CreateAsync(options);

                return Ok(new CreatePaymentIntentResponse
                {
                    ClientSecret = paymentIntent.ClientSecret,
                });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = new { message = e.StripeError.Message } });
            }
            catch (System.Exception)
            {
                return BadRequest(new { error = new { message = "unknown failure: 500" } });
            }
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
            catch (Exception e)
            {
                Console.WriteLine($"Something failed {e}");
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
