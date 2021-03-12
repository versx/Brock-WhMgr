﻿using WhMgr.Configuration;

namespace WhMgr
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using WhMgr.Diagnostics;

    class Program
    {
        /// <summary>
        /// Gets or sets the manager name
        /// </summary>
        public static string ManagerName { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the global log level to use
        /// </summary>
        public static LogLevel LogLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronous main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns></returns>
        static async Task MainAsync(string[] args)
        {
            // Parse command line arguments if given
            var arguments = CommandLine.ParseArgs(new string[] { "--", "-" }, args);
            var configFilePath = string.Empty;
            var managerName = string.Empty;
            // Loop through the parsed command line arguments and set the key values associated with each argument provided
            foreach (var (key, value) in arguments)
            {
                switch (key.ToLower())
                {
                    case "config":
                    case "c":
                        configFilePath = value?.ToString() ?? Strings.ConfigFileName;
                        break;
                    case "name":
                    case "n":
                        managerName = value?.ToString() ?? "Default";
                        break;
                }
            }

            configFilePath = Path.Combine(Environment.CurrentDirectory, string.IsNullOrEmpty(configFilePath) ? Strings.ConfigFileName : configFilePath);
            ManagerName = managerName;
            var logger = EventLogger.GetLogger(managerName);
            logger.Info(Strings.BannerAsciiText);
            logger.Info($"Version: {Strings.Version}");
            logger.Info($".NET Runtime Version: {System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion}\n");
            var whConfig = WhConfig.Load(configFilePath);
            if (whConfig == null)
            {
                logger.Error($"Failed to load config {configFilePath}.");
                return;
            }
            whConfig.FileName = configFilePath;
            whConfig.LoadDiscordServers();

            LogLevel = whConfig.LogLevel;

            // Start bot
            var bot = new Bot(new WhConfigHolder(whConfig));
            await bot.Start();

            // Keep the process alive
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}