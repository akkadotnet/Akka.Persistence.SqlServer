using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Akka.Persistence.SqlServer.Tests
{
    public static class DbUtils
    {
        public static string ConnectionString;
        
        public static void Initialize()
        {
            ConnectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            var connectionBuilder = new SqlConnectionStringBuilder(ConnectionString);

            //connect to postgres database to create a new database
            var databaseName = connectionBuilder.DataSource;
            connectionBuilder.DataSource = "master";
            ConnectionString = connectionBuilder.ToString();

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = string.Format(@"
                        IF db_id('{0}') IS NOT NULL
                            BEGIN
                                CREATE DATABASE {0}
                            END
                        ELSE
                            BEGIN
                                DROP DATABASE {0}
                            END
                    ", databaseName);
                    cmd.Connection = conn;

                    var result = cmd.ExecuteScalar();
                }
            }
        }

        public static void Clean()
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand())
            {
                conn.Open();
                cmd.CommandText = @"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'EventJournal') BEGIN DELETE FROM dbo.EventJournal END;
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SnapshotStore') BEGIN DELETE FROM dbo.SnapshotStore END";
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
        }
    }
}