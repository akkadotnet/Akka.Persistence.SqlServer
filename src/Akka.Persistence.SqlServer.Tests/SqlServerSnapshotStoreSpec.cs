﻿//-----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStoreSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Snapshot;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests
{
    [Collection("SqlServerSpec")]
    public class SqlServerSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static Config InitConfig(SqlServerFixture fixture)
        {
            //need to make sure db is created before the tests start
            DbUtils.Initialize(fixture.ConnectionString);
            var specString = @"
                        akka.persistence {
                            publish-plugin-commands = on
                            snapshot-store {
                                plugin = ""akka.persistence.snapshot-store.sql-server""
                                sql-server {
                                    class = ""Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer""
                                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                                    table-name = SnapshotStore
                                    schema-name = dbo
                                    auto-initialize = on
                                    connection-string = """ + DbUtils.ConnectionString + @"""
                                }
                            }
                        }";

            return ConfigurationFactory.ParseString(specString);
        }

        public SqlServerSnapshotStoreSpec(ITestOutputHelper output, SqlServerFixture fixture)
            : base(InitConfig(fixture), "SqlServerSnapshotStoreSpec", output)
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}