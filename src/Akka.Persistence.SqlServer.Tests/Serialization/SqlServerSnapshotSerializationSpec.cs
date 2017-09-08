using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.TCK.Serialization;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Serialization
{
    public class SqlServerSnapshotSerializationSpec : SnapshotStoreSerializationSpec
    {
        private static readonly Config SpecConfig;

        static SqlServerSnapshotSerializationSpec()
        {
            DbUtils.Initialize();
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
            SpecConfig = ConfigurationFactory.ParseString(specString);
        }

        public SqlServerSnapshotSerializationSpec(ITestOutputHelper output) : base(SpecConfig, "SqlServerSnapshotSerializationSpec", output)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}
