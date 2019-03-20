//----------------------------------------------------------------------- 
// <copyright file="CloseFileHandleResultSegment.cs" company="Microsoft"> 
//    Copyright 2018 Microsoft Corporation 
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
//----------------------------------------------------------------------- 

namespace Microsoft.Azure.Storage.File
{
    /// <summary> 
    /// Represents a close file handles result, with continuation information for pagination scenarios. 
    /// </summary> 
    public class CloseFileHandleResultSegment
    {
        /// <summary> 
        /// The number of handles closed on this request. 
        /// </summary> 
        public int NumHandlesClosed { get; internal set; }

        /// <summary> 
        /// Gets the continuation token used to continue the close operation, should it not have completed within one request. 
        /// </summary> 
        /// <value>A <see cref="FileContinuationToken"/> object.</value> 
        public FileContinuationToken ContinuationToken { get; internal set; }
    }
}