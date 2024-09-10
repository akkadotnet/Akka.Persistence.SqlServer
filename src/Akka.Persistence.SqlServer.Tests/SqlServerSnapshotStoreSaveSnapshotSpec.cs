// -----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStoreSaveSnapshotSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.TCK.Serialization;
using Akka.Persistence.TCK.Snapshot;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests;

[Collection("SqlServerSpec")]
public class SqlServerSnapshotStoreSaveSnapshotSpec: SnapshotStoreSaveSnapshotSpec
{
    public SqlServerSnapshotStoreSaveSnapshotSpec(ITestOutputHelper output, SqlServerFixture fixture) 
        : base(InitConfig(fixture), nameof(SqlServerSnapshotStoreSaveSnapshotSpec), output)
    {
            
    }
    
    private static Config InitConfig(SqlServerFixture fixture)
    {
        //need to make sure db is created before the tests start
        DbUtils.Initialize(fixture.ConnectionString);
        var specString = $$"""
                           akka.persistence {
                               publish-plugin-commands = on
                               snapshot-store {
                                   plugin = "akka.persistence.snapshot-store.sql-server"
                                   sql-server {
                                       class = "Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer"
                                       plugin-dispatcher = "akka.actor.default-dispatcher"
                                       table-name = SnapshotStore
                                       schema-name = dbo
                                       auto-initialize = on
                                       connection-string = "{{DbUtils.ConnectionString}}"
                                   }
                               }
                           }
                           """;

        return ConfigurationFactory.ParseString(specString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DbUtils.Clean();
    }
    
    [Fact(DisplayName = "Multiple SaveSnapshot invocation with default metadata should not throw")]
    public async Task MultipleSnapshotsWithDefaultMetadata()
    {
        var persistence = Persistence.Instance.Apply(Sys);
        var snapshotStore = persistence.SnapshotStoreFor(null);
        var snap = new TestPayload(SenderProbe.Ref);
        
        var now = DateTime.UtcNow;
        var metadata = new SnapshotMetadata(PersistenceId, 0, DateTime.MinValue);
        snapshotStore.Tell(new SaveSnapshot(metadata, snap), SenderProbe);
        var success = await SenderProbe.ExpectMsgAsync<SaveSnapshotSuccess>(10.Minutes());
        success.Metadata.PersistenceId.Should().Be(metadata.PersistenceId);
        success.Metadata.Timestamp.Should().BeAfter(now);
        success.Metadata.SequenceNr.Should().Be(metadata.SequenceNr);
        
        snapshotStore.Tell(new SaveSnapshot(metadata, snap), SenderProbe);
        success = await SenderProbe.ExpectMsgAsync<SaveSnapshotSuccess>();
        success.Metadata.PersistenceId.Should().Be(metadata.PersistenceId);
        success.Metadata.Timestamp.Should().BeAfter(now);
        success.Metadata.SequenceNr.Should().Be(metadata.SequenceNr);
    }  
}