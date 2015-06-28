using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Akka.Persistence.SqlServer.Tests
{
    public static class DbUtils
    {
       
        public static void Initialize()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            var connectionBuilder = new SqlConnectionStringBuilder(connectionString);

            //connect to postgres database to create a new database
            var databaseName = connectionBuilder.InitialCatalog;
            connectionBuilder.InitialCatalog = "master";
            connectionString = connectionBuilder.ToString();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = string.Format(@"
                        IF db_id('{0}') IS NULL
                            BEGIN
                                CREATE DATABASE {0}
                            END
                            
                    ", databaseName);
                    cmd.Connection = conn;

                    var result = cmd.ExecuteScalar();
                }
            }
        }

        public static void Clean()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
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