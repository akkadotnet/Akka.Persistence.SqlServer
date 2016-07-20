using System;
using Xunit;

namespace Akka.Persistence.SqlServer.Tests
{
    public class SqlServerConfigSpec : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Should_PostgreSql_journal_has_default_config()
        {
            SqlServerPersistence.Get(Sys);

            var config = Sys.Settings.Config.GetConfig("akka.persistence.journal.sql-server");

            Assert.NotNull(config);
            Assert.Equal("Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer", config.GetString("class"));
            Assert.Equal("akka.actor.default-dispatcher", config.GetString("plugin-dispatcher"));
            Assert.Equal(string.Empty, config.GetString("connection-string"));
            Assert.Equal(string.Empty, config.GetString("connection-string-name"));
            Assert.Equal(TimeSpan.FromSeconds(30), config.GetTimeSpan("connection-timeout"));
            Assert.Equal("dbo", config.GetString("schema-name"));
            Assert.Equal("EventJournal", config.GetString("table-name"));
            Assert.Equal("Metadata", config.GetString("metadata-table-name"));
            Assert.Equal(false, config.GetBoolean("auto-initialize"));
            Assert.Equal("Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common", config.GetString("timestamp-provider"));
        }

        [Fact]
        public void Should_PostgreSql_snapshot_has_default_config()
        {
            SqlServerPersistence.Get(Sys);

            var config = Sys.Settings.Config.GetConfig("akka.persistence.snapshot-store.sql-server");

            Assert.NotNull(config);
            Assert.Equal("Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer", config.GetString("class"));
            Assert.Equal("akka.actor.default-dispatcher", config.GetString("plugin-dispatcher"));
            Assert.Equal(string.Empty, config.GetString("connection-string"));
            Assert.Equal(string.Empty, config.GetString("connection-string-name"));
            Assert.Equal(TimeSpan.FromSeconds(30), config.GetTimeSpan("connection-timeout"));
            Assert.Equal("dbo", config.GetString("schema-name"));
            Assert.Equal("SnapshotStore", config.GetString("table-name"));
            Assert.Equal(false, config.GetBoolean("auto-initialize"));
        }
    }
}
