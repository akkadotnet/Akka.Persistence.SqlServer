﻿// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerEventsByPersistenceIdSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerEventsByPersistenceIdSpec : EventsByPersistenceIdSpec
    {
        public BatchingSqlServerEventsByPersistenceIdSpec(ITestOutputHelper output, SqlServerFixture fixture)
            : base(InitConfig(fixture), nameof(BatchingSqlServerEventsByPersistenceIdSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        }

        private static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);

            var conf = ConfigurationFactory.ParseString($@"
            akka.loglevel = DEBUG
            akka.test.single-expect-default = 10s
            akka.persistence.journal.plugin = ""akka.persistence.journal.sql-server""
            akka.persistence.journal.sql-server {{
                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                table-name = EventJournal
                schema-name = dbo
                auto-initialize = on
                connection-string = ""{DbUtils.ConnectionString}""
                refresh-interval = 1s
            }}");

            return conf.WithFallback(SqlReadJournal.DefaultConfiguration());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}