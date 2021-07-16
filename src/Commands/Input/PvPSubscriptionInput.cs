namespace WhMgr.Commands.Input
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;

    using WhMgr.Data.Subscriptions.Models;
    using WhMgr.Extensions;
    using WhMgr.Localization;

    internal class PvPSubscriptionInput : SubscriptionInput
    {
        private readonly CommandContext _context;

        public PvPSubscriptionInput(CommandContext ctx) : base(ctx)
        {
            _context = ctx;
        }

        public async Task<PvPLeague> GetLeagueResult()
        {
            var message = (await _context.RespondEmbed($"Enter the PvP league type to use (i.e `Great` or `Ultra`):", DiscordColor.Blurple)).FirstOrDefault();
            var league = await _context.WaitForUserChoice();

            var pvpLeague = string.Compare(league, "great", true) == 0 ?
                PvPLeague.Great :
                string.Compare(league, "ultra", true) == 0 ?
                    PvPLeague.Ultra :
                    string.Compare(league, "master", true) == 0 ?
                        PvPLeague.Master :
                        PvPLeague.Other;

            if (pvpLeague == PvPLeague.Other)
            {
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_LEAGUE").FormatText(new
                {
                    author = _context.User.Username,
                    league = league,
                }), DiscordColor.Red);
                return PvPLeague.Other;
            }

            await message.DeleteAsync();

            return pvpLeague;
        }

        public async Task<int> GetRankResult()
        {
            var message = (await _context.RespondEmbed($"Enter the minimum PvP rank to receive (i.e `1`, `3`, `25`):", DiscordColor.Blurple)).FirstOrDefault();
            var minRank = await _context.WaitForUserChoice();
            await message.DeleteAsync();

            if (!int.TryParse(minRank, out var minimumRank))
                return 3;

            //You may only subscribe to the top 100 or higher rank.
            if (minimumRank < Strings.MinimumRank || minimumRank > Strings.MaximumRank)
            {
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_RANK_RANGE").FormatText(new
                {
                    author = _context.User.Username,
                    rank = minRank,
                }), DiscordColor.Red);
                return 3;
            }
            return minimumRank;
        }

        public async Task<double> GetPercentResult()
        {
            var message = (await _context.RespondEmbed($"Enter the minimum PvP rank percent to receive (i.e `90`, `99`, '100'):", DiscordColor.Blurple)).FirstOrDefault();
            var minPercent = await _context.WaitForUserChoice();
            await message.DeleteAsync();

            if (!double.TryParse(minPercent, out var minimumPercent))
                return 98;

            if (minimumPercent < Strings.MinimumPercent || minimumPercent > Strings.MaximumPercent)
            {
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_PERCENT_RANGE").FormatText(new
                {
                    author = _context.User.Username,
                    percent = minimumPercent,
                }), DiscordColor.Red);
                return 98;
            }
            return minimumPercent;
        }
    }
}