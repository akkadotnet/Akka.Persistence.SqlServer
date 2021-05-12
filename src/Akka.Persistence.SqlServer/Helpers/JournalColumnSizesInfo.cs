// //-----------------------------------------------------------------------
// // <copyright file="ColumnSizesInfo.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

namespace Akka.Persistence.SqlServer.Helpers
{
    /// <summary>
    /// Represents information about SQL Journal Column sizes
    /// </summary>
    internal class JournalColumnSizesInfo
    {
        public JournalColumnSizesInfo(int persistenceIdColumnSize, int tagsColumnSize, int manifestColumnSize)
        {
            PersistenceIdColumnSize = persistenceIdColumnSize;
            TagsColumnSize = tagsColumnSize;
            ManifestColumnSize = manifestColumnSize;
        }

        /// <summary>
        /// Size of PersistenceId column
        /// </summary>
        public int PersistenceIdColumnSize { get; }
        /// <summary>
        /// Size of Tags column
        /// </summary>
        public int TagsColumnSize { get; }
        /// <summary>
        /// Size of manifest column
        /// </summary>
        public int ManifestColumnSize { get; }
    }
}