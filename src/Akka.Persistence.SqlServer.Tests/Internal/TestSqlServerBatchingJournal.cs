// -----------------------------------------------------------------------
// <copyright file="TestSqlServerBatchingJournal.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.SqlServer.Journal;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class TestSqlServerBatchingJournal : BatchingSqlServerJournal
    {
        public TestSqlServerBatchingJournal(Config config) : base(config)
        {
        }

        public TestSqlServerBatchingJournal(BatchingSqlServerJournalSetup setup) : base(setup)
        {
        }
    }
}