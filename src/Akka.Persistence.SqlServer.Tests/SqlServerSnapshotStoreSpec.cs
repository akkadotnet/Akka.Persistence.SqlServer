using System.Configuration;
using Akka.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests
{
    public class SqlServerSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static readonly Config SpecConfig;

        static SqlServerSnapshotStoreSpec()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString.Replace(@"\", "\\");

            var specString = @"
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
                                    connection-string = ""Data Source=localhost\\SQLEXPRESS;Database=akka_persistence_tests;User Id=akkadotnet;Password=akkadotnet;""
                                }
                            }
                        }";

            SpecConfig = ConfigurationFactory.ParseString(specString);


            //need to make sure db is created before the tests start
            DbUtils.Initialize();
        }

        public SqlServerSnapshotStoreSpec(ITestOutputHelper output)
            : base(SpecConfig, "SqlServerSnapshotStoreSpec", output)
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