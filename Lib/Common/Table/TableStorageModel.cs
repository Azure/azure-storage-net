// -----------------------------------------------------------------------------------------
// <copyright file="TableStorageModel.cs" company="Microsoft">
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
    using Microsoft.Data.OData;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;

    /// <summary>
    /// Represents a data model that will be used by OData for table transactions.
    /// </summary>
    public class TableStorageModel : EdmModel, IEdmModel
    {
        private string accountName;
        private readonly EdmEntityType tableType;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableStorageModel"/> class.
        /// </summary>
        internal TableStorageModel()
            : this(Constants.DefaultNamespaceName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableStorageModel"/> class.
        /// </summary>
        public TableStorageModel(string accountName)
        {
            // Add the special table type.  
            this.accountName = accountName;
            this.tableType = new EdmEntityType(this.accountName, Constants.EntitySetName);
            this.tableType.AddKeys(this.tableType.AddStructuralProperty(Constants.DefaultTableName, EdmPrimitiveTypeKind.String));
            this.AddElement(this.tableType);

            // Add the default entity container - the name and namespace should not matter since we look for default entity container. 
            TableStorageEntityContainer entityContainer = new TableStorageEntityContainer(this);
            this.AddElement(entityContainer);
            this.SetIsDefaultEntityContainer(entityContainer, true);
        }

        /// <summary>
        /// Searches for a type with the given name in this model and creates a new type if no such type exists.
        /// </summary>
        /// <param name="qualifiedName">The qualified name of the type being found.</param>
        /// <returns>The requested type, or the new type created if no such type exists.</returns>
        IEdmSchemaType IEdmModel.FindDeclaredType(string qualifiedName)
        {
            CommonUtility.AssertNotNullOrEmpty("qualifiedName", qualifiedName);

            if (qualifiedName.StartsWith(Constants.Edm, StringComparison.Ordinal))
            {
                // Primitive type, let the core model handle it.  
                return null;
            }

            IEdmSchemaType schemaType = FindDeclaredType(qualifiedName);
            if (schemaType == null)
            {
                string name;
                string namespaceName;
                SplitFullTypeName(qualifiedName, out name, out namespaceName);

                // If no type is found with the given name, assume it is an open entity type with the normal set of properties. 
                schemaType = CreateEntityType(namespaceName, name);
                this.AddElement(schemaType);
            }

            return schemaType;
        }

        /// <summary>
        /// Create a new type with the standard set of properties(PK, RK and TimeStamp).
        /// </summary>
        /// <param name="namespaceName">Namespace the entity belongs to.</param>
        /// <param name="name">Name of the entity.</param>
        /// <returns>The EdmEntityType created.</returns>
        internal static EdmEntityType CreateEntityType(string namespaceName, string name)
        {
            EdmEntityType entityType = new EdmEntityType(namespaceName, name, null, false, true);
            entityType.AddKeys(
                entityType.AddStructuralProperty("RowKey", EdmPrimitiveTypeKind.String),
                entityType.AddStructuralProperty("PartitionKey", EdmPrimitiveTypeKind.String));
                entityType.AddStructuralProperty("Timestamp", EdmCoreModel.Instance.GetDateTime(false), null, EdmConcurrencyMode.Fixed);  // We need this because we want to ensure that this property is not used for optimistic concurrency checks. 
            return entityType;
        }

        /// <summary>
        /// Searches for a type with the given name in this model. Returns true if such a type is found, otherwise returns false.
        /// </summary>
        /// <param name="qualifiedName">The qualified name of the type being found.</param>
        /// <returns><c>true</c> if the type is found; otherwise, <c>false</c>.</returns>
        internal bool IsKnownType(string qualifiedName)
        {
            return this.FindDeclaredType(qualifiedName) != null;
        }

        private static void SplitFullTypeName(string qualifiedName, out string name, out string namespaceName)
        {
            int lastPeriod = qualifiedName.LastIndexOf('.');
            name = qualifiedName.Substring(lastPeriod + 1);
            namespaceName = qualifiedName.Substring(0, lastPeriod);
        }

        internal string AccountName
        {
            get
            {
                return this.accountName;
            }
        }

        internal EdmEntityType TableType
        {
            get
            {
                return this.tableType;
            }
        }
    }
}
