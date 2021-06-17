namespace WhMgr.Extensions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;
    using DSharpPlus.Interactivity.Extensions;

    static class InteractivityExtensions
    {
        public static async Task<int> GetSubscriptionTypeSelection(this CommandContext ctx)
        {
            var msg = $@"
Select the type of subscription to create:
:one: Pokemon Subscription
:two: PvP Subscription
:three: Raid Subscription
:four: Quest Subscription
:five: Invasion Subscription
:six: Gym Subscription
";
            var message = ctx.RespondEmbed(msg, DiscordColor.Blurple).GetAwaiter().GetResult().FirstOrDefault();
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":one:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":two:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":three:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":four:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":five:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":six:"));

            var interactivity = ctx.Client.GetInteractivity();
            // TODO: Configurable subscription timeout
            var resultReact = await interactivity.WaitForReactionAsync(x => !string.IsNullOrEmpty(x.Emoji?.Name), message, ctx.User, TimeSpan.FromMinutes(3));
            if (resultReact.Result == null)
            {
                await ctx.RespondEmbed($"Invalid result", DiscordColor.Red);
                return 0;
            }

            await message.DeleteAsync();
            switch (resultReact.Result.Emoji.Name.ToLower())
            {
                case "1⃣": return 1;
                case "2⃣": return 2;
                case "3⃣": return 3;
                case "4⃣": return 4;
                case "5⃣": return 5;
                case "6⃣": return 6;
                default: return 0;
            }
        }

        public static async Task<string> WaitForUserChoice(this CommandContext ctx, bool allowNull = false)
        {
            var interactivity = ctx.Client.GetInteractivity();
            // TODO: Configurable subscription timeout
            var result = await interactivity.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id && (allowNull && string.IsNullOrEmpty(x.Content)) || (!allowNull && !string.IsNullOrEmpty(x.Content)), TimeSpan.FromMinutes(3));
            var content = result.Result.Content;
            try
            {
                // Bot can't delete user messages in DMs
                await result.Result.DeleteAsync();
            }
            catch { }
            return content;
        }
    }
}