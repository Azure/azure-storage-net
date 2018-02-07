﻿// -----------------------------------------------------------------------------------------
// <copyright file="TypeInfoExtension.cs" company="Microsoft">
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

using System.Linq;

namespace Microsoft.Azure.Storage.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal static class TypeInfoExtension
    {
        public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo typeInfo)
        {
            IList<PropertyInfo> propertyList = new List<PropertyInfo>();

            while (typeInfo != null)
            {
                foreach (var declaredProperty in typeInfo.DeclaredProperties.Where(declaredProperty => propertyList.All(x => x.Name != declaredProperty.Name)))
                {
                    propertyList.Add(declaredProperty);
                }

                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }

            return propertyList;
        }
    }
}
