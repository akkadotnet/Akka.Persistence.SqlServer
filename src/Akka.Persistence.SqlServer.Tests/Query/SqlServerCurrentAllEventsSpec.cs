// -----------------------------------------------------------------------
// <copyright file="SqlServerCurrentAllEventsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Query
{
    [Collection("SqlServerSpec")]
    public class SqlServerCurrentAllEventsSpec : CurrentAllEventsSpec, IDisposable
    {
        public SqlServerCurrentAllEventsSpec(ITestOutputHelper output, SqlServerFixture fixture) : base(
            InitConfig(fixture),
            nameof(SqlServerCurrentAllEventsSpec), output)
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
                        event-adapters {{
                          color-tagger  = ""Akka.Persistence.TCK.Query.ColorFruitTagger, Akka.Persistence.TCK""
                        }}
                        event-adapter-bindings = {{
                          ""System.String"" = color-tagger
                        }}
                        class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                        table-name = EventJournal
                        schema-name = dbo
                        auto-initialize = on
                        connection-string = ""{DbUtils.ConnectionString}""
                    }}")
                .WithFallback(SqlReadJournal.DefaultConfiguration());
        }

        protected void Dispose(bool disposing)
        {
            DbUtils.Clean();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}