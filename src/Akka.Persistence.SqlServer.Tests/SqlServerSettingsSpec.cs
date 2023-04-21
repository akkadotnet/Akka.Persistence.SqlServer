// -----------------------------------------------------------------------
// <copyright file="SqlServerSettingsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Persistence.Sql.Common;
using Akka.Persistence.Sql.Common.Extensions;
using Akka.Persistence.SqlServer.Journal;
using Akka.Persistence.SqlServer.Snapshot;
using FluentAssertions;
using FluentAssertions.Extensions;
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
            config.GetString("class").Should()
                .Be("Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer");
            config.GetString("plugin-dispatcher").Should().Be(Dispatchers.DefaultDispatcherId);
            config.GetString("connection-string").Should().BeEmpty();
            config.GetString("connection-string-name").Should().BeNullOrEmpty();
            config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
            config.GetString("schema-name").Should().Be("dbo");
            config.GetString("table-name").Should().Be("EventJournal");
            config.GetString("metadata-table-name").Should().Be("Metadata");
            config.GetBoolean("auto-initialize").Should().BeFalse();
            config.GetString("timestamp-provider").Should()
                .Be("Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common");
            config.GetString("read-isolation-level").Should().Be("unspecified");
            config.GetString("write-isolation-level").Should().Be("unspecified");
        }

        [Fact]
        public void SqlServer_JournalSettings_default_should_contain_default_config()
        {
            var config = SqlServerPersistence.Get(Sys).DefaultJournalConfig;
            var settings = new JournalSettings(config);

            // values should be correct
            settings.ConnectionString.Should().Be(string.Empty);
            settings.ConnectionStringName.Should().BeNullOrEmpty();
            settings.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
            settings.JournalTableName.Should().Be("EventJournal");
            settings.SchemaName.Should().Be("dbo");
            settings.MetaTableName.Should().Be("Metadata");
            settings.TimestampProvider.Should().Be("Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common");
            settings.ReadIsolationLevel.Should().Be(IsolationLevel.Unspecified);
            settings.WriteIsolationLevel.Should().Be(IsolationLevel.Unspecified);
            settings.AutoInitialize.Should().BeFalse();

            // values should reflect configuration
            settings.ConnectionString.Should().Be(config.GetString("connection-string"));
            settings.ConnectionStringName.Should().Be(config.GetString("connection-string-name"));
            settings.ConnectionTimeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            settings.JournalTableName.Should().Be(config.GetString("table-name"));
            settings.SchemaName.Should().Be(config.GetString("schema-name"));
            settings.MetaTableName.Should().Be(config.GetString("metadata-table-name"));
            settings.TimestampProvider.Should().Be(config.GetString("timestamp-provider"));
            settings.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            settings.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));
            settings.AutoInitialize.Should().Be(config.GetBoolean("auto-initialize"));
        }
        
        [Fact]
        public void Modified_SqlServer_JournalSettings_should_contain_proper_config()
        {
            var fullConfig = ConfigurationFactory.ParseString(@"
akka.persistence.journal {
	sql-server {
		connection-string = ""a""
		connection-string-name = ""b""
		connection-timeout = 3s
		table-name = ""c""
		auto-initialize = on
		metadata-table-name = ""d""
        schema-name = ""e""
	    serializer = ""f""
		read-isolation-level = snapshot
		write-isolation-level = snapshot
        sequential-access = on
	}
}").WithFallback(SqlServerPersistence.DefaultConfiguration());

            var config = fullConfig.GetConfig("akka.persistence.journal.sql-server");
            var settings = new JournalSettings(config);
            var executorConfig = SqlServerJournal.CreateQueryConfiguration(config, settings);

            // values should be correct
            settings.ConnectionString.Should().Be("a");
            settings.ConnectionStringName.Should().Be("b");
            settings.JournalTableName.Should().Be("c");
            settings.MetaTableName.Should().Be("d");
            settings.SchemaName.Should().Be("e");
            settings.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(3));
            settings.ReadIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            settings.WriteIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            settings.AutoInitialize.Should().BeTrue();

            executorConfig.JournalEventsTableName.Should().Be("c");
            executorConfig.MetaTableName.Should().Be("d");
            executorConfig.SchemaName.Should().Be("e");
#pragma warning disable CS0618
            executorConfig.DefaultSerializer.Should().Be("f");
#pragma warning restore CS0618
            executorConfig.Timeout.Should().Be(TimeSpan.FromSeconds(3));
            executorConfig.ReadIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            executorConfig.WriteIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            executorConfig.UseSequentialAccess.Should().BeTrue();

            // values should reflect configuration
            settings.ConnectionString.Should().Be(config.GetString("connection-string"));
            settings.ConnectionStringName.Should().Be(config.GetString("connection-string-name"));
            settings.ConnectionTimeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            settings.JournalTableName.Should().Be(config.GetString("table-name"));
            settings.SchemaName.Should().Be(config.GetString("schema-name"));
            settings.MetaTableName.Should().Be(config.GetString("metadata-table-name"));
            settings.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            settings.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));
            settings.AutoInitialize.Should().Be(config.GetBoolean("auto-initialize"));

            executorConfig.JournalEventsTableName.Should().Be(config.GetString("table-name"));
            executorConfig.MetaTableName.Should().Be(config.GetString("metadata-table-name"));
            executorConfig.SchemaName.Should().Be(config.GetString("schema-name"));
#pragma warning disable CS0618
            executorConfig.DefaultSerializer.Should().Be(config.GetString("serializer"));
#pragma warning restore CS0618
            executorConfig.Timeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            executorConfig.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            executorConfig.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));
            executorConfig.UseSequentialAccess.Should().Be(config.GetBoolean("auto-initialize"));
        }

        [Fact]
        public void Should_SqlServer_snapshot_has_default_config()
        {
            SqlServerPersistence.Get(Sys);
            var config = Sys.Settings.Config.GetConfig("akka.persistence.snapshot-store.sql-server");

            config.Should().NotBeNull();
            config.GetString("class").Should()
                .Be("Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer");
            config.GetString("plugin-dispatcher").Should().Be(Dispatchers.DefaultDispatcherId);
            config.GetString("connection-string").Should().BeEmpty();
            config.GetString("connection-string-name").Should().BeNullOrEmpty();
            config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
            config.GetString("schema-name").Should().Be("dbo");
            config.GetString("table-name").Should().Be("SnapshotStore");
            config.GetBoolean("auto-initialize").Should().BeFalse();
            config.GetString("read-isolation-level").Should().Be("unspecified");
            config.GetString("write-isolation-level").Should().Be("unspecified");
        }
        
        [Fact]
        public void SqlServer_SnapshotStoreSettings_default_should_contain_default_config()
        {
            var config = SqlServerPersistence.Get(Sys).DefaultSnapshotConfig;
            var settings = new SnapshotStoreSettings(config);

            // values should be correct
            settings.ConnectionString.Should().Be(string.Empty);
            settings.ConnectionStringName.Should().BeNullOrEmpty();
            settings.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
            settings.SchemaName.Should().Be("dbo");
            settings.TableName.Should().Be("SnapshotStore");
            settings.AutoInitialize.Should().BeFalse();
#pragma warning disable CS0618
            settings.DefaultSerializer.Should().BeNullOrEmpty();
#pragma warning restore CS0618
            settings.ReadIsolationLevel.Should().Be(IsolationLevel.Unspecified);
            settings.WriteIsolationLevel.Should().Be(IsolationLevel.Unspecified);
            settings.FullTableName.Should().Be($"{settings.SchemaName}.{settings.TableName}");

            // values should reflect configuration
            settings.ConnectionString.Should().Be(config.GetString("connection-string"));
            settings.ConnectionStringName.Should().Be(config.GetString("connection-string-name"));
            settings.ConnectionTimeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            settings.SchemaName.Should().Be(config.GetString("schema-name"));
            settings.TableName.Should().Be(config.GetString("table-name"));
            settings.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            settings.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));
            settings.AutoInitialize.Should().Be(config.GetBoolean("auto-initialize"));
#pragma warning disable CS0618
            settings.DefaultSerializer.Should().Be(config.GetString("serializer"));
#pragma warning restore CS0618
        }
        
        [Fact]
        public void Modified_SqlServer_SnapshotStoreSettings_should_contain_proper_config()
        {
            var fullConfig = ConfigurationFactory.ParseString(@"
akka.persistence.snapshot-store.sql-server 
{
	connection-string = ""a""
	connection-string-name = ""b""
	connection-timeout = 3s
	table-name = ""c""
	auto-initialize = on
	serializer = ""d""
    schema-name = ""e""
	sequential-access = on
	read-isolation-level = snapshot
	write-isolation-level = snapshot
}").WithFallback(SqlServerPersistence.DefaultConfiguration());
            
            var config = fullConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
            var settings = new SnapshotStoreSettings(config);
            var executorConfig = SqlServerSnapshotStore.CreateQueryConfiguration(config, settings);

            // values should be correct
            settings.ConnectionString.Should().Be("a");
            settings.ConnectionStringName.Should().Be("b");
            settings.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(3));
            settings.TableName.Should().Be("c");
#pragma warning disable CS0618
            settings.DefaultSerializer.Should().Be("d");
#pragma warning restore CS0618
            settings.SchemaName.Should().Be("e");
            settings.AutoInitialize.Should().BeTrue();
            settings.ReadIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            settings.WriteIsolationLevel.Should().Be(IsolationLevel.Snapshot);

            executorConfig.SnapshotTableName.Should().Be("c");
#pragma warning disable CS0618
            executorConfig.DefaultSerializer.Should().Be("d");
#pragma warning restore CS0618
            executorConfig.SchemaName.Should().Be("e");
            executorConfig.Timeout.Should().Be(TimeSpan.FromSeconds(3));
            executorConfig.ReadIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            executorConfig.WriteIsolationLevel.Should().Be(IsolationLevel.Snapshot);
            executorConfig.UseSequentialAccess.Should().BeTrue();
            
            // values should reflect configuration
            settings.ConnectionString.Should().Be(config.GetString("connection-string"));
            settings.ConnectionStringName.Should().Be(config.GetString("connection-string-name"));
            settings.ConnectionTimeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            settings.TableName.Should().Be(config.GetString("table-name"));
#pragma warning disable CS0618
            settings.DefaultSerializer.Should().Be(config.GetString("serializer"));
#pragma warning restore CS0618
            settings.SchemaName.Should().Be(config.GetString("schema-name"));
            settings.AutoInitialize.Should().Be(config.GetBoolean("auto-initialize"));
            settings.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            settings.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));

            executorConfig.SnapshotTableName.Should().Be(config.GetString("table-name"));
#pragma warning disable CS0618
            executorConfig.DefaultSerializer.Should().Be(config.GetString("serializer"));
#pragma warning restore CS0618
            executorConfig.SchemaName.Should().Be(config.GetString("schema-name"));
            executorConfig.Timeout.Should().Be(config.GetTimeSpan("connection-timeout"));
            executorConfig.ReadIsolationLevel.Should().Be(config.GetIsolationLevel("read-isolation-level"));
            executorConfig.WriteIsolationLevel.Should().Be(config.GetIsolationLevel("write-isolation-level"));
            executorConfig.UseSequentialAccess.Should().Be(config.GetBoolean("sequential-access"));
        }
    }
}