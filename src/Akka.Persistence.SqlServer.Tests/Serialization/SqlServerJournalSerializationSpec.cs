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
    public class SqlServerJournalSerializationSpec : JournalSerializationSpec
    {
        private static readonly Config SpecConfig;

        static SqlServerJournalSerializationSpec()
        {
            DbUtils.Initialize();
            var specString = @"
                akka.persistence {
                    publish-plugin-commands = on
                    journal {
                        plugin = ""akka.persistence.journal.sql-server""
                        sql-server {
                            event-adapters {
                                custom-adapter = ""Akka.Persistence.TCK.Serialization.TestJournal+MyWriteAdapter, Akka.Persistence.TCK""
                            }
                            event-adapter-bindings = {
                                ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload3, Akka.Persistence.TCK"" = custom-adapter
                            }    
                            class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            table-name = EventJournal
                            metadata-table-name = Metadata
                            schema-name = dbo
                            auto-initialize = on
                            connection-string = """ + DbUtils.ConnectionString + @"""
                        }
                    }
                }";

            SpecConfig = ConfigurationFactory.ParseString(specString);
        }

        public SqlServerJournalSerializationSpec(ITestOutputHelper output) : base(SpecConfig, "SqlServerJournalSerializationSpec", output)
        {            
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }

        [Fact(Skip = "Sql plugin does not support EventAdapter.Manifest")]
        public override void Journal_should_serialize_Persistent_with_EventAdapter_manifest()
        {
        }
    }
}
