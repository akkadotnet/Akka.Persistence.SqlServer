using System.Configuration;
using Akka.Configuration;
using Akka.Persistence.TestKit.Snapshot;

namespace Akka.Persistence.SqlServer.Tests
{
    public class SqlServerSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static readonly Config SpecConfig;

        static SqlServerSnapshotStoreSpec()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;

            SpecConfig = ConfigurationFactory.ParseString(@"
                        akka.persistence {
                            publish-plugin-commands = on
                            snapshot-store {
                                plugin = ""akka.persistence.snapshot-store.sql-server""
                                sql-server {
                                    class = ""Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer""
                                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                                    table-name = SnapshotStore
                                    schema-name = dbo
                                    auto-initialize = on
                                    connection-string = """+ connectionString + @"""
                                }
                            }
                        }");


            //need to make sure db is created before the tests start
            DbUtils.Initialize();
        }

        public SqlServerSnapshotStoreSpec()
            : base(SpecConfig, "SqlServerSnapshotStoreSpec")
        {
            DbUtils.Clean();
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}