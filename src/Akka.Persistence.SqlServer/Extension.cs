// -----------------------------------------------------------------------
// <copyright file="Extension.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;
using Hocon;

namespace Akka.Persistence.SqlServer
{
    public class SqlServerJournalSettings : JournalSettings
    {
        public const string ConfigPath = "akka.persistence.journal.sql-server";

        public SqlServerJournalSettings(Config config) : base(config)
        {
        }
    }

    public class SqlServerSnapshotSettings : SnapshotStoreSettings
    {
        public const string ConfigPath = "akka.persistence.snapshot-store.sql-server";

        public SqlServerSnapshotSettings(Config config) : base(config)
        {
        }
    }

    /// <summary>
    ///     An actor system extension initializing support for SQL Server persistence layer.
    /// </summary>
    public class SqlServerPersistence : IExtension
    {
        /// <summary>
        ///     Journal-related settings loaded from HOCON configuration.
        /// </summary>
        public readonly Config DefaultJournalConfig;

        /// <summary>
        ///     Snapshot store related settings loaded from HOCON configuration.
        /// </summary>
        public readonly Config DefaultSnapshotConfig;

        public SqlServerPersistence(ExtendedActorSystem system)
        {
            var defaultConfig = DefaultConfiguration();
            system.Settings.InjectTopLevelFallback(defaultConfig);

            DefaultJournalConfig = defaultConfig.GetConfig(SqlServerJournalSettings.ConfigPath);
            DefaultSnapshotConfig = defaultConfig.GetConfig(SqlServerSnapshotSettings.ConfigPath);
        }

        /// <summary>
        ///     Returns a default configuration for akka persistence SQLite-based journals and snapshot stores.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<SqlServerPersistence>(
                "Akka.Persistence.SqlServer.sql-server.conf");
        }

        public static SqlServerPersistence Get(ActorSystem system)
        {
            return system.WithExtension<SqlServerPersistence, SqlServerPersistenceProvider>();
        }
    }

    /// <summary>
    ///     Singleton class used to setup SQL Server backend for akka persistence plugin.
    /// </summary>
    public class SqlServerPersistenceProvider : ExtensionIdProvider<SqlServerPersistence>
    {
        public override SqlServerPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new SqlServerPersistence(system);
        }
    }
}