// -----------------------------------------------------------------------
// <copyright file="BatchingSqlJournalFailureSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------



/*
namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlJournalFailureSpec : JournalSpec
    {
        private static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);
            var specString = $@"
akka {{
    loglevel = INFO
    stdout-loglevel = INFO
    actor.debug {{
        receive = off
        autoreceive = off
        lifecycle = off
        fsm = off
        event-stream = off
        unhandled = off
        router-misconfiguration = off
    }}
    persistence {{
        publish-plugin-commands = on
        journal {{
            plugin = ""akka.persistence.journal.sql-server""
            sql-server {{
                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                table-name = EventJournal
                schema-name = dbo
                auto-initialize = on
                connection-string = ""{DbUtils.ConnectionString}""

				circuit-breaker {{
				    max-failures = 1
				    call-timeout = 3s
				    reset-timeout = 20s
			    }}

                replay-filter {{
                    debug: true
                }}
            }}
        }}
    }}
}}";
            return ConfigurationFactory.ParseString(specString);
        }

        private readonly TestProbe _probe;
        private readonly SqlServerFixture _fixture;

        public BatchingSqlJournalFailureSpec(ITestOutputHelper output, SqlServerFixture fixture) 
            : base(InitConfig(fixture), "BugTest", output)
        {
            _probe = CreateTestProbe();
            _fixture = fixture;
        }

        [Fact]
        public async Task Bug4265_Persistent_actor_stuck_with_RecoveryTimedOutException_after_circuit_breaker_opens()
        {
            var timeout = TimeSpan.FromHours(1);

            Output.WriteLine("------------------------------ Setup");

            var actor = ActorOf(() => new PersistActor(_probe));
            Watch(actor);

            for (var i = 1; i < 6; ++i)
            {
                actor.Tell(new PersistActor.WriteJournal(i.ToString()), TestActor);
            }
            // Journal should contain 1, 2, 3, 4, 5
            await Task.Delay(3000);

            Output.WriteLine("------------------------------ Stop actor");
            await actor.GracefulStop(TimeSpan.FromSeconds(3));
            ExpectTerminated(actor, timeout);
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Make snapshot fail before succeeding
            if (!await _fixture.StopContainer())
            {
                throw new Exception("Failed to stop the docker container.");
            }
            await Task.Delay(5000);

            Output.WriteLine("------------------------------ Trip circuit breaker");
            // Trigger recovery to trip the circuit breaker
            for (var i = 0; i < 2; ++i)
            {
                actor = ActorOf(() => new PersistActor(_probe));
                Watch(actor);
                ExpectTerminated(actor);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            Output.WriteLine("------------------------------ Actor should die due to circuit breaker");
            // This actor should die because of circuit breaker is failing fast
            actor = ActorOf(() => new PersistActor(_probe));
            Watch(actor);
            ExpectTerminated(actor);
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Start database back 
            if (!await _fixture.StartContainer())
            {
                throw new Exception("Failed to restart the docker container.");
            }
            await Task.Delay(10000);

            // Circuit breaker reset timer is set to 10 seconds, wait a bit until it recover
            await Task.Delay(TimeSpan.FromSeconds(10));

            Output.WriteLine("------------------------------ Actor is back up after circuit breaker reset");
            // This actor should survive
            actor = ActorOf(() => new PersistActor(_probe));
            Watch(actor);

            // actor should not die
            ExpectNoMsg(TimeSpan.FromSeconds(10));
        }
    }

    internal class PersistActor : UntypedPersistentActor
    {
        public event EventHandler<SnapshotOffer> SnapshotOffered;
        public event EventHandler<RecoveryCompleted> RecoveryCompleted;

        private readonly ILoggingAdapter _log;

        public PersistActor(IActorRef probe)
        {
            _probe = probe;
            _log = Context.GetLogger();
        }

        private readonly IActorRef _probe;

        public override string PersistenceId => "foo";

        protected override void OnCommand(object message)
        {
            switch (message)
            {
                case WriteJournal w:
                    Persist(w.Data, msg => _probe.Tell(msg));
                    break;

                case SaveSnapshotMessage s:
                    SaveSnapshot(s.Data);
                    return;

                case "load":
                    LoadSnapshot(PersistenceId, SnapshotSelectionCriteria.Latest, 3);
                    break;

                case SaveSnapshotSuccess _:
                case SaveSnapshotFailure _:
                case DeleteSnapshotSuccess _:
                case DeleteSnapshotFailure _:
                case DeleteSnapshotsSuccess _:
                case DeleteSnapshotsFailure _:
                    _probe.Tell(message);
                    return;

                default:
                    return;
            }
        }

        protected override void OnRecover(object message)
        {
            _log.Debug($"[OnRecover] Message: {message}");
            switch (message)
            {
                case SnapshotOffer offer:
                    SnapshotOffered?.Invoke(this, offer);
                    _probe.Tell(offer);
                    break;
                case RecoveryCompleted complete:
                    RecoveryCompleted?.Invoke(this, complete);
                    _probe.Tell(complete);
                    break;
            }
        }

        protected override void OnRecoveryFailure(Exception reason, object message = null)
        {
            _probe.Tell(new RecoveryFailure(reason, message));
            base.OnRecoveryFailure(reason, message);
        }

        protected override void OnReplaySuccess()
        {
            _probe.Tell("ReplaySuccess");
            base.OnReplaySuccess();
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            _probe.Tell("failure");
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            _probe.Tell("rejected");
            base.OnPersistRejected(cause, @event, sequenceNr);
        }

        public class WriteJournal
        {
            public WriteJournal(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        public class SaveSnapshotMessage
        {
            public SaveSnapshotMessage(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        public class RecoveryFailure
        {
            public RecoveryFailure(Exception reason, object message)
            {
                Reason = reason;
                Message = message;
            }

            public Exception Reason { get; }
            public object Message { get; }
        }
    }
}
*/