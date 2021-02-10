// //-----------------------------------------------------------------------
// // <copyright file="ColumnSizesInfo.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

namespace Akka.Persistence.SqlServer.Helpers
{
    /// <summary>
    /// Represents information about SQL Column sizes
    /// </summary>
    public class ColumnSizesInfo
    {
        public ColumnSizesInfo(int persistenceIdColumnSize, int tagsColumnSize)
        {
            PersistenceIdColumnSize = persistenceIdColumnSize;
            TagsColumnSize = tagsColumnSize;
        }

        /// <summary>
        /// Size of PersistenceId column
        /// </summary>
        public int PersistenceIdColumnSize { get; }
        /// <summary>
        /// Size of Tags column
        /// </summary>
        public int TagsColumnSize { get; }
    }
}