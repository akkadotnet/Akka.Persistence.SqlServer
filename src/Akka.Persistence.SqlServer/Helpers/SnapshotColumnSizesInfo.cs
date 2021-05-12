// //-----------------------------------------------------------------------
// // <copyright file="SnapshotColumnSizesInfo.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

namespace Akka.Persistence.SqlServer.Helpers
{
    /// <summary>
    /// Represents information about SQL SnapshotStore Column sizes
    /// </summary>
    internal class SnapshotColumnSizesInfo
    {
        public SnapshotColumnSizesInfo(int persistenceIdColumnSize, int manifestColumnSize)
        {
            PersistenceIdColumnSize = persistenceIdColumnSize;
            ManifestColumnSize = manifestColumnSize;
        }

        /// <summary>
        /// Size of PersistenceId column
        /// </summary>
        public int PersistenceIdColumnSize { get; }
        /// <summary>
        /// Size of manifest column
        /// </summary>
        public int ManifestColumnSize { get; }
    }
}