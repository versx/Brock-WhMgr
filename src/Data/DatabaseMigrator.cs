namespace WhMgr.Data
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using ServiceStack.OrmLite;

    using WhMgr.Diagnostics;

    /// <summary>
    /// Database migration class
    /// </summary>
    public class DatabaseMigrator
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("MIGRATOR", Program.LogLevel);

        private static readonly string CurrentDatabaseVersionPath = Path.Combine(Strings.AppFolder, "db_version.txt");

        /// <summary>
        /// Gets a value determining whether the migration has finished or not
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// Gets the migrations folder path
        /// </summary>
        public string MigrationsFolder => Path.Combine
        (
            Path.Combine(Directory.GetCurrentDirectory(), "../"),
            Strings.MigrationsFolder
        );

        /// <summary>
        /// Instantiates a new <see cref="DatabaseMigrator"/> class
        /// </summary>
        public DatabaseMigrator()
        {
            // Create directory if not exists
            var dir = Path.GetDirectoryName(CurrentDatabaseVersionPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Get current version from metadata table
            var currentVersion = GetCurrentVersion();
            if (currentVersion == 0)
            {
                var result = SetCurrentVersion(currentVersion);
                if (!result)
                {
                    _logger.Error($"Failed to set current database version: {currentVersion}");
                    Environment.Exit(-1);
                }
            }

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
            Migrate(currentVersion, newestVersion).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Migrate the database from a specified version to the next version
        /// </summary>
        /// <param name="fromVersion">Database version to migrate from</param>
        /// <param name="toVersion">Database version to migrate to</param>
        /// <returns></returns>
        private async Task Migrate(int fromVersion, int toVersion)
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
                        var result = await Execute(sql);
                        if (result != 0)
                        {
                            // Failed to execute query
                            _logger.Warn($"Failed to execute migration: {sql}");
                            Environment.Exit(-1);
                        }
                        _logger.Debug($"Migration execution result: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Migration failed: {ex}");
                        Environment.Exit(-1);
                    }
                }

                // Take a break
                Thread.Sleep(2000);

                // Build query to update metadata table version key
                var newVersion = fromVersion + 1;
                try
                {
                    // Execute update version SQL
                    var result = SetCurrentVersion(newVersion);
                    if (result)
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
                await Migrate(newVersion, toVersion);
            }
            if (fromVersion == toVersion)
            {
                _logger.Info("Migration done");
                Finished = true;
            }
            await Task.CompletedTask;
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
        /// Set current database version
        /// </summary>
        /// <param name="version">Current database version to save</param>
        private static bool SetCurrentVersion(int version)
        {
            try
            {
                // Save current version
                File.WriteAllText(CurrentDatabaseVersionPath, version.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Get the current saved database version
        /// </summary>
        /// <returns>Returns the current saved database version</returns>
        private static int GetCurrentVersion()
        {
            if (!File.Exists(CurrentDatabaseVersionPath))
            {
                // Last version that was in database
                SetCurrentVersion(0);
            }
            using (var sr = new StreamReader(CurrentDatabaseVersionPath))
            {
                var data = sr.ReadToEnd();
                if (string.IsNullOrEmpty(data))
                {
                    _logger.Error($"Failed to get latest database version from {CurrentDatabaseVersionPath}, returning default.");
                    return 0;
                }
                if (!int.TryParse(data, out var currentVersion))
                {
                    _logger.Error($"Failed to parse latest database version {data}");
                    return 0;
                }
                return currentVersion;
            }
        }

        /// <summary>
        /// Execute a raw SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to execute</param>
        /// <returns>Returns the result value from the statement</returns>
        private static async Task<int> Execute(string sql)
        {
            if (string.IsNullOrEmpty(DataAccessLayer.ConnectionString))
                return default;

            try
            {
                using (var db = DataAccessLayer.CreateFactory(DataAccessLayer.ConnectionString).Open())
                {
                    var query = await db.ExecuteSqlAsync(sql);
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
