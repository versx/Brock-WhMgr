namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Extensions;

    using WhMgr.Configuration;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Net.Webhooks;

    public class Areas : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("AREAS", Program.LogLevel);

        private readonly WhConfigHolder _config;
        private readonly WebhookController _whm;

        public Areas(WhConfigHolder config, WebhookController whm)
        {
            _config = config;
            _whm = whm;
        }

        [
            Command("areas"),
            Description("Shows a list of areas")
        ]
        public async Task SendPaginated(CommandContext ctx)
        {
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));
            if (!_config.Instance.Servers.ContainsKey(guildId))
                return;

            var server = _config.Instance.Servers[guildId];
            var geofences = _whm.GetServerGeofences(guildId);
            var areas = geofences.Select(geofence => geofence.Name).OrderBy(Name => Name).ToList();

            var interactivity = ctx.Client.GetInteractivity();
            var pages = new List<Page>();
            int pagelength = 0;
            var psb = new StringBuilder();
            int linesThisPage = 0;
            int num = 1;
            var title = string.Format("Page {0}", (object)num);
            foreach (var line in areas.Select(area => $"- {area}\n"))
            {
                var length = line.Length;
                var wouldGoOver = length + pagelength > 2000;

                if (wouldGoOver || linesThisPage >= 25)
                {
                    title = string.Format("Page {0}", (object)num);
                    var eb = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Blue,
                        Title = title,
                        Description = psb.ToString(),
                    };
                    pages.Add(new Page { Embed = eb });
                    psb.Clear();
                    pagelength = 0;
                    linesThisPage = 0;
                    ++num;
                }

                psb.Append(line);
                pagelength += length;
                linesThisPage++;
            }

            if (psb.Length > 0)
            {
                title = string.Format("Page {0}", (object)num);
                var eb = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = title,
                    Description = psb.ToString(),

                };
                pages.Add(new Page { Embed = eb });
            }
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: TimeSpan.FromMinutes(5));
        }
    }
}