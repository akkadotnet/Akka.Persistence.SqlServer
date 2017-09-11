//-----------------------------------------------------------------------
// <copyright file="BatchingSqlServerPersistenceIdsSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.Sql.TestKit;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerPersistenceIdsSpec : Akka.Persistence.TCK.Query.PersistenceIdsSpec
    {
        private static readonly Config SpecConfig;

        static BatchingSqlServerPersistenceIdsSpec()
        {
            DbUtils.Initialize();

            var conf = ConfigurationFactory.ParseString($@"
            akka.loglevel = INFO
            akka.test.single-expect-default = 10s
            akka.persistence.journal.plugin = ""akka.persistence.journal.sql-server""
            akka.persistence.journal.sql-server {{
                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                table-name = EventJournal
                schema-name = dbo
                auto-initialize = on
                connection-string = """ + DbUtils.ConnectionString + @"""
                refresh-interval = 1s
            }}");

            SpecConfig = conf.WithFallback(SqlReadJournal.DefaultConfiguration());
        }

        public BatchingSqlServerPersistenceIdsSpec(ITestOutputHelper output) 
            : base(SpecConfig, nameof(BatchingSqlServerPersistenceIdsSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}