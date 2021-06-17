/*
namespace WhMgr.Commands
{
    using DSharpPlus.Interactivity;

    using WhMgr.Configuration;
    using WhMgr.Data.Subscriptions;
    using WhMgr.Net.Webhooks;
    using WhMgr.Osm;
    using WhMgr.Services;

    public class Dependencies
    {
        private readonly WhConfigHolder _configHolder;
        
        public InteractivityExtension Interactivity;

        public WebhookController Whm { get; }

        public SubscriptionProcessor SubscriptionProcessor { get; }

        public WhConfig WhConfig => _configHolder.Instance;

        public StripeService Stripe { get; }

        public OsmManager OsmManager { get; }

        public Dependencies(InteractivityExtension interactivity, WebhookController whm, SubscriptionProcessor subProcessor, WhConfigHolder whConfig, StripeService stripe)
        {
            Interactivity = interactivity;
            Whm = whm;
            SubscriptionProcessor = subProcessor;
            _configHolder = whConfig;
            Stripe = stripe;
            OsmManager = new OsmManager();
        }
    }
}
*/