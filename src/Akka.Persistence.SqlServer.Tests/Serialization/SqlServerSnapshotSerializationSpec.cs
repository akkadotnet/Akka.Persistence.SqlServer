using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.TCK.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Serialization
{
    [Collection("SqlServerSpec")]
    public class SqlServerSnapshotSerializationSpec : SnapshotStoreSerializationSpec
    {
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

        public SqlServerSnapshotSerializationSpec(ITestOutputHelper output, SqlServerFixture fixture) : base(InitConfig(fixture), "SqlServerSnapshotSerializationSpec", output)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}
