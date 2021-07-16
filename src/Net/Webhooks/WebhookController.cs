namespace WhMgr.Net.Webhooks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Newtonsoft.Json;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using WhMgr.Alarms;
    using WhMgr.Alarms.Filters;
    using WhMgr.Alarms.Models;
    using WhMgr.Configuration;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Localization;
    using WhMgr.Net;
    using WhMgr.Net.Configuration;
    using WhMgr.Net.Models;
    using WhMgr.Utilities;

    /// <summary>
    /// Webhook controller class to manage and filter Discord channel notifications.
    /// </summary>
    public class WebhookController
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("WHM", Program.LogLevel);

        private readonly object _geofencesLock = new object();
        private readonly HttpServer _http;
        private readonly Dictionary<ulong, AlarmList> _alarms;
        private readonly WhConfigHolder _config;
        private readonly Dictionary<long, WeatherCondition> _weather;
        private Dictionary<string, GymDetailsData> _gyms;

        #endregion

        #region Properties

        /// <summary>
        /// Gyms cache
        /// </summary>
        public IReadOnlyDictionary<string, GymDetailsData> Gyms
        {
            get
            {
                if (_gyms == null)
                {
                    _gyms = GymDetailsData.GetGyms(Data.DataAccessLayer.ScannerConnectionString);
                }
                return _gyms;
            }
        }

        /// <summary>
        /// Weather cells cache
        /// </summary>
        public IReadOnlyDictionary<long, WeatherCondition> Weather => _weather;

        #endregion

        #region Events

        #region Alarms

        /// <summary>
        /// Triggered upon a Pokemon matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<PokemonData>> PokemonAlarmTriggered;
        private void OnPokemonAlarmTriggered(PokemonData pkmn, AlarmObject alarm, ulong guildId)
        {
            PokemonAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<PokemonData>(pkmn, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a raid matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<RaidData>> RaidAlarmTriggered;
        private void OnRaidAlarmTriggered(RaidData raid, AlarmObject alarm, ulong guildId)
        {
            RaidAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<RaidData>(raid, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a field research quest matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<QuestData>> QuestAlarmTriggered;
        private void OnQuestAlarmTriggered(QuestData quest, AlarmObject alarm, ulong guildId)
        {
            QuestAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<QuestData>(quest, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a gym matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<GymData>> GymAlarmTriggered;
        private void OnGymAlarmTriggered(GymData gym, AlarmObject alarm, ulong guildId)
        {
            GymAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<GymData>(gym, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a gym's details matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<GymDetailsData>> GymDetailsAlarmTriggered;
        private void OnGymDetailsAlarmTriggered(GymDetailsData gymDetails, AlarmObject alarm, ulong guildId)
        {
            GymDetailsAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<GymDetailsData>(gymDetails, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a pokestop matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<PokestopData>> PokestopAlarmTriggered;
        private void OnPokestopAlarmTriggered(PokestopData pokestop, AlarmObject alarm, ulong guildId)
        {
            PokestopAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<PokestopData>(pokestop, alarm, guildId));
        }

        /// <summary>
        /// Triggered upon a weather cell matching an alarm filter
        /// </summary>
        public event EventHandler<AlarmEventTriggeredEventArgs<WeatherData>> WeatherAlarmTriggered;
        private void OnWeatherAlarmTriggered(WeatherData weather, AlarmObject alarm, ulong guildId)
        {
            WeatherAlarmTriggered?.Invoke(this, new AlarmEventTriggeredEventArgs<WeatherData>(weather, alarm, guildId));
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Triggered upon a Pokemon matching a subscribers subscription filter
        /// </summary>
        public event EventHandler<PokemonData> PokemonSubscriptionTriggered;

        private void OnPokemonSubscriptionTriggered(PokemonData pkmn)
        {
            PokemonSubscriptionTriggered?.Invoke(this, pkmn);
        }

        /// <summary>
        /// Triggered upon a raid matching a subscribers subscription filter
        /// </summary>
        public event EventHandler<RaidData> RaidSubscriptionTriggered;
        private void OnRaidSubscriptionTriggered(RaidData raid)
        {
            RaidSubscriptionTriggered?.Invoke(this, raid);
        }

        /// <summary>
        /// Triggered upon a field research quest matching a subscribers subscription filter
        /// </summary>
        public event EventHandler<QuestData> QuestSubscriptionTriggered;
        private void OnQuestSubscriptionTriggered(QuestData quest)
        {
            QuestSubscriptionTriggered?.Invoke(this, quest);
        }

        /// <summary>
        /// Triggered upon a pokestop matching a subscribers subscription filter
        /// </summary>
        public event EventHandler<PokestopData> InvasionSubscriptionTriggered;
        private void OnInvasionSubscriptionTriggered(PokestopData pokestop)
        {
            InvasionSubscriptionTriggered?.Invoke(this, pokestop);
        }

        public event EventHandler<PokestopData> LureSubscriptionTriggered;
        private void OnLureSubscriptionTriggered(PokestopData pokestop)
        {
            LureSubscriptionTriggered?.Invoke(this, pokestop);
        }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiate a new <see cref="WebhookController"/> class.
        /// </summary>
        /// <param name="config"><see cref="WhConfig"/> configuration class.</param>
        public WebhookController(WhConfigHolder config)
        {
            _logger.Trace($"WebhookManager::WebhookManager [Config={config}, Port={config.Instance.WebhookPort}, Servers={config.Instance.Servers.Count:N0}]");

            _gyms = new Dictionary<string, GymDetailsData>();
            _weather = new Dictionary<long, WeatherCondition>();
            _alarms = new Dictionary<ulong, AlarmList>();

            _config = config;
            _config.Reloaded += OnConfigReloaded;
            
            LoadGeofences();
            LoadAlarms();

            _http = new HttpServer(new HttpServerConfig
            {
                Host = _config.Instance.ListeningHost,
                Port = _config.Instance.WebhookPort,
                DespawnTimerMinimum = _config.Instance.DespawnTimeMinimumMinutes,
                CheckForDuplicates = _config.Instance.CheckForDuplicates,
            });
            _http.PokemonReceived += OnPokemonReceived;
            _http.RaidReceived += OnRaidReceived;
            _http.QuestReceived += OnQuestReceived;
            _http.PokestopReceived += OnPokestopReceived;
            _http.GymReceived += OnGymReceived;
            _http.GymDetailsReceived += OnGymDetailsReceived;
            _http.WeatherReceived += OnWeatherReceived;
            _http.IsDebug = _config.Instance.Debug;

            new Thread(() => {
                LoadAlarmsOnChange();
                LoadGeofencesOnChange();
            }).Start();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start webhook HTTP listener
        /// </summary>
        public void Start()
        {
            _http?.Start();
        }

        /// <summary>
        /// Stop webhook HTTP listener
        /// </summary>
        public void Stop()
        {
            _http?.Stop();
        }

        public List<GeofenceItem> GetServerGeofences(ulong guildId)
        {
            var server = _config.Instance.Servers[guildId];

            return server.Geofences;
        }

        #endregion

        #region HttpServer Events

        private void OnPokemonReceived(object sender, DataReceivedEventArgs<PokemonData> e)
        {
            var pkmn = e.Data;
            if (DateTime.UtcNow.ConvertTimeFromCoordinates(pkmn.Latitude, pkmn.Longitude) > pkmn.DespawnTime)
            {
                _logger.Debug($"Pokemon {pkmn.Id} already despawned at {pkmn.DespawnTime}");
                return;
            }

            // Check if Pokemon is in event Pokemon list
            if (_config.Instance.EventPokemonIds.Contains(pkmn.Id) && _config.Instance.EventPokemonIds.Count > 0)
            {
                // Skip Pokemon if no IV stats.
                if (pkmn.IsMissingStats)
                    return;

                var iv = PokemonData.GetIV(pkmn.Attack, pkmn.Defense, pkmn.Stamina);
                // Skip Pokemon if IV is greater than 0%, less than 90%, and does not match any PvP league stats.
                if (iv > 0 && iv < _config.Instance.EventMinimumIV && !pkmn.MatchesGreatLeague && !pkmn.MatchesUltraLeague)
                    return;
            }

            ProcessPokemon(pkmn);
            OnPokemonSubscriptionTriggered(pkmn);
        }

        private void OnRaidReceived(object sender, DataReceivedEventArgs<RaidData> e)
        {
            var raid = e.Data;
            if (DateTime.UtcNow.ConvertTimeFromCoordinates(raid.Latitude, raid.Longitude) > raid.EndTime)
            {
                _logger.Debug($"Raid boss {raid.PokemonId} already despawned at {raid.EndTime}");
                return;
            }

            ProcessRaid(raid);
            OnRaidSubscriptionTriggered(raid);
        }

        private void OnQuestReceived(object sender, DataReceivedEventArgs<QuestData> e)
        {
            var quest = e.Data;
            ProcessQuest(quest);
            OnQuestSubscriptionTriggered(quest);
        }

        private void OnPokestopReceived(object sender, DataReceivedEventArgs<PokestopData> e)
        {
            var pokestop = e.Data;
            if (pokestop.HasLure || pokestop.HasInvasion)
            {
                ProcessPokestop(pokestop);
                OnInvasionSubscriptionTriggered(pokestop);
                OnLureSubscriptionTriggered(pokestop);
            }
        }

        private void OnGymReceived(object sender, DataReceivedEventArgs<GymData> e)
        {
            var gym = e.Data;
            ProcessGym(gym);
        }

        private void OnGymDetailsReceived(object sender, DataReceivedEventArgs<GymDetailsData> e)
        {
            var gymDetails = e.Data;
            ProcessGymDetails(gymDetails);
        }

        private void OnWeatherReceived(object sender, DataReceivedEventArgs<WeatherData> e)
        {
            var weather = e.Data;
            ProcessWeather(weather);
        }

        #endregion
        
        private void OnConfigReloaded()
        {
            // Reload stuff after config changes
            LoadGeofences();
            LoadAlarms();
        }

        #region Geofence Initialization

        private void LoadGeofences()
        {
            foreach (var (serverId, serverConfig) in _config.Instance.Servers)
            {
                serverConfig.Geofences.Clear();
                
                var geofenceFiles = serverConfig.GeofenceFiles;
                var geofences = new List<GeofenceItem>();

                if (geofenceFiles != null && geofenceFiles.Any())
                {
                    foreach (var file in geofenceFiles)
                    {
                        var filePath = Path.Combine(Strings.GeofenceFolder, file);
                        
                        try
                        {
                            var fileGeofences = GeofenceItem.FromFile(filePath);
                            
                            geofences.AddRange(fileGeofences);
                            
                            _logger.Info($"Successfully loaded {fileGeofences.Count} geofences from {file}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Could not load Geofence file {file} (for server {serverId}):");
                            _logger.Error(ex);
                        }
                    }
                }
                
                serverConfig.Geofences.AddRange(geofences);
            }
        }

        private void LoadGeofencesOnChange()
        {
            _logger.Trace($"WebhookManager::LoadGeofencesOnChange");

            var geofencesFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), Strings.GeofenceFolder));
            var fileWatcher = new FileWatcher(geofencesFolder);

            fileWatcher.Changed += (sender, e) => {
                try
                {
                    _logger.Debug("Reloading Geofences");

                    LoadGeofences();
                    LoadAlarms(); // Reload alarms after geofences too
                }
                catch (Exception ex)
                {
                    _logger.Error("Error while reloading geofences:");
                    _logger.Error(ex);
                }
            };
            fileWatcher.Start();
        }

        #endregion

        #region Alarms Initialization

        private void LoadAlarms()
        {
            _alarms.Clear();
            
            foreach (var (serverId, serverConfig) in _config.Instance.Servers)
            {
                var alarms = LoadAlarms(serverId, serverConfig.AlarmsFile);
                
                _alarms.Add(serverId, alarms);
            }
        }

        private AlarmList LoadAlarms(ulong forGuildId, string alarmsFilePath)
        {
            _logger.Trace($"WebhookManager::LoadAlarms [AlarmsFilePath={alarmsFilePath}]");

            var alarmsFolder = Path.Combine(Directory.GetCurrentDirectory(), Strings.AlarmsFolder);
            var alarmPath = Path.Combine(alarmsFolder, alarmsFilePath);
            if (!File.Exists(alarmPath))
            {
                _logger.Error($"Failed to load file alarms file '{alarmPath}'...");
                return null;
            }

            var alarmData = File.ReadAllText(alarmPath);
            if (string.IsNullOrEmpty(alarmData))
            {
                _logger.Error($"Failed to load '{alarmPath}', file is empty...");
                return null;
            }

            var alarms = JsonConvert.DeserializeObject<AlarmList>(alarmData);
            if (alarms == null)
            {
                _logger.Error($"Failed to deserialize the alarms file '{alarmPath}', make sure you don't have any json syntax errors.");
                return null;
            }

            _logger.Info($"Alarms file {alarmPath} was loaded successfully.");

            foreach (var alarm in alarms.Alarms)
            {
                if (alarm.Geofences != null)
                {
                    foreach (var geofenceName in alarm.Geofences)
                    {
                        lock (_geofencesLock)
                        {
                            // First try and find loaded geofences for this server by name or filename (so we don't have to parse already loaded files again)
                            var server = _config.Instance.Servers[forGuildId];
                            var geofences = server.Geofences.Where(g => g.Name.Equals(geofenceName, StringComparison.OrdinalIgnoreCase) ||
                                                                        g.Filename.Equals(geofenceName, StringComparison.OrdinalIgnoreCase)).ToList();

                            if (geofences.Any())
                            {
                                alarm.GeofenceItems.AddRange(geofences);
                            }
                            else
                            {
                                // Try and load from a file instead
                                var filePath = Path.Combine(Strings.GeofenceFolder, geofenceName);

                                if (!File.Exists(filePath))
                                {
                                    _logger.Warn($"Could not find Geofence file \"{geofenceName}\" for alarm \"{alarm.Name}\"");
                                    continue;
                                }

                                var fileGeofences = GeofenceItem.FromFile(filePath);
                                
                                alarm.GeofenceItems.AddRange(fileGeofences);
                                _logger.Info($"Successfully loaded {fileGeofences.Count} geofences from {geofenceName}");
                            }
                        }
                    }
                }

                alarm.LoadAlerts();
                alarm.LoadFilters();
            }

            return alarms;
        }

        private void LoadAlarmsOnChange()
        {
            _logger.Trace($"WebhookManager::LoadAlarmsOnChange");

            var alarmsFolder = Path.Combine(Directory.GetCurrentDirectory(), Strings.AlarmsFolder);
            foreach (var (guildId, guildConfig) in _config.Instance.Servers)
            {
                var alarmsFile = guildConfig.AlarmsFile;
                var path = Path.GetFullPath(Path.Combine(alarmsFolder, alarmsFile));
                var fileWatcher = new FileWatcher(path);
                
                fileWatcher.Changed += (sender, e) => {
                    try
                    {
                        _logger.Debug("Reloading Alarms");
                        _alarms[guildId] = LoadAlarms(guildId, path);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error while reloading alarms:");
                        _logger.Error(ex);
                    }
                };
                fileWatcher.Start();
            }
        }

        #endregion

        #region Data Processing

        private void ProcessPokemon(PokemonData pkmn)
        {
            if (pkmn == null)
                return;

            Statistics.Instance.TotalReceivedPokemon++;
            if (pkmn.IsMissingStats)
                Statistics.Instance.TotalReceivedPokemonMissingStats++;
            else
                Statistics.Instance.TotalReceivedPokemonWithStats++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnablePokemon)
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var pokemonAlarms = alarms.Alarms?.FindAll(x => x.Filters?.Pokemon?.Pokemon != null && x.Filters.Pokemon.Enabled);
                if (pokemonAlarms == null)
                    continue;

                for (var i = 0; i < pokemonAlarms.Count; i++)
                {
                    var alarm = pokemonAlarms[i];
                    if (alarm.Filters.Pokemon == null)
                        continue;

                    if (!alarm.Filters.Pokemon.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokemon {pkmn.Id}: Pokemon filter not enabled.");
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(pkmn.Latitude, pkmn.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokemon {pkmn.Id}: not in geofence.");
                        continue;
                    }

                    if ((alarm.Filters.Pokemon.IsEvent && !(pkmn.IsEvent.HasValue && pkmn.IsEvent.Value)) ||
                        (!alarm.Filters.Pokemon.IsEvent && pkmn.IsEvent.HasValue && pkmn.IsEvent.Value))
                    {
                        // Pokemon does not have event flag indicating it was checked with event account and event filter is set, skip.
                        // or Pokemon has event but filter is set to not include them
                        continue;
                    }

                    if (alarm.Filters.Pokemon.FilterType == FilterType.Exclude && alarm.Filters.Pokemon.Pokemon.Contains(pkmn.Id))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    if (alarm.Filters.Pokemon.FilterType == FilterType.Include && alarm.Filters.Pokemon.Pokemon?.Count > 0 && !alarm.Filters.Pokemon.Pokemon.Contains(pkmn.Id))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    var formName = Translator.Instance.GetFormName(pkmn.FormId)?.ToLower();
                    if (alarm.Filters.Pokemon.FilterType == FilterType.Exclude && alarm.Filters.Pokemon.Forms.Select(x => x.ToLower()).Contains(formName))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id} with form {pkmn.FormId} ({formName}): filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    if (alarm.Filters.Pokemon.FilterType == FilterType.Include && alarm.Filters.Pokemon.Forms?.Count > 0 && !alarm.Filters.Pokemon.Forms.Select(x => x.ToLower()).Contains(formName))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id} with form {pkmn.FormId} ({formName}): filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    var costumeName = Translator.Instance.GetCostumeName(pkmn.Costume)?.ToLower();
                    if (alarm.Filters.Pokemon.FilterType == FilterType.Exclude && alarm.Filters.Pokemon.Costumes.Select(x => x.ToLower()).Contains(costumeName))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id} with costume {pkmn.Costume} ({costumeName}): filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    if (alarm.Filters.Pokemon.FilterType == FilterType.Include && alarm.Filters.Pokemon.Costumes?.Count > 0 && !alarm.Filters.Pokemon.Costumes.Select(x => x.ToLower()).Contains(costumeName))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id} with costume {pkmn.Costume} ({costumeName}): filter {alarm.Filters.Pokemon.FilterType}.");
                        continue;
                    }

                    if (alarm.Filters.Pokemon.IgnoreMissing && pkmn.IsMissingStats)
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: IgnoreMissing=true.");
                        continue;
                    }

                    if (!Filters.MatchesIV(pkmn.IV, alarm.Filters.Pokemon.MinimumIV, alarm.Filters.Pokemon.MaximumIV))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: MinimumIV={alarm.Filters.Pokemon.MinimumIV} and MaximumIV={alarm.Filters.Pokemon.MaximumIV} and IV={pkmn.IV}.");
                        continue;
                    }

                    if (!Filters.MatchesCP(pkmn.CP, alarm.Filters.Pokemon.MinimumCP, alarm.Filters.Pokemon.MaximumCP))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: MinimumCP={alarm.Filters.Pokemon.MinimumCP} and MaximumCP={alarm.Filters.Pokemon.MaximumCP} and CP={pkmn.CP}.");
                        continue;
                    }

                    if (!Filters.MatchesLvl(pkmn.Level, alarm.Filters.Pokemon.MinimumLevel, alarm.Filters.Pokemon.MaximumLevel))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: MinimumLevel={alarm.Filters.Pokemon.MinimumLevel} and MaximumLevel={alarm.Filters.Pokemon.MaximumLevel} and Level={pkmn.Level}.");
                        continue;
                    }

                    var skipGreatLeague = alarm.Filters.Pokemon.IsPvpGreatLeague &&
                        !(pkmn.MatchesGreatLeague && pkmn.GreatLeague.Exists(x =>
                            Filters.MatchesPvPRank(x.Rank ?? 4096, alarm.Filters.Pokemon.MinimumRank, alarm.Filters.Pokemon.MaximumRank)
                            && x.CP >= Strings.MinimumGreatLeagueCP && x.CP <= Strings.MaximumGreatLeagueCP));
                    if (skipGreatLeague)
                        continue;

                    var skipUltraLeague = alarm.Filters.Pokemon.IsPvpUltraLeague &&
                        !(pkmn.MatchesUltraLeague && pkmn.UltraLeague.Exists(x =>
                            Filters.MatchesPvPRank(x.Rank ?? 4096, alarm.Filters.Pokemon.MinimumRank, alarm.Filters.Pokemon.MaximumRank)
                            && x.CP >= Strings.MinimumUltraLeagueCP && x.CP <= Strings.MaximumUltraLeagueCP));
                    if (skipUltraLeague)
                        continue;

                    //if (!Filters.MatchesGender(pkmn.Gender, alarm.Filters.Pokemon.Gender.ToString()))
                    //{
                    //    //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping pokemon {pkmn.Id}: DesiredGender={alarm.Filters.Pokemon.Gender} and Gender={pkmn.Gender}.");
                    //    continue;
                    //}

                    if ((alarm.Filters?.Pokemon?.IgnoreMissing ?? false) && !(float.TryParse(pkmn.Height, out var height) && float.TryParse(pkmn.Weight, out var weight) && Filters.MatchesSize(pkmn.Id.GetSize(height, weight), alarm.Filters?.Pokemon?.Size)))
                    {
                        continue;
                    }

                    OnPokemonAlarmTriggered(pkmn, alarm, guildId);
                }
            }
        }

        private void ProcessRaid(RaidData raid)
        {
            if (raid == null)
                return;

            if (raid.IsEgg)
                Statistics.Instance.TotalReceivedEggs++;
            else
                Statistics.Instance.TotalReceivedRaids++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnableRaids)
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var raidAlarms = alarms.Alarms.FindAll(x => (x.Filters.Raids?.Enabled ?? false) || (x.Filters.Eggs?.Enabled ?? false));
                for (var i = 0; i < raidAlarms.Count; i++)
                {
                    var alarm = raidAlarms[i];
                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(raid.Latitude, raid.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping raid Pokemon={raid.PokemonId}, Level={raid.Level}: not in geofence.");
                        continue;
                    }

                    if (raid.IsEgg)
                    {
                        if (alarm.Filters.Eggs == null)
                            continue;

                        if (!alarm.Filters.Eggs.Enabled)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping level {raid.Level} raid egg: raids filter not enabled.");
                            continue;
                        }

                        if (!int.TryParse(raid.Level, out var level))
                        {
                            _logger.Warn($"[{alarm.Name}] [{geofence.Name}] Failed to parse '{raid.Level}' as raid level.");
                            continue;
                        }

                        if (!(level >= alarm.Filters.Eggs.MinimumLevel && level <= alarm.Filters.Eggs.MaximumLevel))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping level {raid.Level} raid egg: '{raid.Level}' does not meet the MinimumLevel={alarm.Filters.Eggs.MinimumLevel} and MaximumLevel={alarm.Filters.Eggs.MaximumLevel} filters.");
                            continue;
                        }

                        if (alarm.Filters.Eggs.OnlyEx && !raid.IsExEligible)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping level {raid.Level} raid egg: only ex {alarm.Filters.Eggs.OnlyEx}.");
                            continue;
                        }

                        if (alarm.Filters.Eggs.Team != PokemonTeam.All && alarm.Filters.Eggs.Team != raid.Team)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping level {raid.Level} raid egg: '{raid.Team}' does not meet Team={alarm.Filters.Eggs.Team} filter.");
                            continue;
                        }

                        OnRaidAlarmTriggered(raid, alarm, guildId);
                    }
                    else
                    {
                        if (alarm.Filters.Raids == null)
                            continue;

                        if (!alarm.Filters.Raids.Enabled)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: raids filter not enabled.");
                            continue;
                        }

                        if (!int.TryParse(raid.Level, out var level))
                        {
                            _logger.Warn($"[{alarm.Name}] [{geofence.Name}] Failed to parse '{raid.Level}' as raid level.");
                            continue;
                        }

                        if (!(level >= alarm.Filters.Raids.MinimumLevel && level <= alarm.Filters.Raids.MaximumLevel))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping level {raid.Level} raid {raid.PokemonId}: '{raid.Level}' does not meet the MinimumLevel={alarm.Filters.Raids.MinimumLevel} and MaximumLevel={alarm.Filters.Raids.MaximumLevel} filters.");
                            continue;
                        }

                        if (alarm.Filters.Raids.FilterType == FilterType.Exclude && alarm.Filters.Raids.Pokemon.Contains(raid.PokemonId))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        if (alarm.Filters.Raids.FilterType == FilterType.Include && (!alarm.Filters.Raids.Pokemon.Contains(raid.PokemonId) && alarm.Filters.Raids.Pokemon?.Count > 0))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        var formName = Translator.Instance.GetFormName(raid.Form)?.ToLower();
                        if (alarm.Filters.Raids.FilterType == FilterType.Exclude && alarm.Filters.Raids.Forms.Select(x => x.ToLower()).Contains(formName))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.Id} with form {raid.Form} ({formName}): filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        if (alarm.Filters.Raids.FilterType == FilterType.Include && alarm.Filters.Raids.Forms?.Count > 0 && !alarm.Filters.Raids.Forms.Select(x => x.ToLower()).Contains(formName))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.Id} with form {raid.Form} ({formName}): filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        var costumeName = Translator.Instance.GetCostumeName(raid.Costume)?.ToLower();
                        if (alarm.Filters.Raids.FilterType == FilterType.Exclude && alarm.Filters.Raids.Costumes.Select(x => x.ToLower()).Contains(costumeName))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.Id} with costume {raid.Costume} ({costumeName}): filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        if (alarm.Filters.Raids.FilterType == FilterType.Include && alarm.Filters.Raids.Costumes?.Count > 0 && !alarm.Filters.Raids.Costumes.Select(x => x.ToLower()).Contains(costumeName))
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.Id} with costume {raid.Costume} ({costumeName}): filter {alarm.Filters.Raids.FilterType}.");
                            continue;
                        }

                        if (alarm.Filters.Raids.OnlyEx && !raid.IsExEligible)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: only ex {alarm.Filters.Raids.OnlyEx}.");
                            continue;
                        }

                        if (alarm.Filters.Raids.Team != PokemonTeam.All && alarm.Filters.Raids.Team != raid.Team)
                        {
                            //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: '{raid.Team}' does not meet Team={alarm.Filters.Raids.Team} filter.");
                            continue;
                        }

                        if (alarm.Filters.Raids.IgnoreMissing && raid.IsMissingStats)
                        {
                            _logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping raid boss {raid.PokemonId}: IgnoreMissing=true.");
                            continue;
                        }
                        
                        OnRaidAlarmTriggered(raid, alarm, guildId);
                    }
                }
            }
        }

        private void ProcessQuest(QuestData quest)
        {
            if (quest == null)
                return;

            Statistics.Instance.TotalReceivedQuests++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnableQuests)
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var rewardKeyword = quest.GetReward();
                var questAlarms = alarms.Alarms.FindAll(x => x.Filters?.Quests?.RewardKeywords != null && x.Filters.Quests.Enabled);
                for (var i = 0; i < questAlarms.Count; i++)
                {
                    var alarm = questAlarms[i];
                    if (alarm.Filters.Quests == null)
                        continue;

                    if (!alarm.Filters.Quests.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping quest PokestopId={quest.PokestopId}, Type={quest.Type}: quests filter not enabled.");
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(quest.Latitude, quest.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping quest PokestopId={quest.PokestopId}, Type={quest.Type}: not in geofence.");
                        continue;
                    }

                    var contains = alarm.Filters.Quests.RewardKeywords.Select(x => x.ToLower()).FirstOrDefault(x => rewardKeyword.ToLower().Contains(x.ToLower())) != null;
                    if (alarm.Filters.Quests.FilterType == FilterType.Exclude && contains)
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping quest PokestopId={quest.PokestopId}, Type={quest.Type}: filter {alarm.Filters.Quests.FilterType}.");
                        continue;
                    }

                    if (!(alarm.Filters.Quests.FilterType == FilterType.Include && (contains || alarm.Filters.Quests?.RewardKeywords.Count == 0)))
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping quest PokestopId={quest.PokestopId}: filter {alarm.Filters.Quests.FilterType}.");
                        continue;
                    }

                    if (!contains && alarm.Filters?.Quests?.RewardKeywords?.Count > 0)
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping quest PokestopId={quest.PokestopId}, Type={quest.Type}: rewards does not match reward keywords.");
                        continue;
                    }

                    if (alarm.Filters.Quests.IsShiny && !quest.IsShiny)
                    {
                        //_logger.Info($"[{alarm.Name}] [{geofence.Name}] Skipping quest PokestopId={quest.PokestopId}, Type={quest.Type}: filter IsShiny={alarm.Filters.Quests.IsShiny} Quest={quest.IsShiny}.");
                        continue;
                    }

                    OnQuestAlarmTriggered(quest, alarm, guildId);
                }
            }
        }

        private void ProcessPokestop(PokestopData pokestop)
        {
            //Skip if Pokestop filter is not defined.
            if (pokestop == null)
                return;

            Statistics.Instance.TotalReceivedPokestops++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                //Skip if EnablePokestops is disabled in the config.
                if (!alarms.EnablePokestops)
                    continue;

                //Skip if alarms list is null or empty.
                if (alarms.Alarms?.Count == 0)
                    continue;

                var pokestopAlarms = alarms.Alarms.FindAll(x => x.Filters?.Pokestops != null && x.Filters.Pokestops.Enabled);
                for (var i = 0; i < pokestopAlarms.Count; i++)
                {
                    var alarm = pokestopAlarms[i];
                    if (alarm.Filters.Pokestops == null)
                        continue;

                    if (!alarm.Filters.Pokestops.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name}: pokestop filter not enabled.");
                        continue;
                    }

                    if (!alarm.Filters.Pokestops.Lured && pokestop.HasLure)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name}: lure filter not enabled.");
                        continue;
                    }

                    if (!alarm.Filters.Pokestops.LureTypes.Select(x => x.ToLower()).Contains(pokestop.LureType.ToString().ToLower()) && alarm.Filters.Pokestops?.LureTypes?.Count > 0)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name}, LureType={pokestop.LureType}: lure type not included.");
                        continue;
                    }

                    if (!alarm.Filters.Pokestops.Invasions && pokestop.HasInvasion)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name}: invasion filter not enabled.");
                        continue;
                    }

                    if (alarm.Filters.Pokestops.InvasionTypes.ContainsKey(pokestop.GruntType) && !alarm.Filters.Pokestops.InvasionTypes[pokestop.GruntType] && pokestop.HasInvasion)
                    {
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(pokestop.Latitude, pokestop.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name} because not in geofence.");
                        continue;
                    }

                    OnPokestopAlarmTriggered(pokestop, alarm, guildId);
                }
            }
        }

        private void ProcessGym(GymData gym)
        {
            if (gym == null)
                return;

            Statistics.Instance.TotalReceivedGyms++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnableGyms)
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var gymAlarms = alarms.Alarms?.FindAll(x => x.Filters?.Gyms != null && x.Filters.Gyms.Enabled);
                for (var i = 0; i < gymAlarms.Count; i++)
                {
                    var alarm = gymAlarms[i];
                    if (alarm.Filters.Gyms == null)
                        continue;

                    if (!alarm.Filters.Gyms.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym GymId={gym.GymId}, GymName={gym.GymName}: gym filter not enabled.");
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(gym.Latitude, gym.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym GymId={gym.GymId}, GymName={gym.GymName} because not in geofence.");
                        continue;
                    }

                    OnGymAlarmTriggered(gym, alarm, guildId);
                }
            }
        }

        private void ProcessGymDetails(GymDetailsData gymDetails)
        {
            if (gymDetails == null)
                return;

            Statistics.Instance.TotalReceivedGyms++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnableGyms) //GymDetails
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var gymDetailsAlarms = alarms.Alarms?.FindAll(x => x.Filters?.Gyms != null && x.Filters.Gyms.Enabled);
                for (var i = 0; i < gymDetailsAlarms.Count; i++)
                {
                    var alarm = gymDetailsAlarms[i];
                    if (alarm.Filters.Gyms == null)
                        continue;

                    if (!alarm.Filters.Gyms.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym GymId={gym.GymId}, Name={gym.Name}: gym filter not enabled.");
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(gymDetails.Latitude, gymDetails.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym details GymId={gymDetails.GymId}, GymName={gymDetails.GymName}: not in geofence.");
                        continue;
                    }

                    if ((alarm.Filters?.Gyms?.UnderAttack ?? false) && !gymDetails.InBattle)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym details GymId={gymDetails.GymId}, GymName{gymDetails.GymName}, not under attack.");
                        continue;
                    }

                    if (alarm.Filters?.Gyms?.Team != gymDetails.Team && alarm.Filters?.Gyms?.Team != PokemonTeam.All)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym details GymId={gymDetails.GymId}, GymName{gymDetails.GymName}, not specified team {alarm.Filters.Gyms.Team}.");
                        continue;
                    }

                    if (!_gyms.ContainsKey(gymDetails.GymId))
                    {
                        _gyms.Add(gymDetails.GymId, gymDetails);
                        //OnGymDetailsAlarmTriggered(gymDetails, alarm, guildId);
                        //continue;
                    }

                    /*
                    var oldGym = _gyms[gymDetails.GymId];
                    var changed = oldGym.Team != gymDetails.Team || gymDetails.InBattle;
                    if (!changed)
                        return;
                    */

                    OnGymDetailsAlarmTriggered(gymDetails, alarm, guildId);
                }
            }
        }

        private void ProcessWeather(WeatherData weather)
        {
            if (weather == null)
                return;

            Statistics.Instance.TotalReceivedWeathers++;

            foreach (var (guildId, alarms) in _alarms)
            {
                if (alarms == null)
                    continue;

                if (!alarms.EnableWeather)
                    continue;

                if (alarms.Alarms?.Count == 0)
                    continue;

                var weatherAlarms = alarms.Alarms.FindAll(x => x.Filters?.Weather != null && x.Filters.Weather.Enabled);
                for (var i = 0; i < weatherAlarms.Count; i++)
                {
                    var alarm = weatherAlarms[i];
                    if (alarm.Filters.Weather == null)
                        continue;

                    if (!alarm.Filters.Weather.Enabled)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping pokestop PokestopId={pokestop.PokestopId}, Name={pokestop.Name}: pokestop filter not enabled.");
                        continue;
                    }

                    var geofence = GeofenceService.GetGeofence(alarm.GeofenceItems, new Location(weather.Latitude, weather.Longitude));
                    if (geofence == null)
                    {
                        //_logger.Info($"[{alarm.Name}] Skipping gym details GymId={gymDetails.GymId}, GymName={gymDetails.GymName}: not in geofence.");
                        continue;
                    }

                    if (!alarm.Filters.Weather.WeatherTypes.Contains(weather.GameplayCondition))
                    {
                        // Weather is not in list of accepted ones to send alarms for
                        continue;
                    }

                    if (!_weather.ContainsKey(weather.Id))
                    {
                        _weather.Add(weather.Id, weather.GameplayCondition);
                        OnWeatherAlarmTriggered(weather, alarm, guildId);
                        continue;
                    }

                    var oldWeather = _weather[weather.Id];
                    // If previous weather and current weather are the same then don't report it.
                    if (oldWeather == weather.GameplayCondition)
                        continue;

                    OnWeatherAlarmTriggered(weather, alarm, guildId);
                }
            }
        }

        #endregion

        public void SetGym(string id, GymDetailsData gymDetails)
        {
            _gyms[id] = gymDetails;
        }

        public void SetWeather(long id, WeatherCondition type)
        {
            _weather[id] = type;
        }

        #region Geofence Utilities

        /// <summary>
        /// Get the geofence the provided location falls within.
        /// </summary>
        /// <param name="guildId">The guild ID in which to look for Geofences</param>
        /// <param name="latitude">Latitude geocoordinate</param>
        /// <param name="longitude">Longitude geocoordinate</param>
        /// <returns>Returns a <see cref="GeofenceItem"/> object the provided location falls within.</returns>
        public GeofenceItem GetGeofence(ulong guildId, double latitude, double longitude)
        {
            var server = _config.Instance.Servers[guildId];
            
            return GeofenceService.GetGeofence(server.Geofences, new Location(latitude, longitude));
        }

        #endregion
    }
}
