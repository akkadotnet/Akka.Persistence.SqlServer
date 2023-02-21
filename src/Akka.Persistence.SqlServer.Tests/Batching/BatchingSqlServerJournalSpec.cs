// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerJournalSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Journal;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerJournalSpec : JournalSpec
    {
        public BatchingSqlServerJournalSpec(ITestOutputHelper output, SqlServerFixture fixture)
            : base(InitConfig(fixture), nameof(BatchingSqlServerJournalSpec), output)
        {
            Initialize();
        }

        // TODO: hack. Replace when https://github.com/akkadotnet/akka.net/issues/3811
        protected override bool SupportsSerialization => false;

        private static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);
            var specString = @"
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.sql-server""
                            sql-server {
                                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = EventJournal
                                schema-name = dbo
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