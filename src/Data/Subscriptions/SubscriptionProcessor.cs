using WhMgr.Geofence;

namespace WhMgr.Data.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using ServiceStack.OrmLite;

    using WhMgr.Alarms.Filters;
    using WhMgr.Configuration;
    using WhMgr.Data.Subscriptions.Models;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Localization;
    using WhMgr.Net.Models;
    using WhMgr.Net.Webhooks;
    using Utils = WhMgr.Utilities.Utils;

    /// <summary>
    /// Discord user subscription processing class.
    /// </summary>
    public class SubscriptionProcessor
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("SUBSCRIPTION", Program.LogLevel);

        // Contains all Discord clients by Guild ID
        private readonly Dictionary<ulong, DiscordClient> _servers;
        // Holds the WhConfg instance
        private readonly WhConfigHolder _whConfig;
        // WebhookController class
        private readonly WebhookController _whm;
        // Subscription notification processing queue
        private readonly NotificationQueue _queue;

        #endregion

        #region Properties

        /// <summary>
        /// Get subscription manager class
        /// </summary>
        public SubscriptionManager Manager { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiate a new <see cref="SubscriptionProcessor"/> class.
        /// </summary>
        /// <param name="servers">Discord servers dictionary</param>
        /// <param name="config">Configuration file</param>
        /// <param name="whm">Webhook controller class</param>
        public SubscriptionProcessor(Dictionary<ulong, DiscordClient> servers, WhConfigHolder config, WebhookController whm)
        {
            _logger.Trace($"SubscriptionProcessor::SubscriptionProcessor");

            _servers = servers;
            _whConfig = config;
            _whm = whm;
            _queue = new NotificationQueue();

            Manager = new SubscriptionManager(_whConfig);

            ProcessQueue();
        }

        #endregion

        #region Public Methods

        public async Task ProcessPokemonSubscription(PokemonData pkmn)
        {
            if (!MasterFile.Instance.Pokedex.ContainsKey(pkmn.Id))
                return;

            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();
            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, pkmn.Latitude, pkmn.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByPokemonId(pkmn.Id);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            SubscriptionObject user;
            DiscordMember member = null;
            var pokemon = MasterFile.GetPokemon(pkmn.Id, pkmn.FormId);
            var matchesIV = false;
            var matchesLvl = false;
            var matchesGender = false;
            var matchesIVList = false;
            for (var i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    try
                    {
                        member = await client.GetMemberById(user.GuildId, user.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"FAILED TO GET MEMBER BY ID {user.UserId}");
                        _logger.Error(ex);
                        continue;
                    }

                    if (member?.Roles == null)
                        continue;

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Debug($"User {member?.Username} ({user.UserId}) is not a supporter, skipping pokemon {pokemon.Name}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var form = Translator.Instance.GetFormName(pkmn.FormId);
                    var pokemonSubscriptions = user.Pokemon.Where(x =>
                        x.PokemonId.Contains(pkmn.Id) && (x.Forms?.Contains(form) ?? true)
                    );
                    foreach (var pkmnSub in pokemonSubscriptions)
                    {
                        matchesIV = Filters.MatchesIV(pkmn.IV, pkmnSub.MinimumIV);
                        //var matchesCP = _whm.Filters.MatchesCpFilter(pkmn.CP, subscribedPokemon.MinimumCP);
                        matchesLvl = Filters.MatchesLvl(pkmn.Level, (uint)pkmnSub.MinimumLevel, (uint)pkmnSub.MaximumLevel);
                        matchesGender = Filters.MatchesGender(pkmn.Gender, pkmnSub.Gender);
                        matchesIVList = pkmnSub.IVList?.Select(x => x.Replace("\r", null)).Contains($"{pkmn.Attack}/{pkmn.Defense}/{pkmn.Stamina}") ?? false;

                        if (!(
                            (!pkmnSub.HasStats && matchesIV && matchesLvl && matchesGender) ||
                            (pkmnSub.HasStats && matchesIVList)
                            ))
                            continue;

                        if (!(float.TryParse(pkmn.Height, out var height) && float.TryParse(pkmn.Weight, out var weight) && Filters.MatchesSize(pkmn.Id.GetSize(height, weight), pkmnSub.Size)))
                        {
                            // Pokemon doesn't match size
                            continue;
                        }

                        var geofence = GetGeofence(user.GuildId);
                        if (geofence == null)
                        {
                            //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                            continue;
                        }

                        var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                        var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, pkmnSub.Location, true) == 0);
                        var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pkmn.Latitude, pkmn.Longitude));
                        var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pkmn.Latitude, pkmn.Longitude));
                        var geofenceMatches = pkmnSub.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                        // If set distance does not match and no geofences match, then skip Pokemon...
                        if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                            continue;

                        var embed = pkmn.GeneratePokemonMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name);
                        //var end = DateTime.Now.Subtract(start);
                        //_logger.Debug($"Took {end} to process Pokemon subscription for user {user.UserId}");
                        embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokemon.Name, geofence.Name, pkmn)));

                        Statistics.Instance.SubscriptionPokemonSent++;
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            member = null;
            user = null;
            pokemon = null;

            await Task.CompletedTask;
        }

        public async Task ProcessPvPSubscription(PokemonData pkmn)
        {
            if (!MasterFile.Instance.Pokedex.ContainsKey(pkmn.Id))
                return;

            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, pkmn.Latitude, pkmn.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByPvPPokemonId(pkmn.Id);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            SubscriptionObject user;
            DiscordMember member = null;
            var pokemon = MasterFile.GetPokemon(pkmn.Id, pkmn.FormId);
            var matchesGreat = false;
            var matchesUltra = false;
            for (var i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    try
                    {
                        member = await client.GetMemberById(user.GuildId, user.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"FAILED TO GET MEMBER BY ID {user.UserId}");
                        _logger.Error(ex);
                        continue;
                    }

                    if (member?.Roles == null)
                        continue;

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Debug($"User {member?.Username} ({user.UserId}) is not a supporter, skipping pvp pokemon {pokemon.Name}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var form = Translator.Instance.GetFormName(pkmn.FormId);
                    var pokemonSubscriptions = user.PvP.Where(x =>
                        x.PokemonId.Contains(pkmn.Id) && (x.Forms?.Contains(form) ?? true)
                    );
                    foreach (var pkmnSub in pokemonSubscriptions)
                    {
                        matchesGreat = pkmn.GreatLeague != null && (pkmn.GreatLeague?.Exists(x => pkmnSub.League == PvPLeague.Great &&
                                                                         (x.CP ?? 0) >= Strings.MinimumGreatLeagueCP && (x.CP ?? 0) <= Strings.MaximumGreatLeagueCP &&
                                                                         (x.Rank ?? 4096) <= pkmnSub.MinimumRank &&
                                                                         (x.Percentage ?? 0) * 100 >= pkmnSub.MinimumPercent) ?? false);
                        matchesUltra = pkmn.UltraLeague != null && (pkmn.UltraLeague?.Exists(x => pkmnSub.League == PvPLeague.Ultra &&
                                                                         (x.CP ?? 0) >= Strings.MinimumUltraLeagueCP && (x.CP ?? 0) <= Strings.MaximumUltraLeagueCP &&
                                                                         (x.Rank ?? 4096) <= pkmnSub.MinimumRank &&
                                                                         (x.Percentage ?? 0) * 100 >= pkmnSub.MinimumPercent) ?? false);

                        // Check if Pokemon IV stats match any relevant great or ultra league ranks, if not skip.
                        if (!matchesGreat && !matchesUltra)
                            continue;

                        var geofence = GetGeofence(user.GuildId);
                        if (geofence == null)
                        {
                            //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                            continue;
                        }

                        var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                        var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, pkmnSub.Location, true) == 0);
                        var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pkmn.Latitude, pkmn.Longitude));
                        var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pkmn.Latitude, pkmn.Longitude));
                        var geofenceMatches = pkmnSub.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                        // If set distance does not match and no geofences match, then skip Pokemon...
                        if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                            continue;

                        var embed = pkmn.GeneratePokemonMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name);
                        //var end = DateTime.Now.Subtract(start);
                        //_logger.Debug($"Took {end} to process PvP subscription for user {user.UserId}");
                        embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokemon.Name, geofence.Name)));

                        Statistics.Instance.SubscriptionPokemonSent++;
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            member = null;
            user = null;
            pokemon = null;

            await Task.CompletedTask;
        }

        public async Task ProcessRaidSubscription(RaidData raid)
        {
            if (!MasterFile.Instance.Pokedex.ContainsKey(raid.PokemonId))
                return;

            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, raid.Latitude, raid.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByRaidBossId(raid.PokemonId);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            SubscriptionObject user;
            var pokemon = MasterFile.GetPokemon(raid.PokemonId, raid.Form);
            for (int i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    var member = await client.GetMemberById(user.GuildId, user.UserId);
                    if (member == null)
                    {
                        _logger.Warn($"Failed to find member with id {user.UserId}.");
                        continue;
                    }

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Info($"User {user.UserId} is not a supporter, skipping raid boss {pokemon.Name}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var form = Translator.Instance.GetFormName(raid.Form);
                    var subPkmn = user.Raids.FirstOrDefault(x =>
                        x.PokemonId == raid.PokemonId &&
                        (string.IsNullOrEmpty(x.Form) || (!string.IsNullOrEmpty(x.Form) && string.Compare(x.Form, form, true) == 0))
                    );
                    // Not subscribed to Pokemon
                    if (subPkmn == null)
                    {
                        //_logger.Debug($"Skipping notification for user {member.DisplayName} ({member.Id}) for raid boss {pokemon.Name}, raid is in city '{loc.Name}'.");
                        continue;
                    }

                    var geofence = GetGeofence(user.GuildId);
                    if (geofence == null)
                    {
                        //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                        continue;
                    }

                    var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                    var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, subPkmn.Location, true) == 0);
                    var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(raid.Latitude, raid.Longitude));
                    var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(raid.Latitude, raid.Longitude));
                    var geofenceMatches = subPkmn.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                    // If set distance does not match and no geofences match, then skip Raid Pokemon...
                    if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                        continue;

                    var embed = raid.GenerateRaidMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name);
                    //var end = DateTime.Now;
                    //_logger.Debug($"Took {end} to process raid subscription for user {user.UserId}");
                    embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokemon.Name, geofence.Name)));

                    Statistics.Instance.SubscriptionRaidsSent++;
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            user = null;

            await Task.CompletedTask;
        }

        public async Task ProcessGymSubscription(RaidData raid)
        {
            //if (!MasterFile.Instance.Pokedex.ContainsKey(raid.PokemonId))
            //    return;

            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, raid.Latitude, raid.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByGymName(raid.GymName);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            SubscriptionObject user;
            var pokemon = MasterFile.GetPokemon(raid.PokemonId, raid.Form);
            for (int i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    var member = await client.GetMemberById(user.GuildId, user.UserId);
                    if (member == null)
                    {
                        _logger.Warn($"Failed to find member with id {user.UserId}.");
                        continue;
                    }

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Info($"User {user.UserId} is not a supporter, skipping raid boss {pokemon.Name} for gym {raid.GymName}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var geofence = GetGeofence(user.GuildId);
                    if (geofence == null)
                    {
                        //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                        continue;
                    }

                    var gymSub = user.Gyms.FirstOrDefault(x => string.Compare(x.Name, raid.GymName, true) == 0);
                    if (gymSub == null)
                        continue;

                    var checkLevel = gymSub.MinimumLevel > 0 && gymSub.MaximumLevel > 0;
                    var containsPokemon = gymSub.PokemonIDs?.Contains((uint)raid.PokemonId) ?? false;
                    if (!checkLevel && !containsPokemon)
                        continue;

                    /*
                    // TODO: Gym distance location
                    var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                    var pokemonLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, subPkmn.Location, true) == 0);
                    var globalDistanceMatches = globalLocation.DistanceM > 0 && globalLocation.DistanceM > new Coordinates(globalLocation.Latitude, globalLocation.Longitude).DistanceTo(new Coordinates(raid.Latitude, raid.Longitude));
                    var invasionDistanceMatches = pokemonLocation.DistanceM > 0 && pokemonLocation.DistanceM > new Coordinates(pokemonLocation.Latitude, pokemonLocation.Longitude).DistanceTo(new Coordinates(raid.Latitude, raid.Longitude));
                    var geofenceMatches = subPkmn.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                    // If set distance does not match and no geofences match, then skip Pokemon...
                    if (!globalDistanceMatches && !invasionDistanceMatches && !geofenceMatches)
                        continue;
                    */

                    var embed = raid.GenerateRaidMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name);
                    //var end = DateTime.Now;
                    //_logger.Debug($"Took {end} to process gym raid subscription for user {user.UserId}");
                    embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokemon.Name, geofence.Name)));

                    Statistics.Instance.SubscriptionRaidsSent++;
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            user = null;

            await Task.CompletedTask;
        }

        public async Task ProcessQuestSubscription(QuestData quest)
        {
            var reward = quest.Rewards.FirstOrDefault().Info;
            var rewardKeyword = quest.GetReward();
            var questName = quest.GetQuestMessage();

            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, quest.Latitude, quest.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByQuest(quest.PokestopName, rewardKeyword);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            bool isSupporter;
            SubscriptionObject user;
            for (int i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    var member = await client.GetMemberById(user.GuildId, user.UserId);
                    if (member == null)
                    {
                        _logger.Warn($"Failed to find member with id {user.UserId}.");
                        continue;
                    }

                    isSupporter = member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds);
                    if (!isSupporter)
                    {
                        _logger.Info($"User {user.UserId} is not a supporter, skipping quest {questName}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var subQuest = user.Quests.FirstOrDefault(x => rewardKeyword.ToLower().Contains(x.RewardKeyword.ToLower()));
                    // Not subscribed to quest
                    if (subQuest == null)
                    {
                        //_logger.Debug($"Skipping notification for user {member.DisplayName} ({member.Id}) for quest {questName} because the quest is in city '{loc.Name}'.");
                        continue;
                    }

                    var geofence = GetGeofence(user.GuildId);
                    if (geofence == null)
                    {
                        //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                        continue;
                    }

                    var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                    var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, subQuest.Location, true) == 0);
                    var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(quest.Latitude, quest.Longitude));
                    var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(quest.Latitude, quest.Longitude));
                    var geofenceMatches = subQuest.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                    // If set distance does not match and no geofences match, then skip Pokemon...
                    if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                        continue;

                    var embed = quest.GenerateQuestMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name);
                    //var end = DateTime.Now.Subtract(start);
                    //_logger.Debug($"Took {end} to process quest subscription for user {user.UserId}");
                    embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, questName, geofence.Name)));

                    Statistics.Instance.SubscriptionQuestsSent++;
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            user = null;

            await Task.CompletedTask;
        }

        public async Task ProcessInvasionSubscription(PokestopData pokestop)
        {
            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, pokestop.Latitude, pokestop.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var invasion = MasterFile.Instance.GruntTypes.ContainsKey(pokestop.GruntType) ? MasterFile.Instance.GruntTypes[pokestop.GruntType] : null;
            var encounters = invasion?.GetEncounterRewards();
            if (encounters == null)
                return;

            var subscriptions = Manager.GetUserSubscriptionsByInvasion(pokestop?.Name, pokestop?.GruntType ?? InvasionCharacter.CharacterUnset, encounters);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            if (!MasterFile.Instance.GruntTypes.ContainsKey(pokestop.GruntType))
            {
                //_logger.Error($"Failed to parse grunt type {pokestop.GruntType}, not in `grunttype.json` list.");
                return;
            }
         
            SubscriptionObject user;
            for (int i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    var member = await client.GetMemberById(user.GuildId, user.UserId);
                    if (member == null)
                    {
                        _logger.Warn($"Failed to find member with id {user.UserId}.");
                        continue;
                    }

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Info($"User {user.UserId} is not a supporter, skipping Team Rocket invasion {pokestop.Name}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var subInvasion = user.Invasions.FirstOrDefault(x => x.RewardPokemonId.Intersects(encounters));
                    // Not subscribed to invasion
                    if (subInvasion == null)
                    {
                        //_logger.Debug($"Skipping notification for user {member.DisplayName} ({member.Id}) for raid boss {pokemon.Name}, raid is in city '{loc.Name}'.");
                        continue;
                    }

                    var geofence = GetGeofence(user.GuildId);
                    if (geofence == null)
                    {
                        //_logger.Warn($"Failed to lookup city from coordinates {pkmn.Latitude},{pkmn.Longitude} {db.Pokemon[pkmn.Id].Name} {pkmn.IV}, skipping...");
                        continue;
                    }

                    var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                    var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, subInvasion.Location, true) == 0);
                    var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pokestop.Latitude, pokestop.Longitude));
                    var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pokestop.Latitude, pokestop.Longitude));
                    var geofenceMatches = subInvasion.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                    // If set distance does not match and no geofences match, then skip Pokemon...
                    if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                        continue;

                    var embed = pokestop.GeneratePokestopMessage(user.GuildId, client, _whConfig.Instance, null, geofence?.Name, false, true);
                    //var end = DateTime.Now.Subtract(start);
                    //_logger.Debug($"Took {end} to process invasion subscription for user {user.UserId}");
                    embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokestop.Name, geofence.Name)));

                    Statistics.Instance.SubscriptionInvasionsSent++;
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            user = null;

            await Task.CompletedTask;
        }

        public async Task ProcessLureSubscription(PokestopData pokestop)
        {
            // Cache the result per-guild so that geospatial stuff isn't queried for every single subscription below
            var locationCache = new Dictionary<ulong, GeofenceItem>();

            GeofenceItem GetGeofence(ulong guildId)
            {
                if (!locationCache.TryGetValue(guildId, out var geofence))
                {
                    geofence = _whm.GetGeofence(guildId, pokestop.Latitude, pokestop.Longitude);
                    locationCache.Add(guildId, geofence);
                }

                return geofence;
            }

            var subscriptions = Manager.GetUserSubscriptionsByLureType(pokestop.LureType);
            if (subscriptions == null)
            {
                _logger.Warn($"Failed to get subscriptions from database table.");
                return;
            }

            SubscriptionObject user;
            for (int i = 0; i < subscriptions.Count; i++)
            {
                //var start = DateTime.Now;
                try
                {
                    user = subscriptions[i];

                    if (!_whConfig.Instance.Servers.ContainsKey(user.GuildId))
                        continue;

                    if (!_whConfig.Instance.Servers[user.GuildId].Subscriptions.Enabled)
                        continue;

                    if (!_servers.ContainsKey(user.GuildId))
                        continue;

                    var client = _servers[user.GuildId];

                    var member = await client.GetMemberById(user.GuildId, user.UserId);
                    if (member == null)
                    {
                        _logger.Warn($"Failed to find member with id {user.UserId}.");
                        continue;
                    }

                    if (!member.HasSupporterRole(_whConfig.Instance.Servers[user.GuildId].DonorRoleIds))
                    {
                        _logger.Info($"User {user.UserId} is not a supporter, skipping Pokestop lure {pokestop.Name}...");
                        // Automatically disable users subscriptions if not supporter to prevent issues
                        //user.Enabled = false;
                        //user.Save(false);
                        continue;
                    }

                    var subLure = user.Lures.FirstOrDefault(x => x.LureType == pokestop.LureType);
                    // Not subscribed to lure
                    if (subLure == null)
                    {
                        //_logger.Debug($"Skipping notification for user {member.DisplayName} ({member.Id}) for Pokestop lure {pokemon.Name}, lure is in city '{loc.Name}'.");
                        continue;
                    }

                    var geofence = GetGeofence(user.GuildId);
                    if (geofence == null)
                    {
                        //_logger.Warn($"Failed to lookup city from coordinates {pokestop.Latitude},{pokestop.Longitude} {pokestop.PokestopId} {pokestop.Name}, skipping...");
                        continue;
                    }

                    var globalLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, user.Location, true) == 0);
                    var subscriptionLocation = user.Locations?.FirstOrDefault(x => string.Compare(x.Name, subLure.Location, true) == 0);
                    var globalDistanceMatches = globalLocation?.DistanceM > 0 && globalLocation?.DistanceM > new Coordinates(globalLocation?.Latitude ?? 0, globalLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pokestop.Latitude, pokestop.Longitude));
                    var subscriptionDistanceMatches = subscriptionLocation?.DistanceM > 0 && subscriptionLocation?.DistanceM > new Coordinates(subscriptionLocation?.Latitude ?? 0, subscriptionLocation?.Longitude ?? 0).DistanceTo(new Coordinates(pokestop.Latitude, pokestop.Longitude));
                    var geofenceMatches = subLure.Areas.Select(x => x.ToLower()).Contains(geofence.Name.ToLower());

                    // If set distance does not match and no geofences match, then skip Pokemon...
                    if (!globalDistanceMatches && !subscriptionDistanceMatches && !geofenceMatches)
                        continue;

                    var embed = pokestop.GeneratePokestopMessage(user.GuildId, client, _whConfig.Instance, null, geofence.Name, true, false);
                    //var end = DateTime.Now.Subtract(start);
                    //_logger.Debug($"Took {end} to process lure subscription for user {user.UserId}");
                    embed.Embeds.ForEach(x => _queue.Enqueue(new NotificationItem(user, member, x, pokestop.Name, geofence.Name)));

                    Statistics.Instance.SubscriptionLuresSent++;
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            subscriptions.Clear();
            subscriptions = null;
            user = null;

            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void ProcessQueue()
        {
            _logger.Trace($"SubscriptionProcessor::ProcessQueue");

            new Thread(async () =>
            {
                while (true)
                {
                    if (_queue.Count == 0)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    if (_queue.Count > Strings.MaxQueueCountWarning)
                    {
                        _logger.Warn($"Subscription queue is {_queue.Count:N0} items long.");
                    }

                    var item = _queue.Dequeue();
                    if (item == null || item?.Subscription == null || item?.Member == null || item?.Embed == null)
                        continue;

                    // Check if user is receiving messages too fast.
                    var maxNotificationsPerMinute = _whConfig.Instance.MaxNotificationsPerMinute;
                    if (item.Subscription.Limiter.IsLimited(maxNotificationsPerMinute))
                    {
                        _logger.Warn($"{item.Member.Username} notifications rate limited, waiting {(60 - item.Subscription.Limiter.TimeLeft.TotalSeconds)} seconds...", item.Subscription.Limiter.TimeLeft.TotalSeconds.ToString("N0"));
                        // Send ratelimited notification to user if not already sent to adjust subscription settings to more reasonable settings.
                        if (!item.Subscription.RateLimitNotificationSent)
                        {
                            if (!_servers.ContainsKey(item.Subscription.GuildId))
                                continue;

                            var server = _servers[item.Subscription.GuildId].Guilds[item.Subscription.GuildId];
                            var emoji = DiscordEmoji.FromName(_servers.FirstOrDefault().Value, ":no_entry:");
                            var guildIconUrl = _servers.ContainsKey(item.Subscription.GuildId) ? _servers[item.Subscription.GuildId].Guilds[item.Subscription.GuildId]?.IconUrl : string.Empty;
                            // TODO: Localize
                            var rateLimitMessage = $"{emoji} Your notification subscriptions have exceeded {maxNotificationsPerMinute:N0}) per minute and are now being rate limited." +
                                                   $"Please adjust your subscriptions to receive a maximum of {maxNotificationsPerMinute:N0} notifications within a {NotificationLimiter.ThresholdTimeout} second time span.";
                            var eb = new DiscordEmbedBuilder
                            {
                                Title = "Rate Limited",
                                Description = rateLimitMessage,
                                Color = DiscordColor.Red,
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"{server?.Name} | {DateTime.Now}",
                                    IconUrl = server?.IconUrl
                                }
                            };

                            await _servers[item.Subscription.GuildId].SendDirectMessage(item.Member, string.Empty, eb.Build());
                            item.Subscription.RateLimitNotificationSent = true;
                            item.Subscription.Status = NotificationStatusType.None;
                            if (!item.Subscription.Update())
                            {
                                _logger.Error($"Failed to disable {item.Subscription.UserId}'s subscriptions");
                            }
                        }
                        continue;
                    }

                    // Ratelimit is up, allow for ratelimiting again
                    item.Subscription.RateLimitNotificationSent = false;

                    if (!_servers.ContainsKey(item.Subscription.GuildId))
                    {
                        _logger.Error($"User subscription for guild that's not configured. UserId={item.Subscription.UserId} GuildId={item.Subscription.GuildId}");
                        continue;
                    }

                    // Send text message notification to user if a phone number is set
                    if (_whConfig.Instance.Twilio.Enabled && !string.IsNullOrEmpty(item.Subscription.PhoneNumber))
                    {
                        // Check if user is in the allowed text message list or server owner
                        if (HasRole(item.Member, _whConfig.Instance.Twilio.RoleIds) ||
                            _whConfig.Instance.Twilio.UserIds.Contains(item.Member.Id) ||
                            _whConfig.Instance.Servers[item.Subscription.GuildId].OwnerId == item.Member.Id)
                        {
                            // Send text message (max 160 characters)
                            if (item.Pokemon != null && IsUltraRare(_whConfig.Instance.Twilio, item.Pokemon))
                            {
                                var result = Utils.SendSmsMessage(StripEmbed(item), _whConfig.Instance.Twilio, item.Subscription.PhoneNumber);
                                if (!result)
                                {
                                    _logger.Error($"Failed to send text message to phone number '{item.Subscription.PhoneNumber}' for user {item.Subscription.UserId}");
                                }
                            }
                        }
                    }

                    // Send direct message notification to user
                    var client = _servers[item.Subscription.GuildId];
                    await client.SendDirectMessage(item.Member, item.Embed);
                    _logger.Info($"[WEBHOOK] Notified user {item.Member.Username} of {item.Description}.");
                    Thread.Sleep(10);
                }
            })
            { IsBackground = true }.Start();
        }

        private static bool HasRole(DiscordMember member, List<ulong> roleIds)
        {
            var memberRoles = member.Roles.Select(x => x.Id);
            return roleIds.Any(x => memberRoles.Contains(x));
        }

        private static bool IsUltraRare(TwilioConfig twilo, PokemonData pkmn)
        {
            // If no Pokemon are set, do not send text messages
            if (twilo.PokemonIds.Count == 0)
                return false;

            // Check if Pokemon is in list of allowed IDs
            if (!twilo.PokemonIds.Contains(pkmn.Id))
                return false;

            // Send text message if Unown, Azelf, etc
            if (pkmn.Id.IsRarePokemon())
                return true;

            // Send text message if 100% Gible, Deino, and Axew
            if (Filters.MatchesIV(pkmn.IV, twilo.MinimumIV))
                return true;

            return false;
        }

        private static string StripEmbed(NotificationItem item)
        {
            const int MAX_TEXT_LENGTH = 120;
            var text = item.Embed.Description;
            text = text.Replace("**", null);
            text = text.Length > MAX_TEXT_LENGTH
                ? text.Substring(0, Math.Min(text.Length, MAX_TEXT_LENGTH))
                : text;
            // TODO: Construct text message instead of using embed description and url for google maps link
            return $"{item.City}\n{text}\n{item.Embed.Url}";
        }

        #endregion
    }
}
