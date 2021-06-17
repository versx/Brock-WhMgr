namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using ServiceStack.OrmLite;

    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Localization;

    [
        Group("event"),
        Aliases("ev"),
        Description("Event Pokemon management commands."),
        Hidden,
        RequirePermissions(Permissions.KickMembers)
    ]
    public class Event : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("COMMUNITYDAY", Program.LogLevel);

        private readonly WhConfigHolder _config;

        public Event(WhConfigHolder config)
        {
            _config = config;
        }

        [
            Command("list"),
            Aliases("l"),
            Description("List all Pokemon considered as `event`.")
        ]
        public async Task ListAsync(CommandContext ctx)
        {
            var eb = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Blurple,
                Description = "List of Pokemon set as event Pokemon which will only show in channels that are 90% or higher.",
                Title = "Event Pokemon List",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{ctx.Guild?.Name} | {DateTime.Now}",
                    IconUrl = ctx.Guild?.IconUrl
                }
            };
            var pkmnNames = new List<string>();
            for (var i = 0; i < _config.Instance.EventPokemonIds.Count; i++)
            {
                var pkmnId = _config.Instance.EventPokemonIds[i];
                if (MasterFile.Instance.Pokedex.ContainsKey(pkmnId))
                {
                    pkmnNames.Add(pkmnId + ":" + MasterFile.Instance.Pokedex[pkmnId].Name);
                }
            }

            eb.AddField("Event Pokemon", string.Join("\r\n", pkmnNames));
            await ctx.RespondAsync(embed: eb);
        }

        [
            Command("set"),
            Aliases("s"),
            Description("Sets a list of Pokemon as `event`.")
        ]
        public async Task SetAsync(CommandContext ctx,
            [Description("Comma separated list of event Pokemon")] string eventPokemonIds = "0")
        {
            var eventPokemonSplit = eventPokemonIds.Split(',');
            var pkmnToAdd = new List<uint>();
            var pkmnFailed = new List<string>();
            for (var i = 0; i < eventPokemonSplit.Length; i++)
            {
                var eventPokemonId = eventPokemonSplit[i];
                if (uint.TryParse(eventPokemonId, out var pokemonId) && (pokemonId == 0 || MasterFile.Instance.Pokedex.ContainsKey(pokemonId)))
                {
                    pkmnToAdd.Add(pokemonId);
                    continue;
                }

                pkmnFailed.Add(eventPokemonId);
            }

            _config.Instance.EventPokemonIds = pkmnToAdd;
            _config.Instance.Save(_config.Instance.FileName);

            var pkmnNames = new List<string>();
            for (var i = 0; i < pkmnToAdd.Count; i++)
            {
                var pkmnId = pkmnToAdd[i];
                if (MasterFile.Instance.Pokedex.ContainsKey(pkmnId))
                {
                    pkmnNames.Add(pkmnId + ":" + MasterFile.Instance.Pokedex[pkmnId].Name);
                }
            }

            var message = Translator.Instance.Translate("EVENT_POKEMON_SET").FormatText(ctx.User.Username, string.Join(", ", pkmnNames));
            if (pkmnFailed.Count > 0)
            {
                message += "\r\n" + Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", pkmnFailed));
            }
            await ctx.RespondEmbed(message);
        }

        [
            Command("add"),
            Aliases("a"),
            Description("Adds one or more Pokemon to current `event` list.")
        ]
        public async Task AddAsync(CommandContext ctx,
            [Description("Comma separated list of event Pokemon")] string eventPokemonIds)
        {
            var eventPokemonSplit = eventPokemonIds.Split(',');
            var pkmnToAdd = new List<uint>();
            var pkmnFailed = new List<string>();
            for (var i = 0; i < eventPokemonSplit.Length; i++)
            {
                var eventPokemonId = eventPokemonSplit[i];
                if (uint.TryParse(eventPokemonId, out var pokemonId) && (pokemonId == 0 || MasterFile.Instance.Pokedex.ContainsKey(pokemonId)))
                {
                    pkmnToAdd.Add(pokemonId);
                    continue;
                }

                pkmnFailed.Add(eventPokemonId);
            }

            _config.Instance.EventPokemonIds.AddRange(pkmnToAdd);
            _config.Instance.Save(_config.Instance.FileName);

            var pkmnNames = new List<string>();
            for (var i = 0; i < pkmnToAdd.Count; i++)
            {
                var pkmnId = pkmnToAdd[i];
                if (MasterFile.Instance.Pokedex.ContainsKey(pkmnId))
                {
                    pkmnNames.Add(pkmnId + ":" + MasterFile.Instance.Pokedex[pkmnId].Name);
                }
            }

            var message = Translator.Instance.Translate("EVENT_POKEMON_SET").FormatText(ctx.User.Username, string.Join(", ", pkmnNames));
            if (pkmnFailed.Count > 0)
            {
                message += "\r\n" + Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", pkmnFailed));
            }
            await ctx.RespondEmbed(message);
        }

        [
            Command("remove"),
            Aliases("r", "rm"),
            Description("Removes one or more Pokemon from `event` list.")
        ]
        public async Task RemoveAsync(CommandContext ctx,
            [Description("Command separated list of event Pokemon")] string eventPokemonIds)
        {
            var eventPokemonSplit = eventPokemonIds.Split(',');
            var pkmnToRemove = new List<uint>();
            var pkmnFailed = new List<string>();
            for (var i = 0; i < eventPokemonSplit.Length; i++)
            {
                var eventPokemonId = eventPokemonSplit[i];
                if (uint.TryParse(eventPokemonId, out var pokemonId) && (pokemonId == 0 || MasterFile.Instance.Pokedex.ContainsKey(pokemonId)))
                {
                    pkmnToRemove.Add(pokemonId);
                    continue;
                }

                pkmnFailed.Add(eventPokemonId);
            }

            pkmnToRemove.ForEach(x => _config.Instance.EventPokemonIds.Remove(x));
            _config.Instance.Save(_config.Instance.FileName);

            var pkmnNames = new List<string>();
            for (var i = 0; i < pkmnToRemove.Count; i++)
            {
                var pkmnId = pkmnToRemove[i];
                if (MasterFile.Instance.Pokedex.ContainsKey(pkmnId))
                {
                    pkmnNames.Add(pkmnId + ":" + MasterFile.Instance.Pokedex[pkmnId].Name);
                }
            }

            var message = Translator.Instance.Translate("EVENT_POKEMON_REMOVE").FormatText(ctx.User.Username, string.Join(", ", pkmnNames));
            if (pkmnFailed.Count > 0)
            {
                message += "\r\n" + Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", pkmnFailed));
            }
            await ctx.RespondEmbed(message);
        }

        [
            Command("stats"),
            Description("")
        ]
        public async Task StatsAsync(CommandContext ctx,
            [Description("")] string pokemon,
            [Description("")] string start,
            [Description("")] string end)
        {
            var pokeId = pokemon.PokemonIdFromName();
            if (pokeId == 0)
            {
                // TODO: Localize
                await ctx.RespondEmbed($"{ctx.User.Username} Unable to find Pokemon by name or id {pokemon}");
                return;
            }

            // TODO: Parse and validate start/end

            var sql = @"
            SELECT
              COUNT(id) AS total,
              SUM(iv > 0) AS with_iv,
              SUM(iv IS NULL) AS without_iv,
              SUM(iv = 0) AS iv_0,
              SUM(iv >= 1 AND iv < 10) AS iv_1_9,
              SUM(iv >= 10 AND iv < 20) AS iv_10_19,
              SUM(iv >= 20 AND iv < 30) AS iv_20_29,
              SUM(iv >= 30 AND iv < 40) AS iv_30_39,
              SUM(iv >= 40 AND iv < 50) AS iv_40_49,
              SUM(iv >= 50 AND iv < 60) AS iv_50_59,
              SUM(iv >= 60 AND iv < 70) AS iv_60_69,
              SUM(iv >= 70 AND iv < 80) AS iv_70_79,
              SUM(iv >= 80 AND iv < 90) AS iv_80_89,
              SUM(iv >= 90 AND iv < 100) AS iv_90_99,
              SUM(iv = 100) AS iv_100,
              SUM(gender = 1) AS male,
              SUM(gender = 2) AS female,
              SUM(gender = 3) AS genderless,
              SUM(level >= 1 AND level <= 9) AS level_1_9,
              SUM(level >= 10 AND level <= 19) AS level_10_19,
              SUM(level >= 20 AND level <= 29) AS level_20_29,
              SUM(level >= 30 AND level <= 35) AS level_30_35
            FROM
              pokemon
            WHERE
              pokemon_id = @pokemonId
              AND first_seen_timestamp >= @start
              AND first_seen_timestamp <= @end
            ";
            var dict = new Dictionary<string, object>
            {
                { "pokemonId", pokemon },
                { "start", start },
                { "end", end }
            };
            var results = ExecuteQuery<object>(sql, dict);
            if (results.Count > 0)
            {
                var result = (dynamic)results.FirstOrDefault();
                Console.WriteLine(result);
                int total = Convert.ToInt32(result.total ?? 0);
                int withIV = Convert.ToInt32(result.with_iv ?? 0);
                int withoutIV = Convert.ToInt32(result.without_iv ?? 0);
                int iv0 = Convert.ToInt32(result.iv_0 ?? 0);
                int iv1_9 = Convert.ToInt32(result.iv_1_9 ?? 0);
                int iv10_19 = Convert.ToInt32(result.iv_10_19 ?? 0);
                int iv20_29 = Convert.ToInt32(result.iv_20_29 ?? 0);
                int iv30_39 = Convert.ToInt32(result.iv_30_39 ?? 0);
                int iv40_49 = Convert.ToInt32(result.iv_40_49 ?? 0);
                int iv50_59 = Convert.ToInt32(result.iv_50_59 ?? 0);
                int iv60_69 = Convert.ToInt32(result.iv_60_69 ?? 0);
                int iv70_79 = Convert.ToInt32(result.iv_70_79 ?? 0);
                int iv80_89 = Convert.ToInt32(result.iv_80_89 ?? 0);
                int iv90_99 = Convert.ToInt32(result.iv_90_99 ?? 0);
                int iv100 = Convert.ToInt32(result.iv_100 ?? 0);
                int male = Convert.ToInt32(result.male ?? 0);
                int female = Convert.ToInt32(result.female ?? 0);
                int genderless = Convert.ToInt32(result.genderless ?? 0);
                int level1_9 = Convert.ToInt32(result.level_1_9 ?? 0);
                int level10_19 = Convert.ToInt32(result.level_10_19 ?? 0);
                int level20_29 = Convert.ToInt32(result.level_20_29 ?? 0);
                int level30_35 = Convert.ToInt32(result.level_30_35 ?? 0);

                var pkmn = MasterFile.GetPokemon(pokeId, 0);
                var eb = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Blurple,
                    Title = $"{ctx.Guild.Name} Community Day Stats",
                    Description = $"**{pkmn.Name}** ({pokeId})\r\nBetween: {start} - {end}",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{ctx.Guild?.Name} | {DateTime.Now}",
                        IconUrl = ctx.Guild?.IconUrl
                    }
                };
                // TODO: Localize
                eb.AddField("Total", total.ToString("N0"), true);
                eb.AddField("With IV", withIV.ToString("N0"), true);
                eb.AddField("Without IV", withoutIV.ToString("N0"), true);

                eb.AddField("0% IV", iv0.ToString("N0"), true);
                eb.AddField("1-9% IV", iv1_9.ToString("N0"), true);
                eb.AddField("10-19% IV", iv10_19.ToString("N0"), true);
                eb.AddField("20-29% IV", iv20_29.ToString("N0"), true);
                eb.AddField("30-39% IV", iv30_39.ToString("N0"), true);
                eb.AddField("40-49% IV", iv40_49.ToString("N0"), true);
                eb.AddField("50-59% IV", iv50_59.ToString("N0"), true);
                eb.AddField("60-69% IV", iv60_69.ToString("N0"), true);
                eb.AddField("70-79% IV", iv70_79.ToString("N0"), true);
                eb.AddField("80-89% IV", iv80_89.ToString("N0"), true);
                eb.AddField("90-99% IV", iv90_99.ToString("N0"), true);
                eb.AddField("100 % IV", iv100.ToString("N0"), true);

                eb.AddField("Male", male.ToString("N0"), true);
                eb.AddField("Female", female.ToString("N0"), true);
                eb.AddField("Genderless", genderless.ToString("N0"), true);

                eb.AddField("Level 1-9", level1_9.ToString("N0"), true);
                eb.AddField("Level 10-19", level10_19.ToString("N0"), true);
                eb.AddField("Level 20-29", level20_29.ToString("N0"), true);
                eb.AddField("Level 30-35", level30_35.ToString("N0"), true);
                await ctx.RespondAsync(embed: eb);
            }
        }

        public static List<T> ExecuteQuery<T>(string sql, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(DataAccessLayer.ScannerConnectionString))
                return default;

            try
            {
                using (var db = DataAccessLayer.CreateFactory(DataAccessLayer.ScannerConnectionString).Open())
                {
                    var query = db.Select<T>(sql, args);
                    return query;
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                _logger.Error(ex);
            }
            return default;
        }
    }
}