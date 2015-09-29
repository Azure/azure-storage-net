//-----------------------------------------------------------------------
// <copyright file="TableStorageEntityContainer.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.Data.Edm;
    using Microsoft.Data.Edm.Library;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    /// <summary>
    /// Represents the default EDM entity container for table storage.
    /// </summary>
    internal class TableStorageEntityContainer : EdmEntityContainer
    {
        private readonly TableStorageModel model;
        private readonly IEdmEntitySet tablesSet;
 
        /// <summary>
        /// Initializes a new instance of the EdmEntityContainer class and sets the model and entity set. The name and namespace should not matter since we look for default entity container.
        /// </summary>
        /// <param name="model">Sets the data model that will be used for table transactions. </param>
        public TableStorageEntityContainer(TableStorageModel model)
            : base(Constants.EdmEntityTypeNamespaceName, Constants.EdmEntityTypeName)
        {
            this.model = model;
            this.tablesSet = this.AddEntitySet(Constants.EntitySetName, this.model.TableType);
        }

        /// <summary>
        /// Searches for an entity set with the given name in this entity container and creates a new set if no such set exists.
        /// </summary>
        /// <param name="setName">The name of the element being found.</param>
        /// <returns>The requested element, or the new element created if no such element exists.</returns>
        public override IEdmEntitySet FindEntitySet(string setName)
        {
            // Check to make sure the set name is not a primitive type or collection type before you create a dynamic set. 
            if (this.model.IsKnownType(setName))
            {
                return null;
            }

            if (setName == Constants.EntitySetName)
            {
                return this.tablesSet;
            }

            // See if a set has already been created for this name.  
            IEdmEntitySet set = base.FindEntitySet(setName);
            if (set == null)
            {
                // If not, create the set dynamically, and assume the type name is based on the table/set name.  
                string serverTypeName = this.InferServerTypeNameFromTableName(setName);
                set = new EdmEntitySet(this, setName, (IEdmEntityType)((IEdmModel)this.model).FindDeclaredType(serverTypeName));
                this.AddElement(set);
            }

            return set;
        }

        private string InferServerTypeNameFromTableName(string setName)
        {
            return this.model.AccountName + '.' + setName;
        }
    } 
}
