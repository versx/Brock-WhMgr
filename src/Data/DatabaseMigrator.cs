﻿namespace WhMgr.Data
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.EntityFrameworkCore;

    using WhMgr.Diagnostics;
    using WhMgr.Data.Models;
    using WhMgr.Data.Factories;

    /// <summary>
    /// Database migration class
    /// </summary>
    public class DatabaseMigrator
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("MIGRATOR", Program.LogLevel);

        /// <summary>
        /// Gets a value determining whether the migration has finished or not
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// Gets the migrations folder path
        /// </summary>
        public string MigrationsFolder => Path.Combine
        (
            Path.Combine(Directory.GetCurrentDirectory(), "../../.."),
            Strings.MigrationsFolder
        );

        /// <summary>
        /// Instantiates a new <see cref="DatabaseMigrator"/> class
        /// </summary>
        public DatabaseMigrator()
        {
            // Create the metadata table
            Execute(Strings.SQL_CREATE_TABLE_METADATA);

            // Get current version from metadata table
            var currentVersion = int.Parse(GetMetadata("DB_VERSION")?.Value ?? "0");

            // Get newest version from migration files
            var newestVersion = GetNewestDbVersion();
            _logger.Info($"Current: {currentVersion}, Latest: {newestVersion}");

            // Attempt to migrate the database
            if (currentVersion < newestVersion)
            {
                // Wait 30 seconds and let user know we are about to migrate the database and for them to make
                // a backup until we handle backups and rollbacks.
                _logger.Info("MIGRATION IS ABOUT TO START IN 30 SECONDS, PLEASE MAKE SURE YOU HAVE A BACKUP!!!");
                Thread.Sleep(30 * 1000);
            }
            Migrate(currentVersion, newestVersion);
        }

        /// <summary>
        /// Get newest database version from local migration file numbers
        /// </summary>
        /// <returns>Returns the latest version number</returns>
        private int GetNewestDbVersion()
        {
            var current = 0;
            var keepChecking = true;
            while (keepChecking)
            {
                var path = Path.Combine(MigrationsFolder, (current + 1) + ".sql");
                if (File.Exists(path))
                    current++;
                else
                    keepChecking = false;
            }
            return current;
        }

        /// <summary>
        /// Migrate the database from a specified version to the next version
        /// </summary>
        /// <param name="fromVersion">Database version to migrate from</param>
        /// <param name="toVersion">Database version to migrate to</param>
        /// <returns></returns>
        private void Migrate(int fromVersion, int toVersion)
        {
            if (fromVersion < toVersion)
            {
                _logger.Info($"Migrating database to version {fromVersion + 1}");
                var sqlFile = Path.Combine(MigrationsFolder, (fromVersion + 1) + ".sql");

                // Read SQL file and remove any new lines
                var migrateSql = File.ReadAllText(sqlFile)?.Replace("\r", "").Replace("\n", "");

                // If the migration file contains multiple queries, split them up
                var sqlSplit = migrateSql.Split(';');

                // Loop through the migration queries
                foreach (var sql in sqlSplit)
                {
                    // If the SQL query is null, skip...
                    if (string.IsNullOrEmpty(sql))
                        continue;

                    try
                    {
                        // Execute the SQL query
                        var result = Execute(sql);
                        if (result != 0)
                        {
                            // Failed to execute query
                            _logger.Warn($"Failed to execute migration: {sql}");
                            continue;
                        }
                        _logger.Debug($"Migration execution result: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Migration failed: {ex}");
                    }
                }

                // Take a break
                Thread.Sleep(2000);

                // Build query to update metadata table version key
                var newVersion = fromVersion + 1;
                var updateVersionSQL = string.Format(Strings.SQL_INSERT_METADATA_FORMAT, newVersion);
                try
                {
                    // Execute update version SQL
                    var result = Execute(updateVersionSQL);
                    if (result > 0)
                    {
                        // Success
                    }
                    _logger.Debug($"Result: {result}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Migration failed: {ex}");
                    Environment.Exit(-1);
                }
                _logger.Info("Migration successful");
                Migrate(newVersion, toVersion);
            }
            if (fromVersion == toVersion)
            {
                _logger.Info("Migration done");
                Finished = true;
            }
        }

        /// <summary>
        /// Execute a raw SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to execute</param>
        /// <returns>Returns the result value from the statement</returns>
        public static int Execute(string sql)
        {
            if (string.IsNullOrEmpty(DbContextFactory.ConnectionString))
                return default;

            try
            {
                using (var db = DbContextFactory.CreateSubscriptionContext(DbContextFactory.ConnectionString))
                {
                    var query = db.Database.ExecuteSqlRaw(sql);
                    return query;
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                _logger.Error(ex);
            }
            return default;
        }

        /// <summary>
        /// Get a metadata table value by key
        /// </summary>
        /// <param name="key">Table key to lookup</param>
        /// <returns>Returns the metadata key and value</returns>
        public static Metadata GetMetadata(string key)
        {
            if (string.IsNullOrEmpty(DbContextFactory.ConnectionString))
                return default;

            try
            {
                using (var db = DbContextFactory.CreateSubscriptionContext(DbContextFactory.ConnectionString))
                {
                    return db.Metadata.Find(key);
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                _logger.Error(ex);
            }
            return null;
        }
    }
}