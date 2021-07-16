namespace WhMgr.Commands
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using ServiceStack.OrmLite;

    using WhMgr.Extensions;
    using WhMgr.Localization;

    [
        Group("gyms"),
        Aliases("g"),
        Description("Gym management commands."),
        Hidden,
        RequirePermissions(Permissions.KickMembers)
    ]
    public class Gyms
    {
        //private static readonly IEventLogger _logger = EventLogger.GetLogger("GYMS", Program.LogLevel);
        private readonly Dependencies _dep;

        public Gyms(Dependencies dep)
        {
            _dep = dep;
        }

        [
            Command("convert"),
            Description("Deletes Pokestops that have converted to Gyms from the database.")
        ]
        public async Task ConvertedPokestopsToGymsAsync(CommandContext ctx,
            [Description("Real or dry run check (y/n)")] string yesNo = "y")
        {
            using (var db = Data.DataAccessLayer.CreateFactory(_dep.WhConfig.Database.Scanner.ToString()).Open())
            {
                //Select query where ids match for pokestops and gyms
                var convertedGyms = db.Select<Data.Models.Pokestop>(Strings.SQL_SELECT_CONVERTED_POKESTOPS);
                if (convertedGyms?.Count == 0)
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("GYM_NO_POKESTOPS_CONVERTED").FormatText(new { author = ctx.User.Username }), DiscordColor.Yellow);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(Translator.Instance.Translate("GYM_POKESTOPS_EMBED_TITLE"));
                sb.AppendLine();
                var eb = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Blurple,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        IconUrl = ctx.Guild?.IconUrl,
                        Text = $"{ctx.Guild?.Name ?? Strings.Creator} | {DateTime.Now}"
                    }
                };
                for (var i = 0; i < convertedGyms.Count; i++)
                {
                    var gym = convertedGyms[i];
                    var name = string.IsNullOrEmpty(gym.Name) ? Translator.Instance.Translate("GYM_UNKNOWN_NAME") : gym.Name;
                    var imageUrl = string.IsNullOrEmpty(gym.Url) ? Translator.Instance.Translate("GYM_UNKNOWN_IMAGE") : gym.Url;
                    var locationUrl = string.Format(Strings.GoogleMaps, gym.Latitude, gym.Longitude);
                    //eb.AddField($"{name} ({gym.Latitude},{gym.Longitude})", url);
                    sb.AppendLine(Translator.Instance.Translate("GYM_NAME").FormatText(new { name = name }));
                    sb.AppendLine(Translator.Instance.Translate("GYM_DIRECTIONS_IMAGE_LINK").FormatText(new
                    {
                        location_url = locationUrl,
                        image_url = imageUrl,
                    }));
                }
                eb.Description = sb.ToString();
                await ctx.RespondAsync(embed: eb);

                if (Regex.IsMatch(yesNo, DiscordExtensions.YesRegex))
                {
                    //Gyms are updated where the ids match.
                    var rowsAffected = db.ExecuteNonQuery(Strings.SQL_UPDATE_CONVERTED_POKESTOPS);
                    await ctx.RespondEmbed(Translator.Instance.Translate("GYM_POKESTOPS_CONVERTED").FormatText(new
                    {
                        author = ctx.User.Username,
                        amount = rowsAffected.ToString("N0"),
                    }));

                    //If no pokestops are updated.
                    if (rowsAffected == 0)
                    {
                        await ctx.RespondEmbed(Translator.Instance.Translate("GYM_NO_POKESTOPS_UPDATED").FormatText(new { author = ctx.User.Username }), DiscordColor.Yellow);
                        return;
                    }

                    //Delete gyms from database where the ids match existing Pokestops.
                    rowsAffected = db.ExecuteNonQuery(Strings.SQL_DELETE_CONVERTED_POKESTOPS);
                    await ctx.RespondEmbed(Translator.Instance.Translate("GYM_POKESTOPS_DELETED").FormatText(new
                    {
                        author = ctx.User.Username,
                        amount = rowsAffected.ToString("N0"),
                    }));
                }
            }
        }
    }
}