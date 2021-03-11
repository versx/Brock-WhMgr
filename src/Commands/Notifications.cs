﻿using WhMgr.Configuration;

namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;

    using WhMgr.Commands.Input;
    using WhMgr.Data;
    using WhMgr.Data.Models;
    using WhMgr.Data.Subscriptions;
    using WhMgr.Data.Subscriptions.Models;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Localization;
    using WhMgr.Net.Models;
    using WhMgr.Utilities;

    public class Notifications
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("NOTIFICATIONS", Program.LogLevel);

        private readonly Dependencies _dep;

        public Notifications(Dependencies dep)
        {
            _dep = dep;
        }

        #region General

        [
            Command("info"),
            Description("Shows your current Pokemon and Raid boss notification subscriptions.")
        ]
        public async Task InfoAsync(CommandContext ctx,
            [Description("Discord user mention string.")] string mention = "")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (string.IsNullOrEmpty(mention))
            {
                await SendUserSubscriptionSettings(ctx.Client, ctx.User, ctx.User, guildId);
                return;
            }

            var isModOrHigher = await ctx.Client.IsModeratorOrHigher(ctx.User.Id, guildId, _dep.WhConfig);
            if (!isModOrHigher)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_NOT_MODERATOR_OR_HIGHER").FormatText(ctx.User.Mention), DiscordColor.Red);
                return;
            }

            var userId = ConvertMentionToUserId(mention);
            if (userId <= 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_INVALID_USER_MENTION").FormatText(ctx.User.Mention, mention), DiscordColor.Red);
                return;
            }

            var user = await ctx.Client.GetUserAsync(userId);
            if (user == null)
            {
                _logger.Warn($"Failed to get Discord user with id {userId}.");
                return;
            }

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();

            await SendUserSubscriptionSettings(ctx.Client, ctx.User, user, guildId);
        }

        [
            Command("enable"),
            Aliases("disable"),
            Description("Enables or disables all of your Pokemon and Raid notification subscriptions at once.")
        ]
        public async Task EnableDisableAsync(CommandContext ctx,
            [Description("Discord user mention string.")] string mention = "")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            if (string.IsNullOrEmpty(mention))
            {
                await EnableDisableUserSubscriptions(ctx, ctx.User, guildId);
                return;
            }

            var isModOrHigher = await ctx.Client.IsModeratorOrHigher(ctx.User.Id, guildId, _dep.WhConfig);
            if (!isModOrHigher)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_NOT_MODERATOR_OR_HIGHER").FormatText(ctx.User.Mention), DiscordColor.Red);
                return;
            }

            var userId = ConvertMentionToUserId(mention);
            if (userId <= 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_INVALID_USER_MENTION").FormatText(ctx.User.Mention, mention), DiscordColor.Red);
                return;
            }

            var user = await ctx.Client.GetUserAsync(userId);
            if (user == null)
            {
                _logger.Warn($"Failed to get Discord user with id {userId}.");
                return;
            }

            await EnableDisableUserSubscriptions(ctx, user, guildId);

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("set-distance"),
            Description("Set the distance and location you'd like to receive raid notifications.")
        ]
        public async Task SetDistanceAsync(CommandContext ctx,
            [Description("Maximum distance in meters between the set coordinates.")] int distance,
            [Description("Coordinates in `34.00,-117.00` format."), RemainingText] string coordinates)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var parts = coordinates.Replace(" ", null).Split(',');
            if (!double.TryParse(parts[0], out var lat) || !double.TryParse(parts[1], out var lng))
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_COORDINATES").FormatText(ctx.User.Username, coordinates), DiscordColor.Red);
                return;
            }

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_USER_NOT_SUBSCRIBED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            subscription.DistanceM = distance;
            subscription.Latitude = lat;
            subscription.Longitude = lng;
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_DISTANCE_SET").FormatText(ctx.User.Username, distance, lat, lng));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("set-number"),
            Description("Set the phone number to receive text message notifications for ultra rare pokemon.")
        ]
        public async Task SetPhoneNumberAsync(CommandContext ctx,
            [Description("10 digit phone number to receive text message alerts for")] string phoneNumber)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            // Check if user is in list of acceptable users to receive Pokemon text message notifications
            if (!_dep.WhConfig.Twilio.UserIds.Contains(ctx.User.Id))
                return;

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_USER_NOT_SUBSCRIBED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            subscription.PhoneNumber = phoneNumber;
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_PHONE_NUMBER_SET").FormatText(ctx.User.Username, phoneNumber));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("expire"),
            Aliases("expires"),
            Description("")
        ]
        public async Task GetExpireDateAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var message = BuildExpirationMessage(guildId, ctx.User);
            await ctx.Client.SendDirectMessage(ctx.User, message);
        }

        [
            Command("expire-admin"),
            Description(""),
            Hidden,
            RequireOwner
        ]
        public async Task GetExpireAdminAsync(CommandContext ctx,
            [Description("Discord user id to check expire date for")] string userId)
        {
            if (!await CanExecute(ctx))
                return;

            if (!ulong.TryParse(userId, out var realUserId))
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_PARSING_USER_ID").FormatText(ctx.User.Username, userId), DiscordColor.Red);
                return;
            }

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var user = await ctx.Client.GetUserAsync(realUserId);
            var message = BuildExpirationMessage(guildId, user);
            await ctx.Client.SendDirectMessage(ctx.User, message);
        }

        #endregion

        #region Pokeme / Pokemenot

        [
            Command("pokeme"),
            Description("Subscribe to Pokemon notifications based on the pokedex number or name, minimum IV stats, or minimum level.")
        ]
        public async Task PokeMeAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokemon name(s) and/or Pokedex IDs to subscribe to Pokemon spawn notifications.")] string poke,
            [Description("Minimum IV to receive notifications for, use 0 to disregard IV. i.e. 100 or 0-15-15")] string iv = "0",
            [Description("Minimum level and maximum level to receive notifications for, use 0 to disregard level. Set a maximum value with 15-35.")] string lvl = "0",
            [Description("Specific gender the Pokemon must be, use * to disregard gender. (*, m, f)")] string gender = "*",
            [Description("City or area to add to the subscription, 'All' for all areas, or leave blank to use set distance."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];

            //if (!int.TryParse(cpArg, out int cp))
            //{
            //    await message.RespondEmbed($"'{cpArg}' is not a valid value for CP.", DiscordColor.Red);
            //    return;
            //}

            var attack = -1;
            var defense = -1;
            var stamina = -1;
            var realIV = 0;

            // Check if IV value contains `-` and to expect individual values instead of whole IV value
            if (iv.Contains("-"))
            {
                var split = iv.Split('-');
                if (split.Length != 3)
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_IV_VALUES").FormatText(ctx.User.Username, iv), DiscordColor.Red);
                    return;
                }
                if (!int.TryParse(split[0], out attack))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_ATTACK_VALUE").FormatText(ctx.User.Username, split[0]), DiscordColor.Red);
                    return;
                }
                if (!int.TryParse(split[1], out defense))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_DEFENSE_VALUE").FormatText(ctx.User.Username, split[1]), DiscordColor.Red);
                    return;
                }
                if (!int.TryParse(split[2], out stamina))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_STAMINA_VALUE").FormatText(ctx.User.Username, split[2]), DiscordColor.Red);
                    return;
                }
            }
            else
            {
                // User provided IV value as a whole
                if (!int.TryParse(iv, out realIV) || realIV < Strings.MinimumIV || realIV > Strings.MaximumIV)
                {
                    await ctx.TriggerTypingAsync();
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_IV_RANGE").FormatText(ctx.User.Username, iv), DiscordColor.Red);
                    return;
                }
            }

            var minLevel = Strings.MinimumLevel;
            var maxLevel = Strings.MaximumLevel;
            // Check if level contains `-` and to expect a minimum and maximum level provided
            if (lvl.Contains('-'))
            {
                var split = lvl.Split('-');
                if (!int.TryParse(split[0], out minLevel))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MINIMUM_LEVEL", ctx.User.Username, split[0]), DiscordColor.Red);
                    return;
                }
                if (!int.TryParse(split[1], out maxLevel))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MAXIMUM_LEVEL", ctx.User.Username, split[1]), DiscordColor.Red);
                    return;
                }
            }
            else
            {
                // Only minimum level was provided
                if (!int.TryParse(lvl, out minLevel))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MINIMUM_LEVEL", ctx.User.Username, lvl), DiscordColor.Red);
                    return;
                }
            }

            // Validate minimum and maximum levels are within range
            if (minLevel < 0 || minLevel > 35)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_LEVEL").FormatText(ctx.User.Username, lvl), DiscordColor.Red);
                return;
            }

            // Check if gender is a valid gender provided
            if (!Strings.ValidGenders.Contains(gender.ToLower()))
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_GENDER").FormatText(ctx.User.Username, gender), DiscordColor.Red);
                return;
            }

            // Check if user is trying to subscribe to 'All' Pokemon
            if (string.Compare(poke, Strings.All, true) == 0)
            {
                // If so, make sure they specified at least 90% or higher
                if (realIV < 90)
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MINIMUM_IV").FormatText(ctx.User.Username), DiscordColor.Red);
                    return;
                }
            }

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxPokemonSubscriptions > 0 && subscription.Pokemon.Count >= server.Subscriptions.MaxPokemonSubscriptions)
            {
                // Max limit for Pokemon subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxPokemonSubscriptions), DiscordColor.Red);
                return;
            }

            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();
            var isModOrHigher = await ctx.Client.IsModeratorOrHigher(ctx.User.Id, guildId, _dep.WhConfig);
            // Validate the provided pokemon list
            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            if (areas.Count == 0 && subscription.DistanceM == 0)
            {
                await ctx.RespondEmbed($"{ctx.User.Username}#{ctx.User.Discriminator} You must either set a distance to receive notifications from (`set-distance <meters> <latitude>,<longitude>`) or provide a city/area for the subscription. Aborting request.", DiscordColor.Red);
                return;
            }

            // Loop through each valid pokemon entry provided
            foreach (var (pokemonId, form) in validation.Valid)
            {
                if (!MasterFile.Instance.Pokedex.ContainsKey(pokemonId))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_ID").FormatText(ctx.User.Username, pokemonId), DiscordColor.Red);
                    continue;
                }

                var pokemon = MasterFile.Instance.Pokedex[pokemonId];
                var name = string.IsNullOrEmpty(form) ? pokemon.Name : pokemon.Name + "-" + form;

                // Check if common type pokemon e.g. Pidgey, Ratatta, Spinarak 'they are beneath him and he refuses to discuss them further'
                if (pokemonId.IsCommonPokemon() && realIV < Strings.CommonTypeMinimumIV && !isModOrHigher)
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_COMMON_TYPE_POKEMON").FormatText(ctx.User.Username, pokemon.Name, Strings.CommonTypeMinimumIV), DiscordColor.Red);
                    continue;
                }

                var subPkmn = subscription.Pokemon.FirstOrDefault(x => x.PokemonId == pokemonId && string.Compare(x.Form, form, true) == 0);
                // Always ignore the user's input for Unown and set it to 0 by default.
                var minIV = pokemonId.IsRarePokemon() ? 0 : realIV;
                var minLvl = pokemonId.IsRarePokemon() ? 0 : minLevel;
                var maxLvl = pokemonId.IsRarePokemon() ? 35 : maxLevel;
                var hasStatsSet = attack >= 0 || defense >= 0 || stamina >= 0;

                if (subPkmn == null)
                {
                    // Does not exist, create.
                    subscription.Pokemon.Add(new PokemonSubscription
                    {
                        GuildId = guildId,
                        UserId = ctx.User.Id,
                        PokemonId = pokemonId,
                        Form = form,
                        MinimumIV = minIV,
                        MinimumLevel = minLvl,
                        MaximumLevel = maxLvl,
                        Gender = gender,
                        IVList = hasStatsSet ? new List<string> { $"{attack}/{defense}/{stamina}" } : new List<string>(),
                        Areas = areas
                    });
                    subscribed.Add(name);
                    continue;
                }

                // Exists, check if anything changed.
                if (realIV != subPkmn.MinimumIV ||
                    string.Compare(form, subPkmn.Form, true) != 0 ||
                    minLvl != subPkmn.MinimumLevel ||
                    maxLvl != subPkmn.MaximumLevel ||
                    gender != subPkmn.Gender ||
                    (!subPkmn.IVList.Contains($"{attack}/{defense}/{stamina}") && hasStatsSet) ||
                    // TODO: Check against cities
                    //(string.Compare(subPkmn.City, cities, true) != 0 && !ContainsCity(subPkmn.City, cities)))
                    !SubscriptionAreas.ContainsCity(subPkmn.Areas, areas))
                {
                    subPkmn.Form = form;
                    subPkmn.MinimumIV = hasStatsSet ? subPkmn.MinimumIV : realIV;
                    subPkmn.MinimumLevel = minLvl;
                    subPkmn.MaximumLevel = maxLvl;
                    subPkmn.Gender = gender;
                    if (hasStatsSet)
                    {
                        subPkmn.IVList.Add($"{attack}/{defense}/{stamina}");
                    }
                    foreach (var area in areas)
                    {
                        if (!subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subPkmn.Areas.Add(area);
                        }
                    }
                    subscribed.Add(name);
                    continue;
                }

                // Already subscribed to the same Pokemon and form
                alreadySubscribed.Add(name);
            }

            subscription.Save();
            await ctx.TriggerTypingAsync();
            if (subscribed.Count == 0 && alreadySubscribed.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SPECIFIED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var isAll = string.Compare(Strings.All, poke, true) == 0;
            var isGen = false;
            for (var i = 1; i < 6; i++)
            {
                if (string.Compare("Gen" + i, poke, true) == 0)
                {
                    isGen = true;
                    break;
                }
            }

            await ctx.RespondEmbed
            (
                (subscribed.Count > 0
                    ? $"{ctx.User.Username} has subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", subscribed))}** notifications with a{(attack >= 0 || defense >= 0 || stamina >= 0 ? $"n IV value of {attack}/{defense}/{stamina}" : $" minimum IV of {iv}%")}{(minLevel > 0 ? $" and between levels {minLevel}-{maxLevel}" : null)}{(gender == "*" ? null : $" and only '{gender}' gender types")} and only from the following areas: {(areas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", areas))}."
                    : string.Empty) +
                (alreadySubscribed.Count > 0
                    ? $"\r\n{ctx.User.Username} is already subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", alreadySubscribed))}** notifications with a{(attack >= 0 || defense >= 0 || stamina >= 0 ? $"n IV value of {attack}/{defense}/{stamina}" : $" minimum IV of {iv}%")}{(minLevel > 0 ? $" and between levels {minLevel}-{maxLevel}" : null)}{(gender == "*" ? null : $" and only '{gender}' gender types")} and only from the following areas: {(areas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", areas))}."
                    : string.Empty)
            );

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("pokemenot"),
            Description("Unsubscribe from one or more or even all subscribed Pokemon notifications by pokedex number or name.")
        ]
        public async Task PokeMeNotAsync(CommandContext ctx,
            [Description("Pokemon name or id to unsubscribe from Pokemon spawn notifications.")] string poke,
            [Description("City or area to remove from the subscription, 'All' for all areas, or leave blank to remove the whole subscription.")] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.Pokemon?.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_NO_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            // Check if we received the all parameter
            if (string.Compare(poke, Strings.All, true) == 0)
            {
                // Send a confirmation confirming the user actually wants to remove all of their Pokemon subscriptions
                var confirm = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Pokemon.Count.ToString("N0")));
                if (!confirm)
                    return;

                // Loop through all Pokemon subscriptions and remove them
                subscription.Pokemon.ForEach(x => x.Id.Remove<PokemonSubscription>());
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            var pokemonNames = validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value));
            var error = false;
            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subPkmn = subscription.Pokemon.FirstOrDefault(x => x.PokemonId == pokemonId && (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0));
                if (subPkmn == null)
                    continue;

                foreach (var area in areas)
                {
                    // TODO: Remove all areas to prevent lingering ones?
                    if (subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subPkmn.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subPkmn.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the Pokemon subscription
                //if (subPkmn.Areas.Count == 0)
                // If no city specified then remove the whole subscription
                if (string.IsNullOrEmpty(city))
                {
                    // If no more areas set for the Pokemon subscription, delete it
                    var result = subPkmn.Id.Remove<PokemonSubscription>();
                    if (!result)
                    {
                        error = true;
                        //TODO: Collect list of failed.
                    }
                }
                else
                {
                    // Save/update Pokemon subscription if cities still assigned
                    subPkmn.Save();
                }
            }

            if (error)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("FAILED_POKEMON_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(ctx.User.Username, string.Join(", ", pokemonNames)), DiscordColor.Red);
                return;
            }

            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_POKEMON_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(ctx.User.Username, string.Join("**, **", pokemonNames)));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Raidme / Raidmenot

        [
            Command("raidme"),
            Description("Subscribe to raid boss notifications based on the pokedex number or name.")
        ]
        public async Task RaidMeAsync(CommandContext ctx,
            [Description("Pokemon name or id to subscribe to raid notifications.")] string poke,
            [Description("City or area to add to the subscription, 'All' for all areas, or leave blank to use set distance."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxRaidSubscriptions > 0 && subscription.Raids.Count >= server.Subscriptions.MaxRaidSubscriptions)
            {
                // Max limit for Raid subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_RAID_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxRaidSubscriptions), DiscordColor.Red);
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            if (areas.Count == 0 && subscription.DistanceM == 0)
            {
                await ctx.RespondEmbed($"{ctx.User.Username}#{ctx.User.Discriminator} You must either set a distance to receive notifications from (`set-distance <meters> <latitude>,<longitude>`) or provide a city/area for the subscription. Aborting request.", DiscordColor.Red);
                return;
            }

            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subRaid = subscription.Raids.FirstOrDefault(x => x.PokemonId == pokemonId && string.Compare(x.Form, form, true) == 0);
                if (subRaid != null)
                {
                    // Existing raid subscription
                    // Loop all areas, check if the area is already in subs, if not add it
                    foreach (var area in areas)
                    {
                        if (!subRaid.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subRaid.Areas.Add(area);
                        }
                    }
                    // Save raid subscription and continue;
                    // REVIEW: Might not be needed
                    subRaid.Save();
                    continue;
                }

                // New raid subscription
                subscription.Raids.Add(new RaidSubscription
                {
                    GuildId = guildId,
                    UserId = ctx.User.Id,
                    PokemonId = pokemonId,
                    Form = form,
                    Areas = areas
                });
            }
            subscription.Save();

            var pokemonNames = validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value));
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_RAID_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(poke, Strings.All, true) == 0 ? Strings.All : string.Join("**, **", pokemonNames),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("raidmenot"),
            Description("Unsubscribe from one or more or even all subscribed raid boss notifications by pokedex number or name.")
        ]
        public async Task RaidMeNotAsync(CommandContext ctx,
            [Description("Pokemon name or id to unsubscribe from raid notifications.")] string poke,
            [Description("City or area to remove from the subscription, 'All' for all areas, or leave blank to remove the whole subscription."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.Raids.Count == 0)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_RAID_SUBSCRIPTIONS").FormatText(ctx.User.Username,
                    string.IsNullOrEmpty(city)
                        ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                        : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city)),
                    DiscordColor.Red
                );
                return;
            }

            if (string.Compare(poke, Strings.All, true) == 0)
            {
                var result = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_RAID_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Raids.Count.ToString("N0")));
                if (!result)
                    return;

                subscription.Raids.ForEach(x => x.Id.Remove<RaidSubscription>());

                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_RAID_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            foreach (var item in validation.Valid)
            {
                var pokemonId = item.Key;
                var form = item.Value;
                var subRaid = subscription.Raids.FirstOrDefault(x => x.PokemonId == pokemonId && (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0));
                // Check if subscribed
                if (subRaid == null)
                    continue;

                foreach (var area in areas)
                {
                    if (subRaid.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subRaid.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subRaid.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the Pokemon subscription
                //if (subRaid.Areas.Count == 0)
                // If no city specified then remove the whole subscription
                if (string.IsNullOrEmpty(city))
                {
                    // If no more areas set for the Pokemon subscription, delete it
                    if (!subRaid.Id.Remove<RaidSubscription>())
                    {
                        _logger.Error($"Unable to remove raid subscription for user id {subRaid.UserId} from guild id {subRaid.GuildId}");
                    }
                }
                else
                {
                    // Save/update raid subscription if cities still assigned
                    subRaid.Save();
                }
            }

            var pokemonNames = validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value));
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_RAID_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(poke, Strings.All, true) == 0 ? Strings.All : string.Join("**, **", pokemonNames),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(string.Join(", ", areas)))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Questme / Questmenot

        [
            Command("questme"),
            Description("Subscribe to quest notifications based on the reward keyword.")
        ]
        public async Task QuestMeAsync(CommandContext ctx,
            [Description("Reward keyword to use to find field research. Example: Spinda, 1200 stardust, candy")] string rewardKeyword,
            [Description("City or area to add to the subscription, 'All' for all areas, or leave blank to use set distance."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxQuestSubscriptions > 0 && subscription.Quests.Count >= server.Subscriptions.MaxQuestSubscriptions)
            {
                // Max limit for Quest subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_QUEST_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxQuestSubscriptions), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            if (areas.Count == 0 && subscription.DistanceM == 0)
            {
                await ctx.RespondEmbed($"{ctx.User.Username}#{ctx.User.Discriminator} You must either set a distance to receive notifications from (`set-distance <meters> <latitude>,<longitude>`) or provide a city/area for the subscription. Aborting request.", DiscordColor.Red);
                return;
            }

            var subQuest = subscription.Quests.FirstOrDefault(x => string.Compare(x.RewardKeyword, rewardKeyword, true) == 0);
            if (subQuest != null)
            {
                // Existing quest subscription
                // Loop all areas, check if the area is already in subs, if not add it
                foreach (var area in areas)
                {
                    if (!subQuest.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        subQuest.Areas.Add(area);
                    }
                }
                // Save quest subscription and continue;
                // REVIEW: Might not be needed
                subQuest.Save();
            }
            else
            {
                subscription.Quests.Add(new QuestSubscription
                {
                    GuildId = guildId,
                    UserId = ctx.User.Id,
                    RewardKeyword = rewardKeyword,
                    Areas = areas
                });
            }

            subscription.Save();
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_QUEST_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                ctx.User.Username,
                rewardKeyword,
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("questmenot"),
            Description("Unsubscribe from one or all subscribed field research quest notifications by reward keyword.")
        ]
        public async Task QuestMeNotAsync(CommandContext ctx,
            [Description("Reward keyword to remove from field research quest subscriptions. Example: Spinda, 1200 stardust, candy")] string rewardKeyword,
            [Description("City or area to remove from the subscription, 'All' for all areas, or leave blank to remove the whole subscription."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.Quests.Count == 0)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_QUEST_SUBSCRIPTIONS").FormatText(
                    ctx.User.Username,
                    rewardKeyword,
                    string.IsNullOrEmpty(city)
                        ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                        : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city)),
                    DiscordColor.Red
                );
                return;
            }

            var notSubscribed = new List<string>();
            var unsubscribed = new List<string>();
            await ctx.TriggerTypingAsync();

            if (string.Compare(rewardKeyword, Strings.All, true) == 0)
            {
                var removeAllResult = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_QUEST_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Quests.Count.ToString("N0")));
                if (!removeAllResult)
                    return;

                subscription.Quests.ForEach(x => x.Id.Remove<QuestSubscription>());
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_CONFIRM_SUCCESS_ALL_QUEST_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            var subQuest = subscription.Quests.FirstOrDefault(x => string.Compare(x.RewardKeyword, rewardKeyword, true) == 0);
            // Check if subscribed
            if (subQuest == null)
                return;

            foreach (var area in areas)
            {
                if (subQuest.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                {
                    var index = subQuest.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                    subQuest.Areas.RemoveAt(index);
                }
            }

            // Check if there are no more areas set for the Pokemon subscription
            //if (subQuest.Areas.Count == 0)
            // If no city specified then remove the whole subscription
            if (string.IsNullOrEmpty(city))
            {
                // If no more areas set for the Pokemon subscription, delete it
                if (!subQuest.Id.Remove<QuestSubscription>())
                {
                    _logger.Error($"Unable to remove quest subscription for user id {subQuest.UserId} from guild id {subQuest.GuildId}");
                }
            }
            else
            {
                // Save/update quest subscription if cities still assigned
                subQuest.Save();
            }
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_QUEST_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                ctx.User.Username,
                rewardKeyword,
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Gymme / Gymmenot

        [
            Command("gymme"),
            Description("Add raid notifications for specific gyms.")
        ]
        public async Task GymMeAsync(CommandContext ctx,
            [Description("Gym name to subscribed to."), RemainingText] string gymName)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxGymSubscriptions > 0 && subscription.Gyms.Count >= server.Subscriptions.MaxGymSubscriptions)
            {
                // Max limit for Gym subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_GYM_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxGymSubscriptions), DiscordColor.Red);
                return;
            }

            var subGym = subscription.Gyms.FirstOrDefault(x => string.Compare(x.Name, gymName, true) == 0);
            if (subGym != null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_EXISTS").FormatText(ctx.User.Username, gymName), DiscordColor.Red);
                return;
            }

            subscription.Gyms.Add(new GymSubscription
            {
                GuildId = guildId,
                UserId = ctx.User.Id,
                Name = gymName
            });
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_ADDED").FormatText(ctx.User.Username, gymName));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("gymmenot"),
            Description("Remove raid notifications for specific gyms.")
        ]
        public async Task GymMeNotAsync(CommandContext ctx,
            [Description("Gym name to unsubscribed from."), RemainingText] string gymName)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (string.Compare(Strings.All, gymName, true) == 0)
            {
                var result = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_GYM_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Gyms.Count.ToString("N0")));
                if (!result)
                    return;

                subscription.Gyms.ForEach(x => x.Id.Remove<GymSubscription>());
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_GYM_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            subscription.Gyms
                .Where(x => string.Compare(x.Name, gymName, true) == 0)?
                .ToList()?
                .ForEach(x => x.Id.Remove<GymSubscription>());
            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_REMOVED").FormatText(ctx.User.Username, gymName));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Invme / Invmenot

        [
            Command("invme"),
            Description("Subscribe to Team Rocket invasion notifications based on the encounter reward.")
        ]
        public async Task InvMeAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokemon name(s) and/or Pokedex IDs to subscribe to rewards from Team Rocket Invasion notifications.")] string poke,
            [Description("City or area to add to the subscription, 'All' for all areas, or leave blank to use set distance."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxInvasionSubscriptions > 0 && subscription.Invasions.Count >= server.Subscriptions.MaxInvasionSubscriptions)
            {
                // Max limit for Invasion subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_INVASION_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxInvasionSubscriptions), DiscordColor.Red);
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            if (areas.Count == 0 && subscription.DistanceM == 0)
            {
                await ctx.RespondEmbed($"{ctx.User.Username}#{ctx.User.Discriminator} You must either set a distance to receive notifications from (`set-distance <meters> <latitude>,<longitude>`) or provide a city/area for the subscription. Aborting request.", DiscordColor.Red);
                return;
            }

            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subInvasion = subscription.Invasions.FirstOrDefault(x => x.RewardPokemonId == pokemonId);
                if (subInvasion != null)
                {
                    // Existing invasion subscription
                    // Loop all areas, check if the area is already in subs, if not add it
                    foreach (var area in areas)
                    {
                        if (!subInvasion.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subInvasion.Areas.Add(area);
                        }
                    }
                    // Save quest subscription and continue;
                    // REVIEW: Might not be needed
                    subInvasion.Save();
                }
                else
                {
                    // New invasion subscription
                    subscription.Invasions.Add(new InvasionSubscription
                    {
                        GuildId = guildId,
                        UserId = ctx.User.Id,
                        RewardPokemonId = pokemonId,
                        Areas = areas
                    });
                }
            }
            subscription.Save();

            var valid = validation.Valid.Keys.Select(x => MasterFile.GetPokemon(x, 0).Name);
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_INVASION_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(poke, Strings.All, true) == 0 ? Strings.All : string.Join(", ", valid),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("invmenot"),
            Description("Unsubscribe from one or all subscribed Team Rocket invasion notifications by encounter reward.")
        ]
        public async Task InvMeNotAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokemon name(s) and/or Pokedex IDs to unsubscribe from rewards for Team Rocket Invasion notifications.")] string poke,
            [Description("City or area to remove from the subscription, 'All' for all areas, or leave blank to remove the whole subscription."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.Invasions.Count == 0)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_INVASION_SUBSCRIPTIONS").FormatText(ctx.User.Username, string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city)),
                    DiscordColor.Red
                );
                return;
            }

            if (string.Compare(poke, Strings.All, true) == 0)
            {
                var result = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_INVASION_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Invasions.Count.ToString("N0")));
                if (!result)
                    return;

                subscription.Invasions.ForEach(x => x.Id.Remove<InvasionSubscription>());

                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_INVASION_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            foreach (var item in validation.Valid)
            {
                var pokemonId = item.Key;
                var subInvasion = subscription.Invasions.FirstOrDefault(x => x.RewardPokemonId == pokemonId);
                // Check if subscribed
                if (subInvasion == null)
                    return;

                foreach (var area in areas)
                {
                    if (subInvasion.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subInvasion.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subInvasion.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the invasion subscription
                //if (subInvasion.Areas.Count == 0)
                // If no city specified then remove the whole subscription
                if (string.IsNullOrEmpty(city))
                {
                    // If no more areas set for the invasion subscription, delete it
                    if (!subInvasion.Id.Remove<InvasionSubscription>())
                    {
                        _logger.Error($"Unable to remove invasion subscription for user id {subInvasion.UserId} from guild id {subInvasion.GuildId}");
                    }
                }
                else
                {
                    // Save/update invasion subscription if cities still assigned
                    subInvasion.Save();
                }
            }
            subscription.Save();

            var valid = validation.Valid.Keys.Select(x => MasterFile.GetPokemon(x, 0).Name);
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_INVASION_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(poke, Strings.All, true) == 0 ? Strings.All : string.Join(", ", valid),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        public static PokemonType GetPokemonTypeFromString(string pokemonType)
        {
            var type = pokemonType.ToLower();
            if (type.Contains("bug"))
                return PokemonType.Bug;
            else if (type.Contains("dark"))
                return PokemonType.Dark;
            else if (type.Contains("dragon"))
                return PokemonType.Dragon;
            else if (type.Contains("electric"))
                return PokemonType.Electric;
            else if (type.Contains("fairy"))
                return PokemonType.Fairy;
            else if (type.Contains("fighting") || type.Contains("fight"))
                return PokemonType.Fighting;
            else if (type.Contains("fire"))
                return PokemonType.Fire;
            else if (type.Contains("flying") || type.Contains("fly"))
                return PokemonType.Flying;
            else if (type.Contains("ghost"))
                return PokemonType.Ghost;
            else if (type.Contains("grass"))
                return PokemonType.Grass;
            else if (type.Contains("ground"))
                return PokemonType.Ground;
            else if (type.Contains("ice"))
                return PokemonType.Ice;
            //else if (type.Contains("tierii") || type.Contains("none") || type.Contains("tier2") || type.Contains("t2"))
            //    return PokemonType.None;
            else if (type.Contains("normal"))
                return PokemonType.Normal;
            else if (type.Contains("poison"))
                return PokemonType.Poison;
            else if (type.Contains("psychic"))
                return PokemonType.Psychic;
            else if (type.Contains("rock"))
                return PokemonType.Rock;
            else if (type.Contains("steel"))
                return PokemonType.Steel;
            else if (type.Contains("water"))
                return PokemonType.Water;
            else
                return PokemonType.None;
        }

        #endregion

        #region Pvpme / Pvpmenot

        [
            Command("pvpme"),
            Description("")
        ]
        public async Task PvpMeAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokemon name(s) and/or Pokedex IDs to subscribe to Pokemon spawn notifications.")] string poke,
            [Description("PvP league")] string league,
            [Description("Minimum PvP ranking.")] int minimumRank = 5,
            [Description("Minimum PvP rank percentage.")] double minimumPercent = 0.0,
            [Description("City or area to add to the subscription, 'All' for all areas, or leave blank to use set distance."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var pvpLeague = string.Compare(league, "great", true) == 0 ?
                PvPLeague.Great :
                string.Compare(league, "ultra", true) == 0 ?
                    PvPLeague.Ultra :
                    string.Compare(league, "master", true) == 0 ?
                        PvPLeague.Master :
                        PvPLeague.Other;

            if (pvpLeague == PvPLeague.Other)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_LEAGUE").FormatText(ctx.User.Username, league), DiscordColor.Red);
                return;
            }

            //You may only subscribe to the top 100 or higher rank.
            if (minimumRank < Strings.MinimumRank || minimumRank > Strings.MaximumRank)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_RANK_RANGE").FormatText(ctx.User.Username, minimumRank), DiscordColor.Red);
                return;
            }

            if (minimumPercent < Strings.MinimumPercent || minimumPercent > Strings.MaximumPercent)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_RANK_RANGE").FormatText(ctx.User.Username, minimumPercent), DiscordColor.Red);
                return;
            }

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxPvPSubscriptions > 0 && subscription.PvP.Count >= server.Subscriptions.MaxPvPSubscriptions)
            {
                // Max limit for PvP subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxPvPSubscriptions), DiscordColor.Red);
                return;
            }

            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();
            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            if (areas.Count == 0 && subscription.DistanceM == 0)
            {
                await ctx.RespondEmbed($"{ctx.User.Username}#{ctx.User.Discriminator} You must either set a distance to receive notifications from (`set-distance <meters> <latitude>,<longitude>`) or provide a city/area for the subscription. Aborting request.", DiscordColor.Red);
                return;
            }

            foreach (var (pokemonId, form) in validation.Valid)
            {
                if (!MasterFile.Instance.Pokedex.ContainsKey(pokemonId))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_ID").FormatText(ctx.User.Username, pokemonId), DiscordColor.Red);
                    continue;
                }

                var pokemon = MasterFile.Instance.Pokedex[pokemonId];
                var name = string.IsNullOrEmpty(form) ? pokemon.Name : pokemon.Name + "-" + form;
                var subPkmn = subscription.PvP.FirstOrDefault(x => x.PokemonId == pokemonId &&
                                                                   (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0) &&
                                                                   x.League == pvpLeague);
                if (subPkmn == null)
                {
                    //Does not exist, create.
                    subscription.PvP.Add(new PvPSubscription
                    {
                        GuildId = guildId,
                        UserId = ctx.User.Id,
                        PokemonId = pokemonId,
                        Form = form,
                        League = pvpLeague,
                        MinimumRank = minimumRank,
                        MinimumPercent = minimumPercent,
                        Areas = areas
                    });
                    subscribed.Add(name);
                    continue;
                }

                //Exists, check if anything changed.
                if (minimumRank != subPkmn.MinimumRank ||
                    minimumPercent != subPkmn.MinimumPercent ||
                    !SubscriptionAreas.ContainsCity(subPkmn.Areas, areas))
                {
                    subPkmn.MinimumRank = minimumRank;
                    subPkmn.MinimumPercent = minimumPercent;
                    foreach (var area in areas)
                    {
                        if (!subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subPkmn.Areas.Add(area);
                        }
                    }
                    subscribed.Add(name);
                    continue;
                }

                //Already subscribed to the same Pokemon and form
                alreadySubscribed.Add(name);
            }

            var result = subscription.Save();

            if (subscribed.Count == 0 && alreadySubscribed.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SPECIFIED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var isAll = string.Compare(Strings.All, poke, true) == 0;
            var isGen = false;
            for (var i = 1; i < 6; i++)
            {
                if (string.Compare("Gen" + i, poke, true) == 0)
                {
                    isGen = true;
                    break;
                }
            }

            await ctx.RespondEmbed
            (
                (subscribed.Count > 0
                    ? $"{ctx.User.Username} has subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", subscribed))}** notifications with a minimum {pvpLeague} League PvP ranking of {minimumRank} or higher and a minimum ranking percentage of {minimumPercent}% and from the following areas: {(areas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", areas))}."
                    : string.Empty) +
                (alreadySubscribed.Count > 0
                    ? $"\r\n{ctx.User.Username} is already subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", alreadySubscribed))}** notifications with a minimum {pvpLeague} League PvP ranking of '{minimumRank}' or higher and a minimum ranking percentage of {minimumPercent}% and from the following areas: {(areas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", areas))}."
                    : string.Empty)
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("pvpmenot"),
            Description("")
        ]
        public async Task PvpMeNotAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokemon name(s) and/or Pokedex IDs to subscribe to Pokemon spawn notifications.")] string poke,
            [Description("PvP league")] string league,
            [Description("City or area to remove from the subscription, 'All' for all areas, or leave blank to remove the whole subscription."), RemainingText] string city = null)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.PvP?.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_NO_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var pvpLeague = string.Compare(league, "great", true) == 0 ?
                PvPLeague.Great :
                string.Compare(league, "ultra", true) == 0 ?
                    PvPLeague.Ultra :
                    string.Compare(league, "master", true) == 0 ?
                        PvPLeague.Master :
                        PvPLeague.Other;

            if (pvpLeague == PvPLeague.Other)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_PVP_LEAGUE").FormatText(ctx.User.Username, league), DiscordColor.Red);
                return;
            }

            if (string.Compare(poke, Strings.All, true) == 0)
            {
                var confirm = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_PVP_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.PvP.Count(x => x.League == pvpLeague).ToString("N0"), pvpLeague));
                if (!confirm)
                    return;

                subscription.PvP
                    .Where(x => x.League == pvpLeague)?
                    .ToList()?
                    .ForEach(x => x.Id.Remove<PvPSubscription>());

                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_PVP_SUBSCRIPTIONS").FormatText(ctx.User.Username, pvpLeague));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var validation = PokemonValidation.Validate(poke, (int)_dep.WhConfig.MaxPokemonId);
            if (validation.Valid == null || validation.Valid.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(ctx.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            await RemovePvPSubscription(ctx, subscription, validation, pvpLeague, areas);
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Lureme / Luremenot

        [
             Command("lureme"),
             Description("Subscribe to Pokestop lure notifications based on the lure type.")
         ]
        public async Task LureMeAsync(CommandContext ctx,
             [Description("Comma delimited list of Pokestop lures to subscribe to notifications.")] string lureTypes = "all",
             [Description("City to send the notification if the lure appears in otherwise if null all will be sent."), RemainingText] string city = "all")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            // Check subscription limits
            if (server.Subscriptions.MaxLureSubscriptions > 0 && subscription.Lures.Count >= server.Subscriptions.MaxLureSubscriptions)
            {
                // Max limit for Lure subscriptions reached
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_LURE_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxLureSubscriptions), DiscordColor.Red);
                return;
            }

            var areas = SubscriptionAreas.GetAreas(server, city);
            var lures = GetLures(lureTypes);
            foreach (var lureType in lures)
            {
                var subLure = subscription.Lures.FirstOrDefault(x => x.LureType == lureType);
                if (subLure != null)
                {
                    // Existing lure subscription
                    // Loop all areas, check if the area is already in subs, if not add it
                    foreach (var area in areas)
                    {
                        if (!subLure.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subLure.Areas.Add(area);
                        }
                    }
                    // Save quest subscription and continue;
                    // REVIEW: Might not be needed
                    subLure.Save();
                }
                else
                {
                    // New lure subscription
                    subscription.Lures.Add(new LureSubscription
                    {
                        GuildId = guildId,
                        UserId = ctx.User.Id,
                        LureType = lureType,
                        Areas = areas
                    });
                }
            }
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_LURE_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(lureTypes, Strings.All, true) == 0 ? Strings.All : string.Join(", ", lures),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        [
            Command("luremenot"),
            Description("Unsubscribe from one or all subscribed Pokestop lure notifications by lure type.")
        ]
        public async Task LureMeNotAsync(CommandContext ctx,
            [Description("Comma delimited list of Pokestop lures to unsubscribe from notifications.")] string lureTypes = "all",
            [Description("City to send the notification if the raid appears in otherwise if null all will be sent."), RemainingText] string city = "all")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null || subscription?.Lures.Count == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_LURE_SUBSCRIPTIONS").FormatText(ctx.User.Username, string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city)),
                    DiscordColor.Red
                );
                return;
            }

            if (string.Compare(lureTypes, Strings.All, true) == 0)
            {
                var result = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_LURE_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Lures.Count.ToString("N0")));
                if (!result)
                    return;

                subscription.Lures.ForEach(x => x.Id.Remove<LureSubscription>());

                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_LURE_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                return;
            }

            var areas = SubscriptionAreas.GetAreas(_dep.WhConfig.Servers[guildId], city);
            var lures = GetLures(lureTypes);
            foreach (var lureType in lures)
            {
                var subLure = subscription.Lures.FirstOrDefault(x => x.LureType == lureType);
                // Check if subscribed
                if (subLure == null)
                    return;

                foreach (var area in areas)
                {
                    if (subLure.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subLure.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subLure.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the lure subscription
                if (subLure.Areas.Count == 0)
                {
                    // If no more areas set for the lure subscription, delete it
                    if (!subLure.Id.Remove<LureSubscription>())
                    {
                        _logger.Error($"Unable to remove lure subscription for user id {subLure.UserId} from guild id {subLure.GuildId}");
                    }
                }
                else
                {
                    // Save/update lure subscription if cities still assigned
                    subLure.Save();
                }
            }
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_LURE_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                ctx.User.Username,
                string.Compare(lureTypes, Strings.All, true) == 0 ? Strings.All : string.Join(", ", lures),
                string.IsNullOrEmpty(city)
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(city))
            );

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Add / Remove

        [
            Command("add"),
            Description("Easily add subscriptions via guided messages.")
        ]
        public async Task AddAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);

            var subType = await ctx.GetSubscriptionTypeSelection();
            // TODO: Maybe show current settings for selected info
            switch (subType)
            {
                case 1: // Pokemon
                    #region Pokemon
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxPokemonSubscriptions > 0 && subscription.Pokemon.Count >= server.Subscriptions.MaxPokemonSubscriptions)
                        {
                            // Max limit for Pokemon subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxPokemonSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var pkmnInput = new PokemonSubscriptionInput(ctx);
                        var pkmnResult = await pkmnInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var ivResult = await pkmnInput.GetIVResult();
                        var lvlResult = await pkmnInput.GetLevelResult();
                        var genderResult = await pkmnInput.GetGenderResult();
                        var areasResult = await pkmnInput.GetAreasResult(guildId);

                        var validPokemonNames = string.Join(", ", pkmnResult.Valid.Keys);
                        var result = await AddPokemonSubscription(ctx, subscription, pkmnResult, ivResult, lvlResult.MinimumLevel, lvlResult.MaximumLevel, genderResult, areasResult);

                        var subscribed = result.Key;
                        var alreadySubscribed = result.Value;
                        if (subscribed.Count == 0 && alreadySubscribed.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SPECIFIED").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
                        var isGen = false;
                        for (var i = 1; i < 6; i++)
                        {
                            if (string.Compare("Gen" + i, validPokemonNames, true) == 0)
                            {
                                isGen = true;
                                break;
                            }
                        }

                        var msg = $@"
{(ivResult.Attack >= 0 || ivResult.Defense >= 0 || ivResult.Stamina >= 0 ? $"an IV value of {ivResult.Attack}/{ivResult.Defense}/{ivResult.Stamina}" : $" a minimum IV of {ivResult.IV}%")}
{(lvlResult.MinimumLevel > 0 ? $"and between levels {lvlResult.MinimumLevel}-{lvlResult.MaximumLevel}" : null)}
{(genderResult == "*" ? null : $" and only '{genderResult}' gender types")}
and only from the following areas: {(areasResult.Count == server.Geofences.Count ? Strings.All : string.Join(", ", areasResult))}.
                    ";

                        await ctx.RespondEmbed
                        (
                            (subscribed.Count > 0
                                ? $"{ctx.User.Username} has subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", subscribed))}** notifications with {msg}."
                                : string.Empty) +
                            (alreadySubscribed.Count > 0
                                ? $"\r\n{ctx.User.Username} is already subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", alreadySubscribed))}** notifications with {msg}."
                                : string.Empty)
                        );
                    }
                    #endregion
                    break;
                case 2: // PVP
                    #region PvP
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxPvPSubscriptions > 0 && subscription.PvP.Count >= server.Subscriptions.MaxPvPSubscriptions)
                        {
                            // Max limit for PvP Pokemon subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxPvPSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var pvpInput = new PvPSubscriptionInput(ctx);
                        var pvpPokemon = await pvpInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var pvpLeague = await pvpInput.GetLeagueResult();
                        var pvpRank = await pvpInput.GetRankResult();
                        var pvpPercent = await pvpInput.GetPercentResult();
                        var pvpAreas = await pvpInput.GetAreasResult(guildId);

                        var validPokemonNames = string.Join(", ", pvpPokemon.Valid.Keys);
                        var pvpResult = await AddPvPSubscription(ctx, subscription, pvpPokemon, pvpLeague, pvpRank, pvpPercent, pvpAreas);
                        var subscribed = pvpResult.Key;
                        var alreadySubscribed = pvpResult.Value;
                        if (subscribed.Count == 0 && alreadySubscribed.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SPECIFIED").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
                        var isGen = false;
                        for (var i = 1; i < 6; i++)
                        {
                            if (string.Compare("Gen" + i, validPokemonNames, true) == 0)
                            {
                                isGen = true;
                                break;
                            }
                        }

                        await ctx.RespondEmbed
                        (
                            (subscribed.Count > 0
                                ? $"{ctx.User.Username} has subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", subscribed))}** notifications with a minimum {pvpLeague} League PvP ranking of {pvpRank} or higher and a minimum ranking percentage of {pvpPercent}% and from the following areas: {(pvpAreas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", pvpAreas))}."
                                : string.Empty) +
                            (alreadySubscribed.Count > 0
                                ? $"\r\n{ctx.User.Username} is already subscribed to **{(isAll || isGen ? "All" : string.Join("**, **", alreadySubscribed))}** notifications with a minimum {pvpLeague} League PvP ranking of '{pvpRank}' or higher and a minimum ranking percentage of {pvpPercent}% and from the following areas: {(pvpAreas.Count == server.Geofences.Count ? Strings.All : string.Join(", ", pvpAreas))}."
                                : string.Empty)
                        );
                    }
                    #endregion
                    break;
                case 3: // Raids
                    #region Raids
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxRaidSubscriptions > 0 && subscription.Raids.Count >= server.Subscriptions.MaxRaidSubscriptions)
                        {
                            // Max limit for Raid subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_RAID_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxRaidSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var raidInput = new RaidSubscriptionInput(ctx);
                        var raidPokemon = await raidInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var raidAreas = await raidInput.GetAreasResult(guildId);

                        var validPokemonNames = string.Join(", ", raidPokemon.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value)));
                        var raidResult = AddRaidSubscription(ctx, subscription, raidPokemon, raidAreas);
                        var subscribed = raidResult.Key;
                        var alreadySubscribed = raidResult.Value;
                        if (subscribed.Count == 0 && alreadySubscribed.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_SPECIFIED").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
                        /*
                        var isGen = false;
                        for (var i = 1; i < 6; i++)
                        {
                            if (string.Compare("Gen" + i, validPokemonNames, true) == 0)
                            {
                                isGen = true;
                                break;
                            }
                        }
                        */
                        await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_RAID_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                            ctx.User.Username,
                            isAll ? Strings.All : validPokemonNames,
                            raidAreas.Count == server.Geofences.Count
                                ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                                : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(string.Join(", ", raidAreas))
                        ));
                    }
                    #endregion
                    break;
                case 4: // Quests
                    #region Quests
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxQuestSubscriptions > 0 && subscription.Quests.Count >= server.Subscriptions.MaxQuestSubscriptions)
                        {
                            // Max limit for Quest subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_QUEST_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxQuestSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var questInput = new QuestSubscriptionInput(ctx);
                        var rewardKeyword = await questInput.GetRewardInput();
                        var areas = await questInput.GetAreasResult(guildId);

                        var subQuest = subscription.Quests.FirstOrDefault(x => string.Compare(x.RewardKeyword, rewardKeyword, true) == 0);
                        if (subQuest != null)
                        {
                            // Existing quest subscription
                            // Loop all areas, check if the area is already in subs, if not add it
                            foreach (var area in areas)
                            {
                                if (!subQuest.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                                {
                                    subQuest.Areas.Add(area);
                                }
                            }
                            // Save quest subscription and continue;
                            // REVIEW: Might not be needed
                            subQuest.Save();
                        }
                        else
                        {
                            subscription.Quests.Add(new QuestSubscription
                            {
                                GuildId = guildId,
                                UserId = ctx.User.Id,
                                RewardKeyword = rewardKeyword,
                                Areas = areas
                            });
                        }

                        subscription.Save();
                        await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_QUEST_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                            ctx.User.Username,
                            rewardKeyword,
                            areas.Count == server.Geofences.Count
                                ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                                : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(string.Join(", ", areas))
                        ));
                    }
                    #endregion
                    break;
                case 5: // Invasions
                    #region
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxInvasionSubscriptions > 0 && subscription.Invasions.Count >= server.Subscriptions.MaxInvasionSubscriptions)
                        {
                            // Max limit for Invasion subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_INVASION_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxInvasionSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var invasionInput = new InvasionSubscriptionInput(ctx);
                        var invasionPokemon = await invasionInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var invasionAreas = await invasionInput.GetAreasResult(guildId);

                        var validPokemonNames = string.Join(", ", invasionPokemon.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name));
                        foreach (var (pokemonId, form) in invasionPokemon.Valid)
                        {
                            var subInvasion = subscription.Invasions.FirstOrDefault(x => x.RewardPokemonId == pokemonId);
                            if (subInvasion != null)
                            {
                                // Existing invasion subscription
                                // Loop all areas, check if the area is already in subs, if not add it
                                foreach (var area in invasionAreas)
                                {
                                    if (!subInvasion.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                                    {
                                        subInvasion.Areas.Add(area);
                                    }
                                }
                                // Save quest subscription and continue;
                                // REVIEW: Might not be needed
                                subInvasion.Save();
                            }
                            else
                            {
                                // New invasion subscription
                                subscription.Invasions.Add(new InvasionSubscription
                                {
                                    GuildId = guildId,
                                    UserId = ctx.User.Id,
                                    RewardPokemonId = pokemonId,
                                    Areas = invasionAreas
                                });
                            }
                        }
                        var result = subscription.Save();
                        if (!result)
                        {
                        }

                        var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
                        var valid = invasionPokemon.Valid.Keys.Select(x => MasterFile.GetPokemon(x, 0).Name);
                        await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_INVASION_SUBSCRIPTIONS_SUBSCRIBE").FormatText(
                            ctx.User.Username,
                            isAll ? Strings.All : validPokemonNames,
                            invasionAreas.Count == server.Geofences.Count
                                ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                                : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(string.Join(", ", invasionAreas))
                        ));
                    }
                    #endregion
                    break;
                case 6: // Gyms
                    #region Gyms
                    {
                        // Check subscription limits
                        if (server.Subscriptions.MaxGymSubscriptions > 0 && subscription.Gyms.Count >= server.Subscriptions.MaxGymSubscriptions)
                        {
                            // Max limit for Gym subscriptions reached
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_GYM_SUBSCRIPTIONS_LIMIT", ctx.User.Username, server.Subscriptions.MaxGymSubscriptions), DiscordColor.Red);
                            return;
                        }

                        var gymMessage = (await ctx.RespondEmbed($"Enter a gym name (or partial name) to get raid subscriptions from:", DiscordColor.Blurple)).FirstOrDefault();
                        var gymName = await ctx.WaitForUserChoice();
                        await gymMessage.DeleteAsync();

                        if (string.IsNullOrEmpty(gymName))
                        {
                            await ctx.RespondEmbed($"{ctx.User.Username} Gym Name must not be empty", DiscordColor.Blurple);
                            return;
                        }

                        var gymExists = subscription.Gyms.Exists(x => string.Compare(x.Name, gymName, true) == 0);
                        if (gymExists)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_EXISTS").FormatText(ctx.User.Username, gymName), DiscordColor.Red);
                            return;
                        }

                        subscription.Gyms.Add(new GymSubscription
                        {
                            GuildId = guildId,
                            UserId = ctx.User.Id,
                            Name = gymName
                        });
                        subscription.Save();

                        await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_ADDED").FormatText(ctx.User.Username, gymName));
                        _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                    }
                    break;
                    #endregion
                case 0: // Invalid entry
                    await ctx.RespondEmbed($"Invalid entry specified, please try again...", DiscordColor.Red);
                    break;
            }
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        private async Task<KeyValuePair<List<string>, List<string>>> AddPokemonSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, IVResult ivResult, int minLevel, int maxLevel, string gender, List<string> areas)
        {
            var subscribed = new List<string>();
            var alreadySubscribed = new List<string>();
            foreach (var (pokemonId, form) in validation.Valid)
            {
                if (!MasterFile.Instance.Pokedex.ContainsKey(pokemonId))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_ID").FormatText(ctx.User.Username, pokemonId), DiscordColor.Red);
                    continue;
                }

                var pokemon = MasterFile.Instance.Pokedex[pokemonId];
                var name = string.IsNullOrEmpty(form) ? pokemon.Name : pokemon.Name + "-" + form;
                var isModOrHigher = await ctx.Client.IsModeratorOrHigher(ctx.User.Id, subscription.GuildId, _dep.WhConfig);

                // Check if common type pokemon e.g. Pidgey, Ratatta, Spinarak 'they are beneath him and he refuses to discuss them further'
                if (pokemonId.IsCommonPokemon() && ivResult.IV < Strings.CommonTypeMinimumIV && !isModOrHigher)
                {
                    await ctx.TriggerTypingAsync();
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_COMMON_TYPE_POKEMON").FormatText(ctx.User.Username, pokemon.Name, Strings.CommonTypeMinimumIV), DiscordColor.Red);
                    continue;
                }

                var subPkmn = subscription.Pokemon.FirstOrDefault(x => x.PokemonId == pokemonId && string.Compare(x.Form, form, true) == 0);
                // Always ignore the user's input for Unown and set it to 0 by default.
                var minIV = pokemonId.IsRarePokemon() ? 0 : ivResult.IV;
                var minLvl = pokemonId.IsRarePokemon() ? 0 : minLevel;
                var maxLvl = pokemonId.IsRarePokemon() ? 35 : maxLevel;
                var hasStatsSet = ivResult.Attack >= 0 || ivResult.Defense >= 0 || ivResult.Stamina >= 0;

                if (subPkmn == null)
                {
                    // Does not exist, create.
                    subscription.Pokemon.Add(new PokemonSubscription
                    {
                        GuildId = subscription.GuildId,
                        UserId = ctx.User.Id,
                        PokemonId = pokemonId,
                        Form = form,
                        MinimumIV = minIV,
                        MinimumLevel = minLvl,
                        MaximumLevel = maxLvl,
                        Gender = gender,
                        IVList = hasStatsSet ? new List<string> { $"{ivResult.Attack}/{ivResult.Defense}/{ivResult.Stamina}" } : new List<string>(),
                        Areas = areas
                    });
                    subscribed.Add(name);
                    continue;
                }

                // Exists, check if anything changed.
                if (ivResult.IV != subPkmn.MinimumIV ||
                    string.Compare(form, subPkmn.Form, true) != 0 ||
                    minLvl != subPkmn.MinimumLevel ||
                    maxLvl != subPkmn.MaximumLevel ||
                    gender != subPkmn.Gender ||
                    (!subPkmn.IVList.Contains($"{ivResult.Attack}/{ivResult.Defense}/{ivResult.Stamina}") && hasStatsSet) ||
                    !SubscriptionAreas.ContainsCity(subPkmn.Areas, areas))
                {
                    subPkmn.Form = form;
                    subPkmn.MinimumIV = hasStatsSet ? subPkmn.MinimumIV : ivResult.IV;
                    subPkmn.MinimumLevel = minLvl;
                    subPkmn.MaximumLevel = maxLvl;
                    subPkmn.Gender = gender;
                    if (hasStatsSet)
                    {
                        subPkmn.IVList.Add($"{ivResult.Attack}/{ivResult.Defense}/{ivResult.Stamina}");
                    }
                    foreach (var area in areas)
                    {
                        if (!subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subPkmn.Areas.Add(area);
                        }
                    }
                    subscribed.Add(name);
                    continue;
                }

                // Already subscribed to the same Pokemon and form
                alreadySubscribed.Add(name);
            }
            var result = subscription.Save();
            if (!result)
            {
            }
            return new KeyValuePair<List<string>, List<string>>(subscribed, alreadySubscribed);
        }

        private async Task<KeyValuePair<List<string>, List<string>>> AddPvPSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, PvPLeague league, int minRank, double minPercent, List<string> areas)
        {
            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();
            foreach (var (pokemonId, form) in validation.Valid)
            {
                if (!MasterFile.Instance.Pokedex.ContainsKey(pokemonId))
                {
                    await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_ID").FormatText(ctx.User.Username, pokemonId), DiscordColor.Red);
                    continue;
                }

                var pokemon = MasterFile.Instance.Pokedex[pokemonId];
                var name = string.IsNullOrEmpty(form) ? pokemon.Name : pokemon.Name + "-" + form;
                var subPkmn = subscription.PvP.FirstOrDefault(x => x.PokemonId == pokemonId &&
                                                                   (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0) &&
                                                                   x.League == league);
                if (subPkmn == null)
                {
                    //Does not exist, create.
                    subscription.PvP.Add(new PvPSubscription
                    {
                        GuildId = subscription.GuildId,
                        UserId = ctx.User.Id,
                        PokemonId = pokemonId,
                        Form = form,
                        League = league,
                        MinimumRank = minRank,
                        MinimumPercent = minPercent,
                        Areas = areas
                    });
                    subscribed.Add(name);
                    continue;
                }

                //Exists, check if anything changed.
                if (minRank != subPkmn.MinimumRank ||
                    minPercent != subPkmn.MinimumPercent ||
                    !SubscriptionAreas.ContainsCity(subPkmn.Areas, areas))
                {
                    subPkmn.MinimumRank = minRank;
                    subPkmn.MinimumPercent = minPercent;
                    foreach (var area in areas)
                    {
                        if (!subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subPkmn.Areas.Add(area);
                        }
                    }
                    subscribed.Add(name);
                    continue;
                }

                //Already subscribed to the same Pokemon and form
                alreadySubscribed.Add(name);
            }

            var result = subscription.Save();
            if (!result)
            {
            }
            return new KeyValuePair<List<string>, List<string>>(subscribed, alreadySubscribed);
        }

        private KeyValuePair<List<string>, List<string>> AddRaidSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, List<string> areas)
        {
            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();
            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subRaid = subscription.Raids.FirstOrDefault(x => x.PokemonId == pokemonId && string.Compare(x.Form, form, true) == 0);
                if (subRaid != null)
                {
                    // Existing raid subscription
                    // Loop all areas, check if the area is already in subs, if not add it
                    foreach (var area in areas)
                    {
                        if (!subRaid.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                        {
                            subRaid.Areas.Add(area);
                        }
                    }
                    // Save raid subscription and continue;
                    // REVIEW: Might not be needed
                    subRaid.Save();
                    continue;
                }

                // New raid subscription
                subscription.Raids.Add(new RaidSubscription
                {
                    GuildId = subscription.GuildId,
                    UserId = ctx.User.Id,
                    PokemonId = pokemonId,
                    Form = form,
                    Areas = areas
                });
            }
            var result = subscription.Save();
            if (!result)
            {
            }
            return new KeyValuePair<List<string>, List<string>>(subscribed, alreadySubscribed);
        }

        [
            Command("remove"),
            Aliases("rem", "rm", "r"),
            Description("Easily remove subscriptions via guided messages.")
        ]
        public async Task RemoveAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));
            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);

            var subType = await ctx.GetSubscriptionTypeSelection();
            // TODO: Maybe show current settings for selected info
            switch (subType)
            {
                case 1: // Pokemon
                    #region Pokemon
                    {
                        if (subscription == null || subscription?.Pokemon.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_NO_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var pkmnInput = new PokemonSubscriptionInput(ctx);
                        var pkmnResult = await pkmnInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var areasResult = await pkmnInput.GetAreasResult(guildId);

                        await RemovePokemonSubscription(ctx, subscription, pkmnResult, areasResult);
                        break;
                    }
                    #endregion
                case 2: // PVP
                    #region PvP
                    {
                        if (subscription == null || subscription?.PvP.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_NO_POKEMON_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var pvpInput = new PvPSubscriptionInput(ctx);
                        var pvpPokemonResult = await pvpInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var pvpLeagueResult = await pvpInput.GetLeagueResult();
                        var pvpAreasResult = await pvpInput.GetAreasResult(guildId);

                        await RemovePvPSubscription(ctx, subscription, pvpPokemonResult, pvpLeagueResult, pvpAreasResult);
                    }
                    #endregion
                    break;
                case 3: // Raids
                    #region Raids
                    {
                        if (subscription == null || subscription?.Raids.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_RAID_SUBSCRIPTIONS").FormatText(ctx.User.Username, DiscordColor.Red));
                            return;
                        }

                        var raidInput = new RaidSubscriptionInput(ctx);
                        var raidPokemonResult = await raidInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var raidAreasResult = await raidInput.GetAreasResult(guildId);

                        await RemoveRaidSubscription(ctx, subscription, null, raidAreasResult);
                    }
                    #endregion
                    break;
                case 4: // Quests
                    #region Quests
                    {
                        if (subscription == null || subscription?.Quests.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_QUEST_SUBSCRIPTIONS").FormatText(ctx.User.Username, DiscordColor.Red));
                            return;
                        }

                        var questInput = new QuestSubscriptionInput(ctx);
                        var rewardResult = await questInput.GetRewardInput();
                        var areasResult = await questInput.GetAreasResult(guildId);

                        var notSubscribed = new List<string>();
                        var unsubscribed = new List<string>();
                        await ctx.TriggerTypingAsync();

                        if (string.Compare(rewardResult, Strings.All, true) == 0)
                        {
                            var removeAllResult = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_QUEST_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Quests.Count.ToString("N0")));
                            if (!removeAllResult)
                                return;

                            subscription.Quests.ForEach(x => x.Id.Remove<QuestSubscription>());
                            await ctx.TriggerTypingAsync();
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_CONFIRM_SUCCESS_ALL_QUEST_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                            return;
                        }

                        var subQuest = subscription.Quests.FirstOrDefault(x => string.Compare(x.RewardKeyword, rewardResult, true) == 0);
                        // Check if subscribed
                        if (subQuest == null)
                            return;

                        foreach (var area in areasResult)
                        {
                            if (subQuest.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                            {
                                var index = subQuest.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                                subQuest.Areas.RemoveAt(index);
                            }
                        }

                        // Check if there are no more areas set for the Pokemon subscription
                        if (subQuest.Areas.Count == 0)
                        {
                            // If no more areas set for the Pokemon subscription, delete it
                            if (!subQuest.Id.Remove<QuestSubscription>())
                            {
                                _logger.Error($"Unable to remove quest subscription for user id {subQuest.UserId} from guild id {subQuest.GuildId}");
                            }
                        }
                        else
                        {
                            // Save/update quest subscription if cities still assigned
                            subQuest.Save();
                        }
                        subscription.Save();

                        await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_QUEST_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                            ctx.User.Username,
                            rewardResult,
                            areasResult.Count == server.Geofences.Count
                                ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                                : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(string.Join(", ", areasResult)))
                        );
                    }
                    #endregion
                    break;
                case 5: // Invasions
                    #region
                    {
                        if (subscription == null || subscription?.Invasions.Count == 0)
                        {
                            await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NO_INVASION_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                            return;
                        }

                        var invasionInput = new InvasionSubscriptionInput(ctx);
                        var invasionPokemonResult = await invasionInput.GetPokemonResult(_dep.WhConfig.MaxPokemonId);
                        var invasionAreasResult = await invasionInput.GetAreasResult(guildId);

                        foreach (var item in invasionPokemonResult.Valid)
                        {
                            var pokemonId = item.Key;
                            var subInvasion = subscription.Invasions.FirstOrDefault(x => x.RewardPokemonId == pokemonId);
                            // Check if subscribed
                            if (subInvasion == null)
                                return;

                            foreach (var area in invasionAreasResult)
                            {
                                if (subInvasion.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                                {
                                    var index = subInvasion.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                                    subInvasion.Areas.RemoveAt(index);
                                }
                            }

                            // Check if there are no more areas set for the invasion subscription
                            if (subInvasion.Areas.Count == 0)
                            {
                                // If no more areas set for the invasion subscription, delete it
                                if (!subInvasion.Id.Remove<InvasionSubscription>())
                                {
                                    _logger.Error($"Unable to remove invasion subscription for user id {subInvasion.UserId} from guild id {subInvasion.GuildId}");
                                }
                            }
                            else
                            {
                                // Save/update invasion subscription if cities still assigned
                                subInvasion.Save();
                            }
                        }
                        subscription.Save();

                        var validPokemonNames = string.Join("**, **", invasionPokemonResult.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name));
                        var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
                        await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_INVASION_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                            ctx.User.Username,
                            isAll ? Strings.All : validPokemonNames,
                            invasionAreasResult.Count == server.Geofences.Count
                                ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                                : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(validPokemonNames)
                        ));
                    }
                    #endregion
                    break;
                case 6: // Gyms
                    #region Gyms
                    {
                        var gymInput = (await ctx.RespondEmbed($"Enter a gym name to remove from receiving raid notifcations:", DiscordColor.Blurple)).FirstOrDefault();
                        var gymName = await ctx.WaitForUserChoice();
                        await gymInput.DeleteAsync();

                        if (string.Compare(Strings.All, gymName, true) == 0)
                        {
                            var result = await ctx.Confirm(Translator.Instance.Translate("NOTIFY_CONFIRM_REMOVE_ALL_GYM_SUBSCRIPTIONS").FormatText(ctx.User.Username, subscription.Gyms.Count.ToString("N0")));
                            if (!result)
                                return;

                            subscription.Gyms.ForEach(x => x.Id.Remove<GymSubscription>());
                            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_SUCCESS_REMOVE_ALL_GYM_SUBSCRIPTIONS").FormatText(ctx.User.Username));
                            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
                            return;
                        }

                        subscription.Gyms
                            .Where(x => string.Compare(x.Name, gymName, true) == 0)?
                            .ToList()?
                            .ForEach(x => x.Id.Remove<GymSubscription>());
                        await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_GYM_SUBSCRIPTION_REMOVED").FormatText(ctx.User.Username, gymName));
                    }
                    break;
                    #endregion
                case 0: // Invalid entry
                    await ctx.RespondEmbed($"Invalid entry specified, please try again...", DiscordColor.Red);
                    break;
            }

            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        private async Task RemovePokemonSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, List<string> areas)
        {
            var error = false;
            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subPkmn = subscription.Pokemon.FirstOrDefault(x => x.PokemonId == pokemonId && (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0));
                if (subPkmn == null)
                    continue;

                foreach (var area in areas)
                {
                    // TODO: Remove all areas to prevent lingering ones?
                    if (subPkmn.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subPkmn.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subPkmn.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the Pokemon subscription
                if (areas.Count == 0)
                {
                    // If no more areas set for the Pokemon subscription, delete it
                    var result = subPkmn.Id.Remove<PokemonSubscription>();
                    if (!result)
                    {
                        error = true;
                        //TODO: Collect list of failed.
                    }
                }
                else
                {
                    // Save/update Pokemon subscription if cities still assigned
                    subPkmn.Save();
                }
            }

            if (error)
            {
                var pokemonNames = validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value));
                await ctx.RespondEmbed(Translator.Instance.Translate("FAILED_POKEMON_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(ctx.User.Username, pokemonNames), DiscordColor.Red);
                return;
            }
        }

        private async Task RemovePvPSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, PvPLeague league, List<string> areas)
        {
            var error = false;
            var pokemonNames = validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value));
            foreach (var (pokemonId, form) in validation.Valid)
            {
                var subPvP = subscription.PvP.FirstOrDefault(x => x.PokemonId == pokemonId && (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0) && x.League == league);
                if (subPvP == null)
                    continue;

                foreach (var area in areas)
                {
                    if (subPvP.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subPvP.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subPvP.Areas.RemoveAt(index);
                    }
                }

                // If no city specified then remove the whole subscription
                if (areas.Count == 0)
                {
                    // If no more areas set for the PvP Pokemon subscription, delete it
                    var result = subPvP.Id.Remove<PvPSubscription>();
                    if (!result)
                    {
                        error = true;
                        //TODO: Collect list of failed.
                    }
                }
                else
                {
                    // Save/update PvP Pokemon subscription if cities still assigned
                    subPvP.Save();
                }
            }

            if (error)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("FAILED_POKEMON_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(ctx.User.Username, string.Join(", ", pokemonNames)), DiscordColor.Red);
                return;
            }

            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_PVP_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(ctx.User.Username, string.Join("**, **", pokemonNames), league));
        }

        private async Task RemoveRaidSubscription(CommandContext ctx, SubscriptionObject subscription, PokemonValidation validation, List<string> areas)
        {
            var server = _dep.WhConfig.Servers[subscription.GuildId];
            foreach (var item in validation.Valid)
            {
                var pokemonId = item.Key;
                var form = item.Value;
                var subRaid = subscription.Raids.FirstOrDefault(x => x.PokemonId == pokemonId && (string.IsNullOrEmpty(x.Form) || string.Compare(x.Form, form, true) == 0));
                // Check if subscribed
                if (subRaid == null)
                    continue;

                foreach (var area in areas)
                {
                    if (subRaid.Areas.Select(x => x.ToLower()).Contains(area.ToLower()))
                    {
                        var index = subRaid.Areas.FindIndex(x => string.Compare(x, area, true) == 0);
                        subRaid.Areas.RemoveAt(index);
                    }
                }

                // Check if there are no more areas set for the Pokemon subscription
                if (areas.Count == 0)
                {
                    // If no more areas set for the Pokemon subscription, delete it
                    if (!subRaid.Id.Remove<RaidSubscription>())
                    {
                        _logger.Error($"Unable to remove raid subscription for user id {subRaid.UserId} from guild id {subRaid.GuildId}");
                    }
                }
                else
                {
                    // Save/update raid subscription if cities still assigned
                    subRaid.Save();
                }
            }

            var validPokemonNames = string.Join("**, **", validation.Valid.Select(x => MasterFile.Instance.Pokedex[x.Key].Name + (string.IsNullOrEmpty(x.Value) ? string.Empty : "-" + x.Value)));;
            var isAll = string.Compare(Strings.All, validPokemonNames, true) == 0;
            await ctx.RespondEmbed(Translator.Instance.Translate("SUCCESS_RAID_SUBSCRIPTIONS_UNSUBSCRIBE").FormatText(
                ctx.User.Username,
                isAll ? Strings.All : validPokemonNames,
                areas.Count == server.Geofences.Count
                    ? Translator.Instance.Translate("SUBSCRIPTIONS_FROM_ALL_CITIES")
                    : Translator.Instance.Translate("SUBSCRIPTIONS_FROM_CITY").FormatText(validPokemonNames))
            );
        }

        #endregion

        #region Reset / Clear / Wipe

        [
            Command("reset"),
            Aliases("clear", "wipe")
        ]
        public async Task ResetAsync(CommandContext ctx,
            [Description("Discord user mention string.")] string mention = "")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            if (string.IsNullOrEmpty(mention))
            {
                var userResult = await ctx.Confirm("Are you sure you want to delete all of your subscriptions? This action is irreversible and cannot be undone.");
                if (!userResult) return;

                if (!SubscriptionManager.RemoveAllUserSubscriptions(guildId, ctx.User.Id))
                {
                    // TODO: Localize
                    await ctx.RespondEmbed($"{ctx.User.Username} failed to cleared all of your subscriptions", DiscordColor.Red);
                    return;
                }

                // TODO: Localize
                await ctx.RespondEmbed($"{ctx.User.Username} has cleared all subscriptions", DiscordColor.Green);
                return;
            }

            var isModOrHigher = await ctx.Client.IsModeratorOrHigher(ctx.User.Id, guildId, _dep.WhConfig);
            if (!isModOrHigher)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_NOT_MODERATOR_OR_HIGHER").FormatText(ctx.User.Mention), DiscordColor.Red);
                return;
            }

            var userId = ConvertMentionToUserId(mention);
            if (userId <= 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_INVALID_USER_MENTION").FormatText(ctx.User.Mention, mention), DiscordColor.Red);
                return;
            }

            var user = await ctx.Client.GetUserAsync(userId);
            if (user == null)
            {
                _logger.Warn($"Failed to get Discord user with id {userId}.");
                return;
            }

            var modResult = await ctx.Confirm($"{ctx.User.Username} is about to delete all of {user.Username}'s subscriptions. This action is irreversible and cannot be undone, are you sure you want to do this?");
            if (!modResult) return;

            if (!SubscriptionManager.RemoveAllUserSubscriptions(guildId, user.Id))
            {
                // TODO: Localize
                await ctx.RespondEmbed($"{ctx.User.Username} failed to cleared all of {user.Username}'s subscriptions", DiscordColor.Red);
                return;
            }

            // TODO: Localize
            await ctx.RespondEmbed($"{ctx.User.Username} has cleared all of {user.Username}'s subscriptions", DiscordColor.Green);
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        #region Import / Export

        [
            Command("import"),
            Description("Import your saved notification subscription settings for Pokemon, Raids, Quests, Pokestops, and Gyms.")
        ]
        public async Task ImportAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_IMPORT_UPLOAD_FILE").FormatText(ctx.User.Username));
            var xc = await _dep.Interactivity.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id && x.Attachments.Count > 0, TimeSpan.FromSeconds(180));
            if (xc == null)
                return;

            var attachment = xc.Message.Attachments[0];
            if (attachment == null)
                return;

            var data = NetUtil.Get(attachment.Url);
            if (string.IsNullOrEmpty(data))
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_IMPORT_INVALID_ATTACHMENT").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var oldSubscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (oldSubscription != null)
            {
                var result = SubscriptionManager.RemoveAllUserSubscriptions(guildId, ctx.User.Id);
                if (!result)
                {
                    _logger.Error($"Failed to clear old user subscriptions for {ctx.User.Username} ({ctx.User.Id}) in guild {ctx.Guild?.Name} ({ctx.Guild?.Id}) before importing.");
                }
            }

            var subscription = JsonConvert.DeserializeObject<SubscriptionObject>(data);
            if (subscription == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_IMPORT_MALFORMED_DATA").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }
            subscription.Save();
            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_IMPORT_SUCCESS").FormatText(ctx.User.Username));
        }

        [
            Command("export"),
            Description("Export your current notification subscription settings for Pokemon, Raids, Quests, Pokestops, and Gyms.")
        ]
        public async Task ExportAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_EXPORT_NO_SUBSCRIPTIONS").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var json = JsonConvert.SerializeObject(subscription, Formatting.Indented);
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{ctx.Guild?.Name}_{ctx.User.Username}_subscriptions_{DateTime.Now:yyyy-MM-dd}.json");
            File.WriteAllText(tmpFile, json);

            await ctx.RespondWithFileAsync(tmpFile, Translator.Instance.Translate("NOTIFY_EXPORT_SUCCESS").FormatText(ctx.User.Username));
        }

        #endregion

        #region Icon Style

        [
            Command("icons"),
            Description("List all available icon styles.")
        ]
        public async Task IconsAsync(CommandContext ctx)
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            var description = "**Available Icon Styles:**\r\n" +
                    $"- {string.Join($"{Environment.NewLine}- ", _dep.WhConfig.IconStyles.Keys)}" +
                    Environment.NewLine +
                    Environment.NewLine +
                    $"*Type `{_dep.WhConfig.Servers[guildId].CommandPrefix}set-icons iconStyle` to use that icon style when receiving notifications from {Strings.BotName}.*";
            var eb = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = description,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{ctx.Guild?.Name} | {DateTime.Now}",
                    IconUrl = ctx.Guild?.IconUrl
                }
            };

            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync(embed: eb.Build());
        }

        [
            Command("set-icons"),
            Description("Set the icon style to use when receiving notifications via direct message.")
        ]
        public async Task SetIconAsync(CommandContext ctx,
            [Description("Icon style to use.")] string iconStyle = "Default")
        {
            if (!await CanExecute(ctx))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x));

            if (!_dep.WhConfig.IconStyles.Select(x => x.Key.ToLower()).Contains(iconStyle.ToLower()))
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_ICON_STYLE").FormatText(ctx.User.Username, _dep.WhConfig.Servers[guildId].CommandPrefix), DiscordColor.Red);
                return;
            }

            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, ctx.User.Id);
            if (subscription == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_USER_NOT_SUBSCRIBED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            subscription.IconStyle = iconStyle;
            subscription.Save();

            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_ICON_STYLE_CHANGE").FormatText(ctx.User.Username, iconStyle));
            _dep.SubscriptionProcessor.Manager.ReloadSubscriptions();
        }

        #endregion

        [
            Command("stats"),
            Description("Notification statistics for alarms and subscriptions of Pokemon, Raids, and Quests.")
        ]
        public async Task StatsAsync(CommandContext ctx)
        {
            var stats = Statistics.Instance;
            var eb = new DiscordEmbedBuilder
            {
                Title = $"{DateTime.Now.ToLongDateString()} Statistics",
                Color = DiscordColor.Blurple,
                ThumbnailUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQdNi3XTIwl8tkN_D6laRdexk0fXJ-fMr0C_s4ju-bXw2kcDSRI"
            };

            var sb = new StringBuilder();
            sb.AppendLine($"__**Pokemon**__");
            sb.AppendLine($"Alarms Sent: {stats.PokemonAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedPokemon:N0}");
            sb.AppendLine($"With IV Stats: {stats.TotalReceivedPokemonWithStats:N0}");
            sb.AppendLine($"Missing IV Stats: {stats.TotalReceivedPokemonMissingStats:N0}");
            sb.AppendLine($"Subscriptions Sent: {stats.SubscriptionPokemonSent:N0}");
            sb.AppendLine();
            sb.AppendLine("__**Raids**__");
            sb.AppendLine($"Egg Alarms Sent: {stats.EggAlarmsSent:N0}");
            sb.AppendLine($"Raids Alarms Sent: {stats.RaidAlarmsSent:N0}");
            sb.AppendLine($"Total Eggs Received: {stats.TotalReceivedRaids:N0}");
            sb.AppendLine($"Total Raids Received: {stats.TotalReceivedRaids:N0}");
            sb.AppendLine($"Raid Subscriptions Sent: {stats.SubscriptionRaidsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Quests**__");
            sb.AppendLine($"Alarms Sent: {stats.QuestAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedQuests:N0}");
            sb.AppendLine($"Subscriptions Sent: {stats.SubscriptionQuestsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Invasions**__");
            sb.AppendLine($"Alarms Sent: {stats.InvasionAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedInvasions:N0}");
            sb.AppendLine($"Subscriptions Sent: {stats.SubscriptionInvasionsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Lures**__");
            sb.AppendLine($"Alarms Sent: {stats.LureAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedLures:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Gyms**__");
            sb.AppendLine($"Alarms Sent: {stats.GymAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedGyms:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Weather**__");
            sb.AppendLine($"Alarms Sent: {stats.WeatherAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {stats.TotalReceivedWeathers:N0}");
            sb.AppendLine();
            //var hundos = string.Join(Environment.NewLine, stats.Hundos.Select(x => $"{x.Key}: {MasterFile.Instance.Pokedex[x.Value.Id].Name} {x.Value.IV} IV {x.Value.CP} CP"));
            //sb.AppendLine($"**Recent 100% Spawns**");
            //sb.AppendLine(string.IsNullOrEmpty(hundos) ? "None" : hundos);

            eb.Description = sb.ToString();
            eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"{(ctx.Guild?.Name ?? Strings.Creator)} | {DateTime.Now}",
                IconUrl = ctx.Guild?.IconUrl
            };
            await ctx.RespondAsync(embed: eb);
        }

        #region Private Methods

        private async Task EnableDisableUserSubscriptions(CommandContext ctx, DiscordUser user, ulong guildId)
        {
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, user.Id);
            if (subscription == null)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondEmbed(Translator.Instance.Translate("MSG_USER_NOT_SUBSCRIBED").FormatText(user.Username), DiscordColor.Red);
                return;
            }

            var cmd = ctx.Message.Content.TrimStart('.', ' ');
            subscription.Enabled = cmd.ToLower().Contains("enable");
            subscription.Save();

            await ctx.TriggerTypingAsync();
            await ctx.RespondEmbed(Translator.Instance.Translate("NOTIFY_ENABLE_DISABLE").FormatText(user.Username, cmd));
        }

        private async Task SendUserSubscriptionSettings(DiscordClient client, DiscordUser receiver, DiscordUser user, ulong guildId)
        {
            var messages = await BuildUserSubscriptionSettings(client, user, guildId);
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                message = message.Length > 2000 ? message.Substring(0, Math.Min(message.Length, 1500)) : message;
                var eb = new DiscordEmbedBuilder
                {
                    Title = Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_TITLE").FormatText(user.Username, i + 1, messages.Count),
                    Description = message,
                    Color = DiscordColor.CornflowerBlue,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{Strings.Creator} | {DateTime.Now}"
                    }
                };
                await client.SendDirectMessage(receiver, eb.Build());
            }
        }

        private async Task<List<string>> BuildUserSubscriptionSettings(DiscordClient client, DiscordUser user, ulong guildId)
        {
            var member = await client.GetMemberById(guildId, user.Id);
            if (member == null)
            {
                var error = $"Failed to get discord member from id {user.Id}.";
                _logger.Error(error);
                return new List<string> { error };
            }

            if (!_dep.WhConfig.Servers.ContainsKey(guildId))
                return null;

            var server = _dep.WhConfig.Servers[guildId];
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, user.Id);
            var isSubbed = subscription?.Pokemon.Count > 0 || subscription?.PvP.Count > 0 || subscription?.Raids.Count > 0 || subscription?.Quests.Count > 0 || subscription?.Invasions.Count > 0 || subscription?.Gyms.Count > 0 || subscription?.Lures.Count > 0;
            var hasPokemon = isSubbed && subscription?.Pokemon?.Count > 0;
            var hasPvP = isSubbed && subscription?.PvP?.Count > 0;
            var hasRaids = isSubbed && subscription?.Raids?.Count > 0;
            var hasGyms = isSubbed && subscription?.Gyms?.Count > 0;
            var hasQuests = isSubbed && subscription?.Quests?.Count > 0;
            var hasInvasions = isSubbed && subscription?.Invasions?.Count > 0;
            var hasLures = isSubbed && subscription?.Lures?.Count > 0;
            var messages = new List<string>();
            var isSupporter = await client.IsSupporterOrHigher(user.Id, guildId, _dep.WhConfig);

            var areas = server.Geofences.Select(x => x.Name).ToList();
            var feeds = member?.Roles?.Select(x => x.Name).Where(x => areas.Contains(x))?.ToList();
            if (feeds == null)
                return messages;
            feeds.Sort();

            var locationLink = $"[{subscription.Latitude},{subscription.Longitude}]({string.Format(Strings.GoogleMaps, subscription.Latitude, subscription.Longitude)})";
            var sb = new StringBuilder();
            sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_ENABLED").FormatText(subscription.Enabled ? "Yes" : "No"));
            sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_ICON_STYLE").FormatText(subscription.IconStyle));
            sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_DISTANCE").FormatText(subscription.DistanceM == 0 ?
                Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_DISTANCE_NOT_SET") :
                Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_DISTANCE_KM").FormatText(subscription.DistanceM.ToString("N0"), locationLink)));
            if (!string.IsNullOrEmpty(subscription.PhoneNumber))
            {
                sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_PHONE_NUMBER").FormatText(subscription.PhoneNumber));
            }
            sb.AppendLine(Environment.NewLine);

            if (hasPokemon)
            {
                var pokemon = subscription.Pokemon;
                pokemon.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                var exceedsLimits = pokemon.Count > Strings.MaxPokemonDisplayed;
                var defaultIV = 0;
                var defaultCount = 0;
                var results = pokemon.GroupBy(p => p.MinimumIV, (key, g) => new { IV = key, Pokes = g.ToList() });
                foreach (var result in results)
                {
                    if (result.Pokes.Count > defaultIV)
                    {
                        defaultIV = result.IV;
                        defaultCount = result.Pokes.Count;
                    }
                }

                sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_POKEMON").FormatText(pokemon.Count, server.Subscriptions.MaxPokemonSubscriptions == 0 ? "∞" : server.Subscriptions.MaxPokemonSubscriptions.ToString("N0")));
                sb.Append("```");

                if (exceedsLimits)
                {
                    sb.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_POKEMON_DEFAULT_UNLISTED").FormatText(defaultIV, defaultCount.ToString("N0")));
                }

                var cityRoles = server.Geofences.Select(x => x.Name);
                foreach (var poke in subscription.Pokemon)
                {
                    if (poke.MinimumIV == defaultIV && poke.IVList.Count == 0 && exceedsLimits)
                        continue;

                    if (!MasterFile.Instance.Pokedex.ContainsKey(poke.PokemonId))
                        continue;

                    var pkmn = MasterFile.Instance.Pokedex[poke.PokemonId];
                    var form = string.IsNullOrEmpty(poke.Form) ? string.Empty : $" ({poke.Form})";
                    var msg = $"{poke.PokemonId}: {pkmn.Name}{form} {(poke.MinimumIV + "%+ " + (poke.HasStats ? string.Join(", ", poke.IVList) : string.Empty))}{(poke.MinimumLevel > 0 ? $", L{poke.MinimumLevel}+" : null)}{(poke.Gender == "*" ? null : $", Gender: {poke.Gender}")}";
                    var isAllCities = cityRoles.ScrambledEquals(poke.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                    sb.AppendLine(Translator.Instance.Translate("NOTIFY_FROM").FormatText(msg, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", poke.Areas)));
                }

                sb.Append("```");
                sb.AppendLine();
                sb.AppendLine();
                messages.Add(sb.ToString());
            }

            if (hasPvP)
            {
                var pvpBuilder = new StringBuilder();
                pvpBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_PVP").FormatText(subscription.PvP.Count.ToString("N0"), server.Subscriptions.MaxPvPSubscriptions == 0 ? "∞" : server.Subscriptions.MaxPvPSubscriptions.ToString("N0")));
                pvpBuilder.Append("```");
                pvpBuilder.Append(string.Join(Environment.NewLine, GetPvPSubscriptionNames(guildId, user.Id)));
                pvpBuilder.Append("```");
                pvpBuilder.AppendLine();
                pvpBuilder.AppendLine();
                messages.Add(pvpBuilder.ToString());
            }

            if (hasRaids)
            {
                var raidsBuilder = new StringBuilder();
                raidsBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_RAIDS").FormatText(subscription.Raids.Count.ToString("N0"), server.Subscriptions.MaxRaidSubscriptions == 0 ? "∞" : server.Subscriptions.MaxRaidSubscriptions.ToString("N0")));
                raidsBuilder.Append("```");
                raidsBuilder.Append(string.Join(Environment.NewLine, GetRaidSubscriptionNames(guildId, user.Id)));
                raidsBuilder.Append("```");
                raidsBuilder.AppendLine();
                raidsBuilder.AppendLine();
                messages.Add(raidsBuilder.ToString());
            }

            if (hasGyms)
            {
                var gymsBuilder = new StringBuilder();
                gymsBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_GYMS").FormatText(subscription.Gyms.Count.ToString("N0"), server.Subscriptions.MaxGymSubscriptions == 0 ? "" : server.Subscriptions.MaxGymSubscriptions.ToString("N0")));
                gymsBuilder.Append("```");
                gymsBuilder.Append(string.Join(Environment.NewLine, GetGymSubscriptionNames(guildId, user.Id)));
                gymsBuilder.Append("```");
                gymsBuilder.AppendLine();
                gymsBuilder.AppendLine();
                messages.Add(gymsBuilder.ToString());
            }

            if (hasQuests)
            {
                var questsBuilder = new StringBuilder();
                questsBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_QUESTS").FormatText(subscription.Quests.Count.ToString("N0"), server.Subscriptions.MaxQuestSubscriptions == 0 ? "∞" : server.Subscriptions.MaxQuestSubscriptions.ToString("N0")));
                questsBuilder.Append("```");
                questsBuilder.Append(string.Join(Environment.NewLine, GetQuestSubscriptionNames(guildId, user.Id)));
                questsBuilder.Append("```");
                questsBuilder.AppendLine();
                questsBuilder.AppendLine();
                messages.Add(questsBuilder.ToString());
            }

            if (hasInvasions)
            {
                var invasionsBuilder = new StringBuilder();
                invasionsBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_INVASIONS").FormatText(subscription.Invasions.Count.ToString("N0"), server.Subscriptions.MaxInvasionSubscriptions == 0 ? "∞" : server.Subscriptions.MaxInvasionSubscriptions.ToString("N0")));
                invasionsBuilder.Append("```");
                invasionsBuilder.Append(string.Join(Environment.NewLine, GetInvasionSubscriptionNames(guildId, user.Id)));
                invasionsBuilder.Append("```");
                invasionsBuilder.AppendLine();
                invasionsBuilder.AppendLine();
                messages.Add(invasionsBuilder.ToString());
            }
          
            if (hasLures)
            {
                var luresBuilder = new StringBuilder();
                luresBuilder.AppendLine(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_LURES").FormatText(subscription.Lures.Count.ToString("N0"), server.Subscriptions.MaxLureSubscriptions == 0 ? "∞" : server.Subscriptions.MaxLureSubscriptions.ToString("N0")));
                luresBuilder.Append("```");
                luresBuilder.Append(string.Join(Environment.NewLine, GetLureSubscriptionNames(guildId, user.Id)));
                luresBuilder.Append("```");
                luresBuilder.AppendLine();
                luresBuilder.AppendLine();
                messages.Add(luresBuilder.ToString());
            }

            return messages;
        }

        private List<string> GetPvPSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedPvP = subscription.PvP;
            subscribedPvP.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

            var defaultRank = 0;
            var defaultCount = 0;
            var results = subscribedPvP.GroupBy(p => p.MinimumRank, (key, g) => new { Rank = key, Pokes = g.ToList() });
            foreach (var result in results)
            {
                if (result.Pokes.Count > defaultRank)
                {
                    defaultRank = result.Rank;
                    defaultCount = result.Pokes.Count;
                }
            }

            var exceedsLimits = subscribedPvP.Count > Strings.MaxPokemonDisplayed;
            if (exceedsLimits)
            {
                list.Add(Translator.Instance.Translate("NOTIFY_SETTINGS_EMBED_PVP_DEFAULT_UNLISTED").FormatText(defaultRank, defaultCount.ToString("N0")));
            }

            //var cityRoles = server.CityRoles;
            foreach (var pvp in subscription.PvP)
            {
                if (pvp.MinimumRank == defaultRank && exceedsLimits)
                    continue;

                if (!MasterFile.Instance.Pokedex.ContainsKey(pvp.PokemonId))
                    continue;

                var pokemon = MasterFile.Instance.Pokedex[pvp.PokemonId];
                if (pokemon == null)
                    continue;

                list.Add($"{pvp.PokemonId}: {pokemon.Name} {(string.IsNullOrEmpty(pvp.Form) ? string.Empty : $" {pvp.Form} ")}({pvp.League} Rank: 1-{pvp.MinimumRank} Percent: {pvp.MinimumPercent}%+)");
                //var isAllCities = cityRoles.ScrambledEquals(poke.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                //sb.AppendLine(Translator.Instance.Translate("NOTIFY_FROM").FormatText(msg, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", poke.Areas)));
            }

            return list;
        }

        private List<string> GetRaidSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedRaids = subscription.Raids;
            subscribedRaids.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));
            var cityRoles = _dep.WhConfig.Servers[guildId].Geofences.Select(x => x.Name.ToLower());

            foreach (var raid in subscribedRaids)
            {
                if (!MasterFile.Instance.Pokedex.ContainsKey(raid.PokemonId))
                    continue;

                var pokemon = MasterFile.Instance.Pokedex[raid.PokemonId];
                if (pokemon == null)
                    continue;

                var isAllCities = cityRoles.ScrambledEquals(raid.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                list.Add(Translator.Instance.Translate("NOTIFY_FROM").FormatText(pokemon.Name, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", raid.Areas)));
            }

            return list;
        }

        private List<string> GetGymSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedGyms = subscription.Gyms;
            subscribedGyms.Sort((x, y) => x.Name.CompareTo(y.Name));
            foreach (var gym in subscribedGyms)
            {
                list.Add(gym.Name);
            }

            return list;
        }

        private List<string> GetQuestSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedQuests = subscription.Quests;
            subscribedQuests.Sort((x, y) => string.Compare(x.RewardKeyword.ToLower(), y.RewardKeyword.ToLower(), true));
            var cityRoles = _dep.WhConfig.Servers[guildId].Geofences.Select(x => x.Name.ToLower());

            foreach (var quest in subscribedQuests)
            {
                var isAllCities = cityRoles.ScrambledEquals(quest.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                list.Add(Translator.Instance.Translate("NOTIFY_FROM").FormatText(quest.RewardKeyword, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", quest.Areas)));
            }

            return list;
        }

        private List<string> GetInvasionSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedInvasions = subscription.Invasions;
            subscribedInvasions.Sort((x, y) => string.Compare(MasterFile.GetPokemon(x.RewardPokemonId, 0).Name, MasterFile.GetPokemon(y.RewardPokemonId, 0).Name, true));
            var cityRoles = _dep.WhConfig.Servers[guildId].Geofences.Select(x => x.Name.ToLower());

            foreach (var invasion in subscribedInvasions)
            {
                var isAllCities = cityRoles.ScrambledEquals(invasion.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                list.Add(Translator.Instance.Translate("NOTIFY_FROM").FormatText(MasterFile.GetPokemon(invasion.RewardPokemonId, 0).Name, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", invasion.Areas)));
            }

            return list;
        }

        private List<string> GetLureSubscriptionNames(ulong guildId, ulong userId)
        {
            var list = new List<string>();
            var subscription = _dep.SubscriptionProcessor.Manager.GetUserSubscriptions(guildId, userId);
            var subscribedLures = subscription.Lures;
            subscribedLures.Sort((x, y) => x.LureType.CompareTo(y.LureType));
            var cityRoles = _dep.WhConfig.Servers[guildId].Geofences.Select(x => x.Name.ToLower());

            foreach (var lure in subscribedLures)
            {
                var isAllCities = cityRoles.ScrambledEquals(lure.Areas, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true));
                list.Add(Translator.Instance.Translate("NOTIFY_FROM").FormatText(lure.LureType, isAllCities ? Translator.Instance.Translate("ALL_AREAS") : string.Join(", ", lure.Areas)));
            }

            return list;
        }

        private DiscordEmbedBuilder BuildExpirationMessage(ulong guildId, DiscordUser user)
        {
            var customerData = _dep.Stripe.GetCustomerData(guildId, user.Id);
            if (!customerData.ExpireDate.HasValue)
            {
                return null;
            }
            var expires = customerData.ExpireDate.Value;
            var remaining = DateTime.Now.GetTimeRemaining(expires);
            return new DiscordEmbedBuilder
            {
                Title = $"{user.Username}#{user.Discriminator} ({user.Id}) Subscription Expires",
                Description = $"Your subscription will expire in {remaining.ToReadableStringNoSeconds()} on {customerData.ExpireDate}\r\n\r\nTo cancel your subscription type `cancel` in the #become-a-donor channel."
            };
        }

        private static ulong ConvertMentionToUserId(string mention)
        {
            //<@201909896357216256>
            //mention = Utils.GetBetween(mention, "<", ">");
            mention = mention.Replace("<", null);
            mention = mention.Replace(">", null);
            mention = mention.Replace("@", null);
            mention = mention.Replace("!", null);

            return ulong.TryParse(mention, out ulong result) ? result : 0;
        }

        private async Task<bool> CanExecute(CommandContext ctx)
        {
            if (!await ctx.IsDirectMessageSupported(_dep.WhConfig))
                return false;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.FirstOrDefault(x => _dep.WhConfig.Servers.ContainsKey(x.Key)).Key;
            if (guildId == 0 || !_dep.WhConfig.Servers.ContainsKey(guildId))
                return false;

            if (!_dep.WhConfig.Servers[guildId].Subscriptions.Enabled)
            {
                await ctx.RespondEmbed(string.Format(Translator.Instance.Translate("MSG_SUBSCRIPTIONS_NOT_ENABLED"), ctx.User.Username), DiscordColor.Red);
                return false;
            }

            var isSupporter = await ctx.Client.IsSupporterOrHigher(ctx.User.Id, guildId, _dep.WhConfig);
            if (!isSupporter)
            {
                await ctx.DonateUnlockFeaturesMessage();
                return false;
            }

            return true;
        }

        private static PokestopLureType GetLureFromName(string lureName)
        {
            lureName = lureName.ToLower();
            if (lureName.Contains("501") || lureName.Contains("norm"))
                return PokestopLureType.Normal;
            else if (lureName.Contains("502") || lureName.Contains("glac"))
                return PokestopLureType.Glacial;
            else if (lureName.Contains("503") || lureName.Contains("mos"))
                return PokestopLureType.Mossy;
            else if (lureName.Contains("504") || lureName.Contains("mag"))
                return PokestopLureType.Magnetic;
            return PokestopLureType.None;
        }

        private static List<PokestopLureType> GetLures(string lureTypes)
        {
            var lureNames = lureTypes.RemoveSpaces();
            var list = new List<PokestopLureType>();
            lureNames.ForEach(x => list.Add(GetLureFromName(x)));
            return list;
        }

        #endregion
    }

    internal class SubscriptionAreas
    {
        public static List<string> GetAreas(DiscordServerConfig server, string city)
        {
            if (string.IsNullOrEmpty(city))
                return new List<string>();

            // Parse user defined cities
            var validCities = server.Geofences.Select(g => g.Name).ToList();
            var cities = /*string.IsNullOrEmpty(city) ||*/ string.Compare(city, Strings.All, true) == 0
                ? validCities
                : city.RemoveSpaces();
            var validAreas = validCities.Select(x => x.ToLower());
            // Validate areas
            return cities
                .Where(x => validAreas.Contains(x.ToLower()))
                .ToList();
        }

        public static bool ContainsCity(List<string> oldCities, List<string> newCities)
        {
            var oldAreas = oldCities.Select(x => x.ToLower());
            var newAreas = newCities.Select(x => x.ToLower());
            foreach (var newArea in newAreas)
            {
                if (oldAreas.Contains(newArea))
                    continue;

                return false;
            }
            return true;
        }
    }
}