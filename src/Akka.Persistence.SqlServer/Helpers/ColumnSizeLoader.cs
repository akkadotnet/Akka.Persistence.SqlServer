// -----------------------------------------------------------------------
// <copyright file="ColumnSizeLoader.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Helpers
{
    /// <summary>
    ///     Helper class that can load column sizes from SqlServer table
    /// </summary>
    internal static class ColumnSizeLoader
    {
        /// <summary>
        ///     Loads column sizes for Journal
        /// </summary>
        /// <returns></returns>
        public static JournalColumnSizesInfo LoadJournalColumnSizes(QueryConfiguration conventions,
            DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"SELECT * FROM {conventions.FullJournalTableName}";

                // start reading - no need to load the table's content
                using (var reader = command.ExecuteReader())
                {
                    // load columns metadata
                    var results = LoadSchemaTableInfo(reader);

                    return new JournalColumnSizesInfo(
                        (int)results.First(r => r["ColumnName"].ToString() == conventions.PersistenceIdColumnName)[
                            "ColumnSize"],
                        (int)results.First(r => r["ColumnName"].ToString() == conventions.TagsColumnName)["ColumnSize"],
                        (int)results.First(r => r["ColumnName"].ToString() == conventions.ManifestColumnName)[
                            "ColumnSize"]
                    );
                }
            }
        }

        public static SnapshotColumnSizesInfo LoadSnapshotColumnSizes(
            Sql.Common.Snapshot.QueryConfiguration conventions, DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"SELECT * FROM {conventions.FullSnapshotTableName}";

                // start reading - no need to load the table's content
                using (var reader = command.ExecuteReader())
                {
                    // load columns metadata
                    var results = LoadSchemaTableInfo(reader);

                    return new SnapshotColumnSizesInfo(
                        (int)results.First(r => r["ColumnName"].ToString() == conventions.PersistenceIdColumnName)[
                            "ColumnSize"],
                        (int)results.First(r => r["ColumnName"].ToString() == conventions.ManifestColumnName)[
                            "ColumnSize"]
                    );
                }
            }
        }

        private static List<Dictionary<string, object>> LoadSchemaTableInfo(DbDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();

            // iterate through the table schema and extract metadata
            var schemaTable = reader.GetSchemaTable();
            foreach (DataRow row in schemaTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in schemaTable.Columns) dict.Add(col.ColumnName, row[col.Ordinal]);
                results.Add(dict);
            }

            return results;
        }
    }
}