// -----------------------------------------------------------------------------------------
// <copyright file="EntityPropertyConverterOptions.cs" company="Microsoft">
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
    /// <summary>
    /// Represents a set of options that may be specified for flattening and re-composition of objects by the Entity property converter.
    /// </summary>
    public class EntityPropertyConverterOptions
    {
        private string propertyNameDelimiter = EntityPropertyConverter.DefaultPropertyNameDelimiter;
        
        private bool ignoreAdditionalProperties = false;

        /// <summary>
        /// Gets or sets the delimiter that will be used to separate names of nested properties.
        /// </summary>
        public string PropertyNameDelimiter
        {
            get
            {
                return propertyNameDelimiter;
            }

            set
            {
                propertyNameDelimiter = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the switch that will allow additional properties to be ignored when converting back.
        /// </summary>
        public bool IgnoreAdditionalProperties
        {
            get
            {
                return ignoreAdditionalProperties;
            }
            
            set
            {
                ignoreAdditionalProperties = value;
            }
        }
    }
}
