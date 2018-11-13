﻿namespace WhMgr.Commands
{
    using System.Collections.Generic;

    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Localization;

    public class Dependencies
    {
        public SubscriptionManager SubscriptionManager { get; }

        public WhConfig WhConfig { get; }

        public Language<string, string, Dictionary<string, string>> Language { get; }

        public Dependencies(SubscriptionManager subMgr, WhConfig whConfig, Language<string, string, Dictionary<string, string>> language)
        {
            SubscriptionManager = subMgr;
            WhConfig = whConfig;
            Language = language;
        }
    }
}