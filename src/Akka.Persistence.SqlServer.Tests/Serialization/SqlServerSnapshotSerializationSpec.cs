// -----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotSerializationSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Serialization;
using Xunit;
using Xunit.Abstractions;
using Hocon;

namespace Akka.Persistence.SqlServer.Tests.Serialization
{
    [Collection("SqlServerSpec")]
    public class SqlServerSnapshotSerializationSpec : SnapshotStoreSerializationSpec
    {
        public SqlServerSnapshotSerializationSpec(ITestOutputHelper output, SqlServerFixture fixture) : base(
            InitConfig(fixture), "SqlServerSnapshotSerializationSpec", output)
        {
        }

        private static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);
            var specString = @"
                akka.persistence {
                    publish-plugin-commands = on
                    snapshot-store {
                        plugin = ""akka.persistence.snapshot-store.sql-server""
                        sql-server {
                            class = ""Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            table-name = SnapshotStore
                            auto-initialize = on
                            connection-string = """ + DbUtils.ConnectionString + @"""
                        }
                    }
                }";
            return ConfigurationFactory.ParseString(specString);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}