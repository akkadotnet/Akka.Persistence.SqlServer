// -----------------------------------------------------------------------
// <copyright file="SqlServerConnectionFailureSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Sql.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests;

[Collection("SqlServerSpec")]
public class SqlServerConnectionFailureSpec: SqlJournalConnectionFailureSpec
{
    public SqlServerConnectionFailureSpec(ITestOutputHelper output, SqlServerFixture fixture) 
        : base(InitConfig(fixture, DefaultInvalidConnectionString), output)
    {
            
    }
    
    private static Config InitConfig(SqlServerFixture fixture, string connectionString)
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
                                       connection-string = "{{connectionString}}"
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


}