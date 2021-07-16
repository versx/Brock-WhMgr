namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;

    using WhMgr.Alarms.Filters.Models;
    using WhMgr.Data;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Net.Webhooks;

    [
        Group("modify-filters"),
        RequirePermissions(DSharpPlus.Permissions.KickMembers),
    ]
    public class ModifyFilters
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("FILTERS", Program.LogLevel);

        private readonly Dependencies _dep;

        public ModifyFilters(Dependencies dep)
        {
            _dep = dep;
        }

        [
            Command("list"),
            Description(""),
        ]
        public async Task ListFiltersAsync(CommandContext ctx, string type, DiscordChannel channel)
        {
            if (!await ctx.IsDirectMessageSupported(_dep.WhConfig))
                return;

            var server = _dep.WhConfig.Servers[ctx.Guild.Id];
            // TODO: Check if guild/channel id exist, if not tell user to wait a few more minutes
            var filter = WebhookController.FiltersCache[channel.GuildId][channel.Id];
            var filterPath = Path.Combine(Strings.FiltersFolder, filter);
            var json = File.ReadAllText(filterPath);
            switch (type.ToLower())
            {
                case "pokemon":
                    var pokemon = JsonConvert.DeserializeObject<Dictionary<string, FilterPokemonObject>>(json);
                    var pokemonList = pokemon["pokemon"].Pokemon.Select(x => MasterFile.GetPokemon(x, 0).Name).ToList();
                    await ctx.RespondEmbed($"**Channel:** {channel.Name}\n**Available Pokemon**\n- {(pokemonList.Count == 0 ? "All" : string.Join("\n- ", pokemonList))}");
                    break;
                case "quest":
                case "quests":
                    var quest = JsonConvert.DeserializeObject<Dictionary<string, FilterQuestObject>>(json);
                    var questList = quest["quests"].RewardKeywords;
                    await ctx.RespondEmbed($"**Channel:** {channel.Name}\n**Available Rewards**\n- {(questList.Count == 0 ? "All": string.Join("\n- ", questList))}");
                    break;
            }
        }

        [
            Command("add"),
            Description("")
        ]
        public async Task AddFiltersAsync(CommandContext ctx, string type, DiscordChannel channel, string add)
        {
            if (!await ctx.IsDirectMessageSupported(_dep.WhConfig))
                return;

            var server = _dep.WhConfig.Servers[ctx.Guild.Id];

            var filter = WebhookController.FiltersCache[channel.GuildId][channel.Id];
            var filterPath = Path.Combine(Strings.FiltersFolder, filter);
            var json = File.ReadAllText(filterPath);
            switch (type.ToLower())
            {
                case "pokemon":
                    var pokemonFilter = JsonConvert.DeserializeObject<Dictionary<string, FilterPokemonObject>>(json);
                    var newPokemonList = pokemonFilter["pokemon"].Pokemon;
                    var validated = PokemonValidation.Validate(add, _dep.WhConfig.MaxPokemonId);
                    var validList = validated.Valid.Keys.ToList();
                    var validPokemonList = validList.Select(x => MasterFile.GetPokemon(x, 0).Name).ToList();
                    pokemonFilter["pokemon"].Pokemon.AddRange(validList);
                    var pkmnJson = JsonConvert.SerializeObject(pokemonFilter, Formatting.Indented);
                    File.WriteAllText(filterPath, pkmnJson);
                    await ctx.RespondEmbed($"Successfully added Pokemon '{string.Join(", ", validPokemonList)}' to filter {filterPath} for channel: {channel.Name}");
                    _dep.Whm.LoadAlarms();
                    break;
                case "quest":
                case "quests":
                    var questFilter = JsonConvert.DeserializeObject<Dictionary<string, FilterQuestObject>>(json);
                    var newRewardsList = questFilter["quests"].RewardKeywords;
                    newRewardsList.AddRange(add.RemoveSpaces());
                    questFilter["quests"].RewardKeywords = newRewardsList;
                    var questJson = JsonConvert.SerializeObject(questFilter, Formatting.Indented);
                    File.WriteAllText(filterPath, questJson);
                    await ctx.RespondEmbed($"Successfully added Quest reward '{add}' to filter {filterPath} for channel: {channel.Name}");
                    _dep.Whm.LoadAlarms();
                    break;
            }
        }

        [
            Command("remove"),
            //Aliases("", ""),
            Description("")
        ]
        public async Task RemoveFiltersAsync(CommandContext ctx, string type, DiscordChannel channel, string remove)
        {
            if (!await ctx.IsDirectMessageSupported(_dep.WhConfig))
                return;

            var server = _dep.WhConfig.Servers[ctx.Guild.Id];

            var filter = WebhookController.FiltersCache[channel.GuildId][channel.Id];
            var filterPath = Path.Combine(Strings.FiltersFolder, filter);
            var json = File.ReadAllText(filterPath);
            switch (type.ToLower())
            {
                case "pokemon":
                    var pokemonFilter = JsonConvert.DeserializeObject<Dictionary<string, FilterPokemonObject>>(json);
                    var newPokemonList = pokemonFilter["pokemon"].Pokemon;
                    var validated = PokemonValidation.Validate(remove, _dep.WhConfig.MaxPokemonId);
                    var validList = validated.Valid.Keys.ToList();
                    var validPokemonList = validList.Select(x => MasterFile.GetPokemon(x, 0).Name).ToList();
                    validList.ForEach(x => pokemonFilter["pokemon"].Pokemon.Remove(x));
                    var pkmnJson = JsonConvert.SerializeObject(pokemonFilter, Formatting.Indented);
                    File.WriteAllText(filterPath, pkmnJson);
                    await ctx.RespondEmbed($"Successfully removed Pokemon '{string.Join(", ", validPokemonList)}' from filter {filterPath} for channel: {channel.Name}");
                    _dep.Whm.LoadAlarms();
                    break;
                case "quest":
                case "quests":
                    var questFilter = JsonConvert.DeserializeObject<Dictionary<string, FilterQuestObject>>(json);
                    var newRewardsList = questFilter["quests"].RewardKeywords;
                    var rewardsToRemove = remove.RemoveSpaces();
                    rewardsToRemove.ForEach(x => newRewardsList.Remove(x));
                    questFilter["quests"].RewardKeywords = newRewardsList;
                    var questJson = JsonConvert.SerializeObject(questFilter, Formatting.Indented);
                    File.WriteAllText(filterPath, questJson);
                    await ctx.RespondEmbed($"Successfully removed Quest reward '{remove}' from filter {filterPath} for channel: {channel.Name}");
                    _dep.Whm.LoadAlarms();
                    break;
            }
        }
    }
}