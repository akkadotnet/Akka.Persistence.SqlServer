using System;
using System.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;

namespace Akka.Persistence.SqlServer
{

    public class SqlServerJournalSettings : JournalSettings
    {
        public const string ConfigPath = "akka.persistence.journal.sql-server";

        /// <summary>
        /// Flag determining in in case of event journal table missing, it should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; private set; }

        public SqlServerJournalSettings(Config config) : base(config)
        {
            AutoInitialize = config.GetBoolean("auto-initialize");
        }
    }

    public class SqlServerSnapshotSettings : SnapshotStoreSettings
    {
        public const string ConfigPath = "akka.persistence.snapshot-store.sql-server";

        /// <summary>
        /// Flag determining in in case of snapshot store table missing, it should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; private set; }

        public SqlServerSnapshotSettings(Config config) : base(config)
        {
            AutoInitialize = config.GetBoolean("auto-initialize");
        }
    }

    /// <summary>
    /// An actor system extension initializing support for SQL Server persistence layer.
    /// </summary>
    public class SqlServerPersistence : IExtension
    {
        /// <summary>
        /// Returns a default configuration for akka persistence SQLite-based journals and snapshot stores.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<SqlServerPersistence>("Akka.Persistence.SqlServer.sql-server.conf");
        }

        public static SqlServerPersistence Get(ActorSystem system)
        {
            return system.WithExtension<SqlServerPersistence, SqlServerPersistenceProvider>();
        }

        /// <summary>
        /// Journal-related settings loaded from HOCON configuration.
        /// </summary>
        public readonly SqlServerJournalSettings JournalSettings;

        /// <summary>
        /// Snapshot store related settings loaded from HOCON configuration.
        /// </summary>
        public readonly SqlServerSnapshotSettings SnapshotSettings;

        public SqlServerPersistence(ExtendedActorSystem system)
        {
            system.Settings.InjectTopLevelFallback(DefaultConfiguration());

            JournalSettings = new SqlServerJournalSettings(system.Settings.Config.GetConfig(SqlServerJournalSettings.ConfigPath));
            SnapshotSettings = new SqlServerSnapshotSettings(system.Settings.Config.GetConfig(SqlServerSnapshotSettings.ConfigPath));

            if (JournalSettings.AutoInitialize)
            {
                var connectionString = string.IsNullOrEmpty(JournalSettings.ConnectionString)
                    ? ConfigurationManager.ConnectionStrings[JournalSettings.ConnectionStringName].ConnectionString
                    : JournalSettings.ConnectionString;

                SqlServerInitializer.CreateSqlServerJournalTables(connectionString, JournalSettings.SchemaName, JournalSettings.TableName);
            }

            if (SnapshotSettings.AutoInitialize)
            {
                var connectionString = string.IsNullOrEmpty(SnapshotSettings.ConnectionString)
                    ? ConfigurationManager.ConnectionStrings[SnapshotSettings.ConnectionStringName].ConnectionString
                    : SnapshotSettings.ConnectionString;

                SqlServerInitializer.CreateSqlServerSnapshotStoreTables(connectionString, SnapshotSettings.SchemaName, SnapshotSettings.TableName);
            }
        }
    }

    /// <summary>
    /// Singleton class used to setup SQL Server backend for akka persistence plugin.
    /// </summary>
    public class SqlServerPersistenceProvider : ExtensionIdProvider<SqlServerPersistence>
    {
        public override SqlServerPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new SqlServerPersistence(system);
        }
    }
}