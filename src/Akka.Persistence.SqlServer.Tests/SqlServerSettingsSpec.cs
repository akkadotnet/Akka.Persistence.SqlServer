//-----------------------------------------------------------------------
// <copyright file="SqlServerSettingsSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Dispatch;
using FluentAssertions;
using Xunit;

namespace Akka.Persistence.SqlServer.Tests
{
    public class SqlServerConfigSpec : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Should_SqlServer_journal_has_default_config()
        {
            SqlServerPersistence.Get(Sys);
            var config = Sys.Settings.Config.GetConfig("akka.persistence.journal.sql-server");

            config.Should().NotBeNull();
            config.GetString("class").Should().Be("Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer");
            config.GetString("plugin-dispatcher").Should().Be(Dispatchers.DefaultDispatcherId);
            config.GetString("connection-string").Should().BeEmpty();
            config.GetString("connection-string-name").Should().BeNullOrEmpty();
            config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
            config.GetString("schema-name").Should().Be("dbo");
            config.GetString("table-name").Should().Be("EventJournal");
            config.GetString("metadata-table-name").Should().Be("Metadata");
            config.GetBoolean("auto-initialize").Should().BeFalse();
            config.GetString("timestamp-provider").Should().Be("Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common");
        }

        [Fact]
        public void Should_SqlServer_snapshot_has_default_config()
        {
            SqlServerPersistence.Get(Sys);
            var config = Sys.Settings.Config.GetConfig("akka.persistence.snapshot-store.sql-server");

            config.Should().NotBeNull();
            config.GetString("class").Should().Be("Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer");
            config.GetString("plugin-dispatcher").Should().Be(Dispatchers.DefaultDispatcherId);
            config.GetString("connection-string").Should().BeEmpty();
            config.GetString("connection-string-name").Should().BeNullOrEmpty();
            config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
            config.GetString("schema-name").Should().Be("dbo");
            config.GetString("table-name").Should().Be("SnapshotStore");
            config.GetBoolean("auto-initialize").Should().BeFalse();
        }
    }
}
