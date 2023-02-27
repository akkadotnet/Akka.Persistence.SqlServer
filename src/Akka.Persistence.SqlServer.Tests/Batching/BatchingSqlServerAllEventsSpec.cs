// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerAllEventsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerAllEventsSpec : AllEventsSpec
    {
        public BatchingSqlServerAllEventsSpec(ITestOutputHelper output, SqlServerFixture fixture) : base(
            InitConfig(fixture),
            nameof(BatchingSqlServerAllEventsSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        }

        public static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);
            return ConfigurationFactory.ParseString($@"
                    akka.loglevel = INFO
                    akka.test.single-expect-default = 10s
                    akka.persistence.journal.plugin = ""akka.persistence.journal.sql-server""
                    akka.persistence.query.journal.sql.refresh-interval = 1s
                    akka.persistence.journal.sql-server {{
                        class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                        schema-name = dbo
                        auto-initialize = on
                        connection-string = ""{DbUtils.ConnectionString}""
                    }}")
                .WithFallback(SqlReadJournal.DefaultConfiguration());
        }

        public override Task DisposeAsync()
        {
            GC.SuppressFinalize(this);
            DbUtils.Clean();
            return base.DisposeAsync();
        }
    }
}