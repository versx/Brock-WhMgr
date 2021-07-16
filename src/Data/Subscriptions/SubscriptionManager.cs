namespace WhMgr.Data.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;

    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using ServiceStack.OrmLite;

    using WhMgr.Configuration;
    using WhMgr.Data.Subscriptions.Models;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Net.Models;

    /// <summary>
    /// User subscription manager class
    /// </summary>
    public class SubscriptionManager
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("MANAGER", Program.LogLevel);

        private readonly WhConfigHolder _whConfig;
        private List<SubscriptionObject> _subscriptions;
        private readonly OrmLiteConnectionFactory _connFactory;
        private readonly Timer _reloadTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all current user subscriptions
        /// </summary>
        public IReadOnlyList<SubscriptionObject> Subscriptions => _subscriptions;

        #endregion

        #region Constructor

        public SubscriptionManager(WhConfigHolder whConfig)
        {
            _logger.Trace($"SubscriptionManager::SubscriptionManager");

            _whConfig = whConfig;

            if (_whConfig.Instance?.Database?.Main == null)
            {
                var err = "Main database is not configured in config.json file.";
                _logger.Error(err);
                throw new NullReferenceException(err);
            }

            if (_whConfig.Instance?.Database?.Scanner == null)
            {
                var err = "Scanner database is not configured in config.json file.";
                _logger.Error(err);
                throw new NullReferenceException(err);
            }

            if (_whConfig.Instance?.Database?.Nests == null)
            {
                _logger.Warn("Nest database is not configured in config.json file, nest alarms and commands will not work.");
            }
          
            _connFactory = new OrmLiteConnectionFactory(_whConfig.Instance.Database.Main.ToString(), MySqlDialect.Provider);

            // Reload subscriptions every minute x 60 seconds to account for UI changes
            _reloadTimer = new Timer(_whConfig.Instance.ReloadSubscriptionChangesMinutes * 60 * 1000);
            _reloadTimer.Elapsed += (sender, e) => ReloadSubscriptions();
            _reloadTimer.Start();

            ReloadSubscriptions();
        }

        #endregion

        #region User

        /// <summary>
        /// Get user subscription from guild id and user id
        /// </summary>
        /// <param name="guildId">Discord guild id to lookup</param>
        /// <param name="userId">Discord user id to lookup</param>
        /// <returns>Returns user subscription object</returns>
        public SubscriptionObject GetUserSubscriptions(ulong guildId, ulong userId)
        {
            if (!IsDbConnectionOpen())
            {
                throw new Exception("Not connected to database.");
            }

            try
            {
                var conn = GetConnection();
                var where = conn?
                    .From<SubscriptionObject>()
                    .Where(x => x.GuildId == guildId && x.UserId == userId);
                var query = conn?.LoadSelect(where);
                var sub = query?.FirstOrDefault();
                return sub ?? new SubscriptionObject { UserId = userId, GuildId = guildId };
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                _logger.Error(ex);
                return GetUserSubscriptions(guildId, userId);
            }
        }

        /// <summary>
        /// Get user subscriptions from subscribed Pokemon id
        /// </summary>
        /// <param name="pokeId">Pokemon ID to lookup</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByPokemonId(uint pokeId)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Pokemon) &&
                            x.Pokemon != null &&
                            x.Pokemon.Exists(y => y.PokemonId.Contains(pokeId))
                      )
                .ToList();
        }

        /// <summary>
        /// Get user subscriptions from subscribed PvP Pokemon id
        /// </summary>
        /// <param name="pokeId">Pokemon ID to lookup</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByPvPPokemonId(uint pokeId)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.PvP) &&
                            x.PvP != null &&
                            x.PvP.Exists(y => y.PokemonId.Contains(pokeId))
                      )
                .ToList();
        }

        /// <summary>
        /// Get user subscriptions from subscribed Raid Pokemon id
        /// </summary>
        /// <param name="pokeId">Pokemon ID to lookup</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByRaidBossId(uint pokeId)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Raids) &&
                            x.Raids != null &&
                            x.Raids.Exists(y => y.PokemonId == pokeId)
                      )
                .ToList();
        }

        public List<SubscriptionObject> GetUserSubscriptionsByGymName(string name)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Gyms) &&
                            x.Gyms != null &&
                            x.Gyms.Exists(y => string.Compare(y.Name, name, true) == 0 || y.Name.ToLower().Contains(name.ToLower()))
                       )
                .ToList();
        }

        /// <summary>
        /// Get user subscriptions from subscribed Quest reward keyword
        /// </summary>
        /// <param name="reward">Ques reward keyword</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByQuest(string pokestopName, string reward)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Quests) &&
                            x.Quests != null &&
                            x.Quests.Exists(y =>
                                reward.Contains(y.RewardKeyword) ||
                                (y.PokestopName != null && (pokestopName.Contains(y.PokestopName) || string.Equals(pokestopName, y.PokestopName, StringComparison.OrdinalIgnoreCase)))
                            )
                      )
                .ToList();
        }

        /// <summary>
        /// Gets user subscriptions from subscribed Invasion encounter rewards
        /// </summary>
        /// <param name="encounterRewards">Invasion encounter rewards</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByInvasion(string pokestopName, InvasionCharacter gruntType, List<uint> encounterRewards)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Invasions) &&
                            x.Invasions != null &&
                            x.Invasions.Exists(y =>
                                (y.RewardPokemonId?.Intersects(encounterRewards) ?? false) ||
                                gruntType == y.InvasionType ||
                                (!string.IsNullOrEmpty(y.PokestopName) && !string.IsNullOrEmpty(pokestopName) && pokestopName.Contains(y.PokestopName)) || string.Equals(pokestopName, y.PokestopName, StringComparison.OrdinalIgnoreCase)
                            )
                      )
                .ToList();
        }



        /// <summary>
        /// Gets user subscriptions from subscribed Pokestop lures
        /// </summary>
        /// <param name="lureType">Pokestop lure type</param>
        /// <returns>Returns list of user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptionsByLureType(PokestopLureType lureType)
        {
            return _subscriptions?
                .Where(x => x.IsEnabled(NotificationStatusType.Lures) &&
                            x.Lures != null &&
                            x.Lures.Exists(y => lureType == y.LureType))
                .ToList();
        }

        /// <summary>
        /// Get all enabled user subscriptions
        /// </summary>
        /// <returns>Returns all enabled user subscription objects</returns>
        public List<SubscriptionObject> GetUserSubscriptions()
        {
            try
            {
                if (!IsDbConnectionOpen())
                {
                    throw new Exception("Not connected to database.");
                }

                var conn = GetConnection();
                var where = conn?
                    .From<SubscriptionObject>()?
                    .Where(x => x.Status != NotificationStatusType.None);
                var results = conn?
                    .LoadSelect(where)?
                    .ToList();
                return results;
            }
            catch (OutOfMemoryException mex)
            {
                _logger.Debug($"-------------------OUT OF MEMORY EXCEPTION!");
                _logger.Error(mex);
                Environment.FailFast($"Out of memory: {mex}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        /// <summary>
        /// Reload all user subscriptions
        /// </summary>
        public void ReloadSubscriptions()
        {
            // TODO: Only reload based on last_changed timestamp in metadata table

            var subs = GetUserSubscriptions();
            if (subs == null)
                return;

            _subscriptions = subs;
        }

        #endregion

        #region Remove

        /// <summary>
        /// Remove all user subscriptions based on guild id and user id
        /// </summary>
        /// <param name="guildId">Discord guild id to lookup</param>
        /// <param name="userId">Discord user id to lookup</param>
        /// <returns>Returns <c>true</c> if all subscriptions were removed, otherwise <c>false</c>.</returns>
        public static bool RemoveAllUserSubscriptions(ulong guildId, ulong userId)
        {
            _logger.Trace($"SubscriptionManager::RemoveAllUserSubscription [GuildId={guildId}, UserId={userId}]");

            try
            {
                using (var conn = DataAccessLayer.CreateFactory().Open())
                {
                    conn.Delete<PokemonSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<PvPSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<RaidSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<QuestSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<GymSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<InvasionSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<LureSubscription>(x => x.GuildId == guildId && x.UserId == userId);
                    conn.Delete<SubscriptionObject>(x => x.GuildId == guildId && x.UserId == userId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
        }

        #endregion

        #region Private Methods

        private System.Data.IDbConnection GetConnection()
        {
            return _connFactory.Open();
        }

        private bool IsDbConnectionOpen()
        {
            return _connFactory != null;
        }

        #endregion
    }
}
