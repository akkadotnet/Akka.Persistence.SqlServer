using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.SqlServer.Journal;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class TestSqlServerBatchingJournal: BatchingSqlServerJournal
    {


        public TestSqlServerBatchingJournal(Config config) : base(config)
        {
        }

        public TestSqlServerBatchingJournal(BatchingSqlServerJournalSetup setup) : base(setup)
        {
        }

        
    }
}
