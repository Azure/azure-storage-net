//-----------------------------------------------------------------------
// <copyright file="FileHandle.cs" company="Microsoft">
//    Copyright 2019 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <summary>
//    Contains code for the FileHandle class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.File
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary> 
    /// Represents a range in a file. 
    /// </summary> 
    /// <summary>
    /// Provides attributes for files and directories.
    /// </summary>
    [Flags]
    public enum CloudFileNtfsAttributes
    {
        /// <summary>
        /// Clear all flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The file is read-only.
        /// </summary>
        ReadOnly = FileAttributes.ReadOnly,

        /// <summary>
        /// The file is hidden, and thus is not included in an ordinary directory listing.
        /// </summary>
        Hidden = FileAttributes.Hidden,

        /// <summary>
        /// The file is a system file. That is, the file is part of the operating system
        /// or is used exclusively by the operating system.
        /// </summary>
        System = FileAttributes.System,

        /// <summary>
        /// The file is a standard file that has no special attributes. This attribute is
        /// valid only if it is used alone.
        /// </summary>
        Normal = FileAttributes.Normal,

        /// <summary>
        /// The file is a directory.
        /// </summary>
        Directory = FileAttributes.Directory,

        /// <summary>
        /// The file is a candidate for backup or removal.
        /// </summary>
        Archive = FileAttributes.Archive,

        /// <summary>
        /// The file is temporary. A temporary file contains data that is needed while an
        /// application is executing but is not needed after the application is finished.
        /// File systems try to keep all the data in memory for quicker access rather than
        /// flushing the data back to mass storage. A temporary file should be deleted by
        /// the application as soon as it is no longer needed.
        /// </summary>
        Temporary = FileAttributes.Temporary,

        /// <summary>
        /// The file is offline. The data of the file is not immediately available.
        /// </summary>
        Offline = FileAttributes.Offline,

        /// <summary>
        /// The file will not be indexed by the operating system's content indexing service.
        /// </summary>
        NotContentIndexed = FileAttributes.NotContentIndexed,

        /// <summary>
        /// The file or directory is excluded from the data integrity scan. When this value
        /// is applied to a directory, by default, all new files and subdirectories within
        /// that directory are excluded from data integrity.
        /// </summary>
        NoScrubData = FileAttributes.NoScrubData
    }

    /// <summary>
    /// CloudFileNtfsAttributesHelper helper.
    /// </summary>
    internal class CloudFileNtfsAttributesHelper
    {
        private static Dictionary<CloudFileNtfsAttributes, string> directory = new Dictionary<CloudFileNtfsAttributes, string>()
        {
            { CloudFileNtfsAttributes.ReadOnly, "ReadOnly" },
            { CloudFileNtfsAttributes.Hidden, "Hidden" },
            { CloudFileNtfsAttributes.System, "System" },
            { CloudFileNtfsAttributes.Normal, "None" },
            { CloudFileNtfsAttributes.Directory, "Directory" },
            { CloudFileNtfsAttributes.Archive, "Archive" },
            { CloudFileNtfsAttributes.Temporary, "Temporary" },
            { CloudFileNtfsAttributes.Offline, "Offline" },
            { CloudFileNtfsAttributes.NotContentIndexed, "NotContentIndexed" },
            { CloudFileNtfsAttributes.NoScrubData, "NoScrubData" },
        };

        /// <summary>
        /// Converts a CloudFileNtfsAttributes to a string
        /// </summary>
        /// <param name="attributes"><see cref="CloudFileNtfsAttributes"/></param>
        /// <returns>string</returns>
        internal static string ToString(CloudFileNtfsAttributes attributes)
        {
            return string.Join("|", directory.Select(r =>
            {
                if (attributes.HasFlag(r.Key))
                {
                    return r.Value;
                }
                return null;
            }).Where(r => r != null));
        }

        /// <summary>
        /// Parses an attributes string to a <see cref="CloudFileNtfsAttributes"/>
        /// </summary>
        /// <param name="attributesString">string</param>
        /// <returns><see cref="CloudFileNtfsAttributes"/></returns>
        internal static CloudFileNtfsAttributes? ToAttributes(string attributesString)
        {
            if(attributesString == null)
            {
                return null;
            }
            var attributes = CloudFileNtfsAttributes.None;
            var splitString = attributesString.Split('|');
            foreach(var s in splitString)
            {
                var trimmed = s.Trim();

                attributes |= directory.FirstOrDefault(r => r.Value == trimmed).Key;
            }
            return attributes;
        }
    }
}