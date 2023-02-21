// -----------------------------------------------------------------------
// <copyright file="SqlServerJournalSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.TCK.Journal;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests
{
    [Collection("SqlServerSpec")]
    public class SqlServerJournalSpec : JournalSpec
    {
        public SqlServerJournalSpec(ITestOutputHelper output, SqlServerFixture fixture)
            : base(InitConfig(fixture), "SqlServerJournalSpec", output)
        {
            Initialize();
        }

        // TODO: hack. Replace when https://github.com/akkadotnet/akka.net/issues/3811
        protected override bool SupportsSerialization => false;

        private static Config InitConfig(SqlServerFixture fixture)
        {
            //need to make sure db is created before the tests start
            DbUtils.Initialize(fixture.ConnectionString);
            var specString = $@"
                    akka.persistence {{
                        publish-plugin-commands = on
                        journal {{
                            plugin = ""akka.persistence.journal.sql-server""
                            sql-server {{
                                class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = EventJournal
                                schema-name = dbo
                                auto-initialize = on
                                connection-string = ""{DbUtils.ConnectionString}""
                            }}
                        }}
                    }}";

            return ConfigurationFactory.ParseString(specString);
        }

        protected override void Dispose(bool disposing)
        {
            
            DbUtils.Clean();
        }

        [Fact]
        public void Journal_should_not_reset_HighestSequenceNr_after_journal_cleanup1()
        {
            var _receiverProbe = CreateTestProbe();
            Journal.Tell(new ReplayMessages(0, long.MaxValue, long.MaxValue, Pid, _receiverProbe.Ref));
            for (var i = 1; i <= 5; i++) _receiverProbe.ExpectMsg<ReplayedMessage>(m => IsReplayedMessage(m, i));
            _receiverProbe.ExpectMsg<RecoverySuccess>(m => m.HighestSequenceNr == 5L);

            Journal.Tell(new DeleteMessagesTo(Pid, long.MaxValue, _receiverProbe.Ref));
            _receiverProbe.ExpectMsg<DeleteMessagesSuccess>(m => m.ToSequenceNr == long.MaxValue);

            Journal.Tell(new ReplayMessages(0, long.MaxValue, long.MaxValue, Pid, _receiverProbe.Ref));
            _receiverProbe.ExpectMsg<RecoverySuccess>(m => m.HighestSequenceNr == 5L);
        }
    }
}