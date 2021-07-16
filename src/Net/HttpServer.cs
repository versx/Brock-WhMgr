namespace WhMgr.Net
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;

    using Newtonsoft.Json;
    using POGOProtos.Rpc;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    using WhMgr.Comparers;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Net.Models;
    using WhMgr.Net.Configuration;

    /// <summary>
    /// HTTP listener class
    /// </summary>
    public class HttpServer
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("HTTP", Program.LogLevel);
        private static readonly object _lock = new object();
        private readonly Dictionary<string, ScannedPokemon> _processedPokemon;
        private readonly Dictionary<string, ScannedRaid> _processedRaids;
        private readonly Dictionary<string, ScannedGym> _processedGyms;
        private readonly Dictionary<string, ScannedPokestop> _processedPokestops;
        private readonly Dictionary<string, ScannedQuest> _processedQuests;
        private readonly Dictionary<long, WeatherData> _processedWeather;
        private readonly System.Timers.Timer _clearCacheTimer;
        private HttpListener _server;
        private bool _initialized = false;
        private readonly int _despawnTimerMinimumMinutes = 5;
        private static string _endpoint;
        private readonly bool _checkForDuplicates;

        #endregion

        #region Properties

        /// <summary>
        /// HTTP listening interface/host address
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Http listening port
        /// </summary>
        public ushort Port { get; }

        /// <summary>
        /// Logs incoming webhook data if set to <c>true</c>
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Skips webhook raid eggs
        /// </summary>
        public bool SkipEggs { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Trigged when a Pokemon webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<PokemonData>> PokemonReceived;

        private void OnPokemonReceived(PokemonData pokemon) => PokemonReceived?.Invoke(this, new DataReceivedEventArgs<PokemonData>(pokemon));

        /// <summary>
        /// Trigged when a Raid or Raid Egg webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<RaidData>> RaidReceived;

        private void OnRaidReceived(RaidData raid) => RaidReceived?.Invoke(this, new DataReceivedEventArgs<RaidData>(raid));

        /// <summary>
        /// Trigged when a Field Research Quest webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<QuestData>> QuestReceived;

        private void OnQuestReceived(QuestData quest) => QuestReceived?.Invoke(this, new DataReceivedEventArgs<QuestData>(quest));

        /// <summary>
        /// Trigged when a Pokestop webhook (lure/invasion) payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<PokestopData>> PokestopReceived;

        private void OnPokestopReceived(PokestopData pokestop) => PokestopReceived?.Invoke(this, new DataReceivedEventArgs<PokestopData>(pokestop));

        /// <summary>
        /// Trigged when a Gym webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<GymData>> GymReceived;

        private void OnGymReceived(GymData gym) => GymReceived?.Invoke(this, new DataReceivedEventArgs<GymData>(gym));

        /// <summary>
        /// Trigged when a Gym Details webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<GymDetailsData>> GymDetailsReceived;

        private void OnGymDetailsReceived(GymDetailsData gymDetails) => GymDetailsReceived?.Invoke(this, new DataReceivedEventArgs<GymDetailsData>(gymDetails));

        /// <summary>
        /// Trigged when a Weather webhook payload is received.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs<WeatherData>> WeatherReceived;

        private void OnWeatherReceived(WeatherData weather) => WeatherReceived?.Invoke(this, new DataReceivedEventArgs<WeatherData>(weather));

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="HttpServer"/> class.
        /// </summary>
        /// <param name="httpConfig">Http server config</param>
        public HttpServer(HttpServerConfig httpConfig)
        {
            // If no host is set use wildcard for all host interfaces
            Host = httpConfig.Host ?? "*";
            Port = httpConfig.Port;
            _processedPokemon = new Dictionary<string, ScannedPokemon>();
            _processedRaids = new Dictionary<string, ScannedRaid>();
            _processedGyms = new Dictionary<string, ScannedGym>();
            _processedPokestops = new Dictionary<string, ScannedPokestop>();
            _processedQuests = new Dictionary<string, ScannedQuest>();
            _processedWeather = new Dictionary<long, WeatherData>();
            _despawnTimerMinimumMinutes = httpConfig.DespawnTimerMinimum;
            _clearCacheTimer = new System.Timers.Timer { Interval = 60000 * 15 };
            _clearCacheTimer.Elapsed += (sender, e) => OnClearCache();
            _checkForDuplicates = httpConfig.CheckForDuplicates;
            
            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the HTTP listener server
        /// </summary>
        public void Start()
        {
            _logger.Trace($"Start");

            if (!_initialized)
            {
                _logger.Error("HTTP listener not initalized, make sure you run as administrator or root.");
                return;
            }

            if (_server.IsListening)
            {
                _logger.Debug($"Already listening, failed to start...");
                return;
            }

            try
            {
                _logger.Info($"Starting...");
                _server.Start();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    _logger.Warn("You need to run the following command in order to not have to run as Administrator or root every start:");
                    _logger.Warn($"netsh http add urlacl url={_endpoint} user={Environment.UserDomainName}\\{Environment.UserName} listen=yes");
                }
                else
                {
                    throw;
                }
            }

            if (_server.IsListening)
            {
                _logger.Debug($"Listening on port {Port}...");
            }

            _logger.Info($"Starting HttpServer request handler...");
            var requestThread = new Thread(RequestHandler) { IsBackground = true };
            requestThread.Start();

            // Start the cache cleaner
            _clearCacheTimer.Start();
        }

        /// <summary>
        /// Attempts to stop the HTTP listener server
        /// </summary>
        public void Stop()
        {
            _logger.Trace($"Stop");

            if (!_server.IsListening)
            {
                _logger.Debug($"Not running, failed to stop...");
                return;
            }

            _logger.Info($"Stopping...");
            _server.Stop();

            // Stop the cache cleaner
            _clearCacheTimer.Stop();
        }

        #endregion

        #region Data Parsing Methods

        private void RequestHandler()
        {
            while (_server.IsListening)
            {
                var context = _server.GetContext();
                var response = context.Response;

                if (context.Request?.InputStream == null)
                    continue;

                // Read from the POST data input stream of the request
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    try
                    {
                        // Read to the end of the stream as a string
                        var data = sr.ReadToEnd();
                        ParseData(data);
                    }
                    catch (HttpListenerException hle)
                    {
                        _logger.Error(hle);

                        //Disconnected, reconnect.
                        HandleDisconnect();
                    }
                }

                try
                {
                    // Convert the default response message to UTF8 encoded bytes
                    var buffer = Encoding.UTF8.GetBytes(Strings.DefaultResponseMessage);
                    response.ContentLength64 = buffer.Length;
                    if (response?.OutputStream != null)
                    {
                        // Write the response buffer to the output stream
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    // Close the response
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                Thread.Sleep(10);
            }
        }

        private void ParseData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            try
            {
                if (IsDebug)
                {
                    lock (_lock)
                    {
                        File.AppendAllText(Strings.DebugLogFileName, data + Environment.NewLine);
                    }
                }

                var messages = JsonConvert.DeserializeObject<List<WebhookMessage>>(data);
                // If we fail to deserialize webhook payload, skip
                if (messages == null)
                    return;

                for (var i = 0; i < messages.Count; i++)
                {
                    var message = messages[i];
                    switch (message.Type)
                    {
                        case PokemonData.WebHookHeader:
                            ParsePokemon(message.Message);
                            break;
                        case GymData.WebhookHeader:
                            ParseGym(message.Message);
                            break;
                        case GymDetailsData.WebhookHeader:
                             ParseGymDetails(message.Message);
                            break;
                        case RaidData.WebHookHeader:
                            ParseRaid(message.Message);
                            break;
                        case QuestData.WebHookHeader:
                            ParseQuest(message.Message);
                            break;
                        case PokestopData.WebhookHeader:
                        case PokestopData.WebhookHeaderInvasion:
                            ParsePokestop(message.Message);
                            break;
                        case WeatherData.WebhookHeader:
                            ParseWeather(message.Message);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(data));
            }
        }

        private void ParsePokemon(dynamic message)
        {
            try
            {
                PokemonData pokemon = JsonConvert.DeserializeObject<PokemonData>(Convert.ToString(message));
                if (pokemon == null)
                {
                    _logger.Error($"Failed to parse Pokemon webhook object: {message}");
                    return;
                }

                pokemon.SetDespawnTime();

                // Only process Pokemon with despawn timers that meet a specific minimum
                if (pokemon.SecondsLeft.TotalMinutes < _despawnTimerMinimumMinutes)
                    return;

                if (_checkForDuplicates)
                {
                    lock (_processedPokemon)
                    {
                        if (_processedPokemon.ContainsKey(pokemon.EncounterId) && (pokemon.IsMissingStats || !pokemon.IsMissingStats && !_processedPokemon[pokemon.EncounterId].IsMissingStats))
                            return;
                        if (!_processedPokemon.ContainsKey(pokemon.EncounterId))
                            _processedPokemon.Add(pokemon.EncounterId, new ScannedPokemon(pokemon));
                        if (!pokemon.IsMissingStats && _processedPokemon[pokemon.EncounterId].IsMissingStats)
                            _processedPokemon[pokemon.EncounterId] = new ScannedPokemon(pokemon);
                    }
                }

                OnPokemonReceived(pokemon);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParseRaid(dynamic message)
        {
            try
            {
                RaidData raid = JsonConvert.DeserializeObject<RaidData>(Convert.ToString(message));
                if (raid == null)
                {
                    _logger.Error($"Failed to parse Pokemon webhook object: {message}");
                    return;
                }

                /*
                if (SkipEggs && raid.PokemonId == 0)
                {
                    _logger.Debug($"Level {raid.Level} Egg, skipping...");
                    return;
                }
                */

                raid.SetTimes();

                if (_checkForDuplicates)
                {
                    lock (_processedRaids)
                    {
                        if (_processedRaids.ContainsKey(raid.GymId))
                        {
                            if (_processedRaids[raid.GymId].PokemonId == raid.PokemonId &&
                                _processedRaids[raid.GymId].FormId == raid.Form &&
                                _processedRaids[raid.GymId].CostumeId == raid.Costume &&
                                _processedRaids[raid.GymId].Level == raid.Level &&
                                !_processedRaids[raid.GymId].IsExpired)
                            {
                                // Processed raid already
                                return;
                            }

                            _processedRaids[raid.GymId] = new ScannedRaid(raid);
                        }
                        else
                        {
                            _processedRaids.Add(raid.GymId, new ScannedRaid(raid));
                        }
                    }
                }

                OnRaidReceived(raid);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParseQuest(dynamic message)
        {
            try
            {
                QuestData quest = JsonConvert.DeserializeObject<QuestData>(Convert.ToString(message));
                if (quest == null)
                {
                    _logger.Error($"Failed to parse Quest webhook object: {message}");
                    return;
                }

                if (_checkForDuplicates)
                {
                    lock (_processedQuests)
                    {
                        if (_processedQuests.ContainsKey(quest.PokestopId))
                        {
                            if (_processedQuests[quest.PokestopId].Type == quest.Type &&
                                _processedQuests[quest.PokestopId].Rewards.ScrambledEquals(quest.Rewards, new QuestRewardEqualityComparer()) &&
                                _processedQuests[quest.PokestopId].Conditions.ScrambledEquals(quest.Conditions, new QuestConditionEqualityComparer()))
                            {
                                // Processed quest already
                                return;
                            }

                            _processedQuests[quest.PokestopId] = new ScannedQuest(quest);
                        }
                        else
                        {
                            _processedQuests.Add(quest.PokestopId, new ScannedQuest(quest));
                        }
                    }
                }

                OnQuestReceived(quest);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParsePokestop(dynamic message)
        {
            try
            {
                PokestopData pokestop = JsonConvert.DeserializeObject<PokestopData>(Convert.ToString(message));
                if (pokestop == null)
                {
                    _logger.Error($"Failed to parse pokestop webhook object: {message}");
                    return;
                }

                pokestop.SetTimes();

                if (_checkForDuplicates)
                {
                    lock (_processedPokestops)
                    {
                        if (_processedPokestops.ContainsKey(pokestop.PokestopId))
                        {
                            var processedLureAlready = _processedPokestops[pokestop.PokestopId].LureType == pokestop.LureType && _processedPokestops[pokestop.PokestopId].LureExpireTime == pokestop.LureExpireTime;
                            var processedInvasionAlready = _processedPokestops[pokestop.PokestopId].GruntType == pokestop.GruntType && _processedPokestops[pokestop.PokestopId].InvasionExpireTime == pokestop.InvasionExpireTime;
                            if (processedLureAlready || processedInvasionAlready)
                            {
                                //_logger.Debug($"PROCESSED LURE OR INVASION ALREADY: Id: {pokestop.PokestopId} Name: {pokestop.Name} Lure: {pokestop.LureType} Expires: {pokestop.LureExpireTime} Grunt: {pokestop.GruntType} Expires: {pokestop.InvasionExpireTime}");
                                // Processed pokestop lure or invasion already
                                return;
                            }

                            _processedPokestops[pokestop.PokestopId] = new ScannedPokestop(pokestop);
                        }
                        else
                        {
                            _processedPokestops.Add(pokestop.PokestopId, new ScannedPokestop(pokestop));
                        }
                    }
                }

                OnPokestopReceived(pokestop);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParseGym(dynamic message)
        {
            try
            {
                GymData gym = JsonConvert.DeserializeObject<GymData>(Convert.ToString(message));
                if (gym == null)
                {
                    _logger.Error($"Failed to parse gym webhook object: {message}");
                    return;
                }

                OnGymReceived(gym);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParseGymDetails(dynamic message)
        {
            try
            {
                GymDetailsData gymDetails = JsonConvert.DeserializeObject<GymDetailsData>(Convert.ToString(message));
                if (gymDetails == null)
                {
                    _logger.Error($"Failed to parse gym details webhook object: {message}");
                    return;
                }

                if (_checkForDuplicates)
                {
                    lock (_processedGyms)
                    {
                        if (_processedGyms.ContainsKey(gymDetails.GymId))
                        {
                            if (gymDetails.Team == gymDetails.Team &&
                                gymDetails.SlotsAvailable == gymDetails.SlotsAvailable &&
                                gymDetails.InBattle == gymDetails.InBattle)
                            {
                                // Gym already processed
                                return;
                            }

                            _processedGyms[gymDetails.GymId] = new ScannedGym(gymDetails);
                        }
                        else
                        {
                            _processedGyms.Add(gymDetails.GymId, new ScannedGym(gymDetails));
                        }
                    }
                }

                OnGymDetailsReceived(gymDetails);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        private void ParseWeather(dynamic message)
        {
            try
            {
                WeatherData weather = JsonConvert.DeserializeObject<WeatherData>(Convert.ToString(message));
                if (weather == null)
                {
                    _logger.Error($"Failed to parse weather webhook object: {message}");
                    return;
                }

                if (_checkForDuplicates)
                {
                    lock (_processedWeather)
                    {
                        if (_processedWeather.ContainsKey(weather.Id))
                        {
                            if (_processedWeather[weather.Id].GameplayCondition == weather.GameplayCondition &&
                                _processedWeather[weather.Id].CloudLevel == weather.CloudLevel &&
                                _processedWeather[weather.Id].FogLevel == weather.FogLevel &&
                                _processedWeather[weather.Id].RainLevel == weather.RainLevel &&
                                _processedWeather[weather.Id].Severity == weather.Severity &&
                                _processedWeather[weather.Id].SnowLevel == weather.SnowLevel &&
                                _processedWeather[weather.Id].WindLevel == weather.WindLevel &&
                                _processedWeather[weather.Id].SpecialEffectLevel == weather.SpecialEffectLevel &&
                                _processedWeather[weather.Id].WarnWeather == weather.WarnWeather &&
                                _processedWeather[weather.Id].WindDirection == weather.WindDirection)
                            {
                                // Processed weather already
                                return;
                            }

                            _processedWeather[weather.Id] = weather;
                        }
                        else
                        {
                            _processedWeather.Add(weather.Id, weather);
                        }
                    }
                }

                OnWeatherReceived(weather);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Debug(Convert.ToString(message));
            }
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            _logger.Trace("Initialize");
            try
            {
                _server = CreateListener();
                _endpoint = $"http://{Host}:{Port}/";
                if (!_server.Prefixes.Contains(_endpoint))
                {
                    _server.Prefixes.Add(_endpoint);
                }
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private HttpListener CreateListener()
        {
            return new HttpListener();
        }

        private void HandleDisconnect()
        {
            _logger.Warn("!!!!! HTTP listener disconnected, handling reconnect...");
            _logger.Warn("Stopping existing listeners...");
            Stop();

            _logger.Warn("Disposing of old listener objects...");
            _server.Close();
            _server = null;

            _logger.Warn("Initializing...");
            Initialize();

            //if (_requestThread != null)
            //{
            //    _logger.Info($"Exiting HttpServer main thread...");
            //    _requestThread.Abort();
            //    _requestThread = null;
            //}

            _logger.Warn("Starting back up...");
            Start();

            _logger.Debug("Disconnect handled.");
        }

        private void OnClearCache()
        {
            //List<string> expiredEncounters;
            lock (_processedPokemon)
            {
                var expiredEncounters = _processedPokemon.Where(pair => pair.Value.IsExpired).Select(pair => pair.Key).ToList();
                foreach (var encounterId in expiredEncounters)
                {
                    // Spawn expired, remove from cache
                    _processedPokemon.Remove(encounterId);
                }
            }

            //List<string> expiredRaids;
            lock (_processedRaids)
            {
                var expiredRaids = _processedRaids.Where(pair => pair.Value.IsExpired).Select(pair => pair.Key).ToList();
                foreach (var gymId in expiredRaids)
                {
                    // Gym expired, remove from cache
                    _processedRaids.Remove(gymId);
                }
            }

            //List<string> expiredQuests;
            lock (_processedQuests)
            {
                var expiredQuests = _processedQuests.Where(pair => pair.Value.IsExpired).Select(pair => pair.Key).ToList();
                foreach (var pokestopId in expiredQuests)
                {
                    // Quest expired, remove from cache
                    _processedQuests.Remove(pokestopId);
                }
            }

            //List<string> expiredPokestops;
            lock (_processedPokestops)
            {
                var expiredPokestops = _processedPokestops.Where(pair => pair.Value.IsExpired).Select(pair => pair.Key).ToList();
                foreach (var pokestopId in expiredPokestops)
                {
                    // Pokestop lure or invasion expired, remove from cache
                    _processedPokestops.Remove(pokestopId);
                }
            }

            // Log expired ones outside lock so that we don't hog too much time on _processedPokemon, _processedRaids, _processedQuests, and _processedPokestops
            /*
            foreach (var encounterId in expiredEncounters)
                _logger.Debug($"Removed expired Pokemon spawn {encounterId} from cache");

            foreach (var gymId in expiredRaids)
                _logger.Debug($"Removed expired Raid for Gym {gymId} from cache");

            foreach (var pokestopId in expiredQuests)
                _logger.Debug($"Removed expired Quest for Pokestop {pokestopId} from cache");

            foreach (var pokestopId in expiredPokestops)
                _logger.Debug($"Removed expired Pokestop lure or invasion {pokestopId} from cache");
            */
        }

        #endregion

        private class WebhookMessage
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("message")]
            public dynamic Message { get; set; }
        }

        #region Cache Structs

        private struct ScannedPokemon
        {
            public double Latitude { get; }

            public double Longitude { get; }

            public bool IsMissingStats { get; }

            public DateTime DespawnTime { get; }

            public bool IsExpired
            {
                get
                {
                    var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
                    return now > DespawnTime;
                }
            }

            public ScannedPokemon(PokemonData pokemon)
            {
                Latitude = pokemon.Latitude;
                Longitude = pokemon.Longitude;
                IsMissingStats = pokemon.IsMissingStats;
                DespawnTime = pokemon.DespawnTime;
            }
        }

        private struct ScannedRaid
        {
            public double Latitude { get; }

            public double Longitude { get; }

            public string Level { get; }

            public uint PokemonId { get; }

            public int FormId { get; }

            public int CostumeId { get; }

            public DateTime ExpireTime { get; }

            public bool IsExpired
            {
                get
                {
                    var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
                    return now > ExpireTime;
                }
            }

            public ScannedRaid(RaidData raid)
            {
                Latitude = raid.Latitude;
                Longitude = raid.Longitude;
                Level = raid.Level;
                PokemonId = raid.PokemonId;
                FormId = raid.Form;
                CostumeId = raid.Costume;
                ExpireTime = raid.EndTime;
            }
        }

        private struct ScannedQuest
        {
            public double Latitude { get; }

            public double Longitude { get; }

            public QuestType Type { get; }

            public List<QuestRewardMessage> Rewards { get; }

            public List<QuestConditionMessage> Conditions { get; }

            public DateTime Added { get; }

            public bool IsExpired
            {
                get
                {
                    var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
                    return now.Day != Added.Day;
                }
            }

            public ScannedQuest(QuestData quest)
            {
                Latitude = quest.Latitude;
                Longitude = quest.Longitude;
                Type = quest.Type;
                Rewards = quest.Rewards;
                Conditions = quest.Conditions;
                Added = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
            }
        }

        private struct ScannedPokestop
        {
            public double Latitude { get; }

            public double Longitude { get; }

            public PokestopLureType LureType { get; }

            public DateTime LureExpireTime { get; }

            public InvasionCharacter GruntType { get; }

            public DateTime InvasionExpireTime { get; }

            public bool IsExpired
            {
                get
                {
                    var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
                    return now > LureExpireTime || now > InvasionExpireTime;
                }
            }

            public ScannedPokestop(PokestopData pokestop)
            {
                Latitude = pokestop.Latitude;
                Longitude = pokestop.Longitude;
                LureType = pokestop.LureType;
                LureExpireTime = pokestop.LureExpireTime;
                GruntType = pokestop.GruntType;
                InvasionExpireTime = pokestop.InvasionExpireTime;
            }
        }

        private struct ScannedGym
        {
            public PokemonTeam Team { get; }

            public int SlotsAvailable { get; }

            public bool InBattle { get; }

            public ScannedGym(GymDetailsData gym)
            {
                Team = gym.Team;
                SlotsAvailable = gym.SlotsAvailable;
                InBattle = gym.InBattle;
            }
        }

        #endregion
    }
}