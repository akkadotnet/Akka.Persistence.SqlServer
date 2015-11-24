using System.Data.Common;
using System.Data.SqlClient;
using Akka.Persistence.Sql.Common;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.SqlServer.Snapshot
{
    /// <summary>
    /// Actor used for storing incoming snapshots into persistent snapshot store backed by SQL Server database.
    /// </summary>
    public class SqlServerSnapshotStore : SqlSnapshotStore
    {
        private readonly SqlServerPersistence  _extension;

        public SqlServerSnapshotStore()
        {
            _extension = SqlServerPersistence.Get(Context.System);
            QueryBuilder = new SqlServerSnapshotQueryBuilder(_extension.SnapshotSettings);
            QueryMapper = new SqlServerQueryMapper(Context.System.Serialization);
        }


        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override SnapshotStoreSettings Settings { get { return _extension.SnapshotSettings; } }
    }
}