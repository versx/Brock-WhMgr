﻿namespace WhMgr
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using WhMgr.Data;
    using WhMgr.Net.Models;

    public class Statistics
    {
        #region Singleton

        private static Statistics _instance;
        public static Statistics Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Statistics();
                }

                return _instance;
            }
        }

        #endregion

        #region Properties

        public long PokemonAlarmsSent { get; set; }

        public long RaidAlarmsSent { get; set; }

        public long EggAlarmsSent { get; set; }

        public long QuestAlarmsSent { get; set; }

        public long LureAlarmsSent { get; set; }

        public long InvasionAlarmsSent { get; set; }

        public long GymAlarmsSent { get; set; }

        public long WeatherAlarmsSent { get; set; }

        public long SubscriptionPokemonSent { get; set; }

        public long SubscriptionRaidsSent { get; set; }

        public long SubscriptionQuestsSent { get; set; }

        public long SubscriptionInvasionsSent { get; set; }

        public long SubscriptionLuresSent { get; set; }

        public Dictionary<DateTime, PokemonData> Hundos { get; }

        public long TotalReceivedPokemon { get; set; }

        public long TotalReceivedPokemonMissingStats { get; set; }

        public long TotalReceivedPokemonWithStats { get; set; }

        public long TotalReceivedRaids { get; set; }

        public long TotalReceivedEggs { get; set; }

        public long TotalReceivedGyms { get; set; }

        public long TotalReceivedPokestops { get; set; }

        public long TotalReceivedQuests { get; set; }

        public long TotalReceivedInvasions { get; set; }

        public long TotalReceivedLures { get; set; }

        public long TotalReceivedWeathers { get; set; }

        #endregion

        #region Constructor

        public Statistics()
        {
            Hundos = new Dictionary<DateTime, PokemonData>();
        }

        #endregion

        #region Public Methods

        public void AddHundredIV(PokemonData pokemon)
        {
            Hundos.Add(DateTime.Now, pokemon);
        }

        public static void WriteOut()
        {
            if (!Directory.Exists(Strings.StatsFolder))
            {
                Directory.CreateDirectory(Strings.StatsFolder);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine($"__**Pokemon**__");
            sb.AppendLine($"Alarms Sent: {Instance.PokemonAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedPokemon:N0}");
            sb.AppendLine($"With IV Stats: {Instance.TotalReceivedPokemonWithStats:N0}");
            sb.AppendLine($"Missing IV Stats: {Instance.TotalReceivedPokemonMissingStats:N0}");
            sb.AppendLine($"Subscriptions Sent: {Instance.SubscriptionPokemonSent:N0}");
            sb.AppendLine();
            sb.AppendLine("__**Raids**__");
            sb.AppendLine($"Egg Alarms Sent: {Instance.EggAlarmsSent:N0}");
            sb.AppendLine($"Raids Alarms Sent: {Instance.RaidAlarmsSent:N0}");
            sb.AppendLine($"Total Eggs Received: {Instance.TotalReceivedRaids:N0}");
            sb.AppendLine($"Total Raids Received: {Instance.TotalReceivedRaids:N0}");
            sb.AppendLine($"Raid Subscriptions Sent: {Instance.SubscriptionRaidsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Quests**__");
            sb.AppendLine($"Alarms Sent: {Instance.QuestAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedQuests:N0}");
            sb.AppendLine($"Subscriptions Sent: {Instance.SubscriptionQuestsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Invasions**__");
            sb.AppendLine($"Alarms Sent: {Instance.InvasionAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedInvasions:N0}");
            sb.AppendLine($"Subscriptions Sent: {Instance.SubscriptionInvasionsSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Lures**__");
            sb.AppendLine($"Alarms Sent: {Instance.LureAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedLures:N0}");
            sb.AppendLine($"Subscriptions Sent: {Instance.SubscriptionLuresSent:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Gyms**__");
            sb.AppendLine($"Alarms Sent: {Instance.GymAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedGyms:N0}");
            sb.AppendLine();
            sb.AppendLine($"__**Weather**__");
            sb.AppendLine($"Alarms Sent: {Instance.WeatherAlarmsSent:N0}");
            sb.AppendLine($"Total Received: {Instance.TotalReceivedWeathers:N0}");
            sb.AppendLine();
            var hundos = string.Join(Environment.NewLine, Instance.Hundos.Select(x => $"{x.Key}: {MasterFile.Instance.Pokedex[x.Value.Id].Name} {x.Value.IV} IV {x.Value.CP} CP"));
            sb.AppendLine($"**Recent 100% Spawns**");
            sb.AppendLine(string.IsNullOrEmpty(hundos) ? "None" : hundos);

            try
            {
                File.WriteAllText(Path.Combine(Strings.StatsFolder, string.Format(Strings.StatsFileName, DateTime.Now.ToString("yyyy-MM-dd_hhmmss"))), sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Reset()
        {
            PokemonAlarmsSent = 0;
            RaidAlarmsSent = 0;
            EggAlarmsSent = 0;
            QuestAlarmsSent = 0;
            LureAlarmsSent = 0;
            InvasionAlarmsSent = 0;
            GymAlarmsSent = 0;
            WeatherAlarmsSent = 0;

            SubscriptionPokemonSent = 0;
            SubscriptionRaidsSent = 0;
            SubscriptionQuestsSent = 0;
            SubscriptionInvasionsSent = 0;

            TotalReceivedPokemon = 0;
            TotalReceivedPokemonMissingStats = 0;
            TotalReceivedPokemonWithStats = 0;
            TotalReceivedRaids = 0;
            TotalReceivedEggs = 0;
            TotalReceivedQuests = 0;
            TotalReceivedPokestops = 0;
            TotalReceivedLures = 0;
            TotalReceivedInvasions = 0;
            TotalReceivedGyms = 0;
            TotalReceivedWeathers = 0;
        }

        #endregion
    }
}