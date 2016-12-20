// -----------------------------------------------------------------------------------------
// <copyright file="EntityPropertyConverter.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

#if NETCORE
    using Microsoft.WindowsAzure.Storage.Extensions;
#endif

    /// <summary>
    /// EntityPropertyConverter class.
    /// </summary>
    public static class EntityPropertyConverter
    {
        /// <summary>
        /// The property delimiter.
        /// </summary>
        public const string DefaultPropertyNameDelimiter = "_";

        /// <summary>
        /// Traverses object graph, flattens and converts all nested (and not nested) properties to EntityProperties, stores them in the property dictionary.
        /// The keys are constructed by appending the names of the properties visited during pre-order depth first traversal from root to each end node property delimited by '_'.
        /// Allows complex objects to be stored in persistent storage systems or passed between web services in a generic way.
        /// </summary>
        /// <param name="root">The object to flatten and convert.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The result containing <see cref="IDictionary{TKey,TValue}"/> of <see cref="EntityProperty"/> objects for all properties of the flattened root object.</returns>
        public static Dictionary<string, EntityProperty> Flatten(object root, OperationContext operationContext)
        {
            return Flatten(root, new EntityPropertyConverterOptions { PropertyNameDelimiter = DefaultPropertyNameDelimiter }, operationContext);
        }

        /// <summary>
        /// Traverses object graph, flattens and converts all nested (and not nested) properties to EntityProperties, stores them in the property dictionary.
        /// The keys are constructed by appending the names of the properties visited during pre-order depth first traversal from root to each end node property delimited by '_'.
        /// Allows complex objects to be stored in persistent storage systems or passed between web services in a generic way.
        /// </summary>
        /// <param name="root">The object to flatten and convert.</param>
        /// <param name="entityPropertyConverterOptions">A <see cref="EntityPropertyConverterOptions"/> object that specifies options for the entity property conversion.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The result containing <see cref="IDictionary{TKey,TValue}"/> of <see cref="EntityProperty"/> objects for all properties of the flattened root object.</returns>
        public static Dictionary<string, EntityProperty> Flatten(object root, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
        {
            if (root == null)
            {
                return null;
            }

            Dictionary<string, EntityProperty> propertyDictionary = new Dictionary<string, EntityProperty>();
            HashSet<object> antecedents = new HashSet<object>(new ObjectReferenceEqualityComparer());

            return Flatten(propertyDictionary, root, string.Empty, antecedents, entityPropertyConverterOptions, operationContext) ? propertyDictionary : null;
        }

        /// <summary>
        /// Reconstructs the complete object graph of type T using the flattened entity property dictionary and returns reconstructed object.
        /// The property dictionary may contain only basic properties, only nested properties or a mix of both types.
        /// </summary>
        /// <typeparam name="T">The type of the object to populate</typeparam>
        /// <param name="flattenedEntityProperties">The flattened entity property dictionary.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The result containing the reconstructed object with its full object hierarchy.</returns>
        public static T ConvertBack<T>(IDictionary<string, EntityProperty> flattenedEntityProperties, OperationContext operationContext)
        {
            return ConvertBack<T>(flattenedEntityProperties, new EntityPropertyConverterOptions { PropertyNameDelimiter = DefaultPropertyNameDelimiter }, operationContext);
        }

        /// <summary>
        /// Reconstructs the complete object graph of type T using the flattened entity property dictionary and returns reconstructed object.
        /// The property dictionary may contain only basic properties, only nested properties or a mix of both types.
        /// </summary>
        /// <typeparam name="T">The type of the object to populate</typeparam>
        /// <param name="flattenedEntityProperties">The flattened entity property dictionary.</param>
        /// <param name="entityPropertyConverterOptions">A <see cref="EntityPropertyConverterOptions"/> object that specifies options for the entity property conversion.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The result containing the reconstructed object with its full object hierarchy.</returns>
        public static T ConvertBack<T>(
            IDictionary<string, EntityProperty> flattenedEntityProperties,
            EntityPropertyConverterOptions entityPropertyConverterOptions,
            OperationContext operationContext)
        {
            if (flattenedEntityProperties == null)
            {
                return default(T);
            }

#if WINDOWS_DESKTOP || WINDOWS_RT || NETCORE
            T root = (T)Activator.CreateInstance(typeof(T));
#else
            T root = (T)FormatterServices.GetUninitializedObject(typeof(T));
#endif

            return flattenedEntityProperties.Aggregate(root, (current, kvp) => (T)SetProperty(current, kvp.Key, kvp.Value.PropertyAsObject, entityPropertyConverterOptions, operationContext));
        }

        /// <summary>
        /// Traverses object graph, flattens and converts all nested (and not nested) properties to EntityProperties, stores them in the property dictionary.
        /// The keys are constructed by appending the names of the properties visited during pre-order depth first traversal from root to each end node property delimited by '.'.
        /// Allows complex objects to be stored in persistent storage systems or passed between web services in a generic way.
        /// </summary>
        /// <param name="propertyDictionary">The property dictionary.</param>
        /// <param name="current">The current object.</param>
        /// <param name="objectPath">The object path.</param>
        /// <param name="antecedents">The antecedents of current object, used to detect circular references in object graph.</param>
        /// <param name="entityPropertyConverterOptions">A <see cref="EntityPropertyConverterOptions"/> object that specifies options for the entity property conversion.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The <see cref="bool"/> to indicate success of conversion to flattened EntityPropertyDictionary.</returns>
        private static bool Flatten(
            Dictionary<string, EntityProperty> propertyDictionary,
            object current,
            string objectPath,
            HashSet<object> antecedents,
            EntityPropertyConverterOptions entityPropertyConverterOptions,
            OperationContext operationContext)
        {
            if (current == null)
            {
                return true;
            }

            Type type = current.GetType();
            EntityProperty entityProperty = CreateEntityPropertyWithType(current, type);

            if (entityProperty != null)
            {
                propertyDictionary.Add(objectPath, entityProperty);
                return true;
            }

#if WINDOWS_RT
            IEnumerable<PropertyInfo> propertyInfos = type.GetRuntimeProperties();
#elif NETCORE
            IEnumerable<PropertyInfo> propertyInfos = type.GetTypeInfo().GetAllProperties();
#else
            IEnumerable<PropertyInfo> propertyInfos = type.GetProperties();
#endif
            if (!propertyInfos.Any())
            {
                throw new SerializationException(string.Format(CultureInfo.InvariantCulture, SR.UnsupportedPropertyTypeForEntityPropertyConversion, type, objectPath));
            }

            bool isAntecedent = false;

#if WINDOWS_RT || NETCORE
            if (!type.GetTypeInfo().IsValueType)
#else
            if (!type.IsValueType)
#endif
            {
                if (antecedents.Contains(current))
                {
                    throw new SerializationException(string.Format(CultureInfo.InvariantCulture, SR.RecursiveReferencedObject, objectPath, type));
                }

                antecedents.Add(current);
                isAntecedent = true;
            }

            string propertyNameDelimiter = entityPropertyConverterOptions != null ? entityPropertyConverterOptions.PropertyNameDelimiter : DefaultPropertyNameDelimiter;

            bool success = propertyInfos
                .Where(propertyInfo => !ShouldSkip(propertyInfo, objectPath, operationContext))
                .All(propertyInfo =>
                   {
                       if (propertyInfo.Name.Contains(propertyNameDelimiter))
                       {
                           throw new SerializationException(
                               string.Format(CultureInfo.InvariantCulture, SR.PropertyDelimiterExistsInPropertyName, propertyNameDelimiter, propertyInfo.Name, objectPath));
                       }

                       return Flatten(
                           propertyDictionary,
                           propertyInfo.GetValue(current, index: null),
                           string.IsNullOrWhiteSpace(objectPath) ? propertyInfo.Name : objectPath + propertyNameDelimiter + propertyInfo.Name,
                           antecedents,
                           entityPropertyConverterOptions,
                           operationContext);
                   });

            if (isAntecedent)
            {
                antecedents.Remove(current);
            }

            return success;
        }

        /// <summary>Creates entity property with given type.</summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="EntityProperty"/>.</returns>
        private static EntityProperty CreateEntityPropertyWithType(object value, Type type)
        {
            if (type == typeof(string))
            {
                return new EntityProperty((string)value);
            }
            else if (type == typeof(byte[]))
            {
                return new EntityProperty((byte[])value);
            }
            else if (type == typeof(bool))
            {
                return new EntityProperty((bool)value);
            }
            else if (type == typeof(bool?))
            {
                return new EntityProperty((bool?)value);
            }
            else if (type == typeof(DateTime))
            {
                return new EntityProperty((DateTime)value);
            }
            else if (type == typeof(DateTime?))
            {
                return new EntityProperty((DateTime?)value);
            }
            else if (type == typeof(DateTimeOffset))
            {
                return new EntityProperty((DateTimeOffset)value);
            }
            else if (type == typeof(DateTimeOffset?))
            {
                return new EntityProperty((DateTimeOffset?)value);
            }
            else if (type == typeof(double))
            {
                return new EntityProperty((double)value);
            }
            else if (type == typeof(double?))
            {
                return new EntityProperty((double?)value);
            }
            else if (type == typeof(Guid?))
            {
                return new EntityProperty((Guid?)value);
            }
            else if (type == typeof(Guid))
            {
                return new EntityProperty((Guid)value);
            }
            else if (type == typeof(int))
            {
                return new EntityProperty((int)value);
            }
            else if (type == typeof(int?))
            {
                return new EntityProperty((int?)value);
            }
            else if (type == typeof(uint))
            {
                return new EntityProperty(unchecked((int)Convert.ToUInt32(value, CultureInfo.InvariantCulture)));
            }
            else if (type == typeof(uint?))
            {
                return new EntityProperty(unchecked((int?)Convert.ToUInt32(value, CultureInfo.InvariantCulture)));
            }
            else if (type == typeof(long))
            {
                return new EntityProperty((long)value);
            }
            else if (type == typeof(long?))
            {
                return new EntityProperty((long?)value);
            }
            else if (type == typeof(ulong))
            {
                return new EntityProperty(unchecked((long)Convert.ToUInt64(value, CultureInfo.InvariantCulture)));
            }
            else if (type == typeof(ulong?))
            {
                return new EntityProperty(unchecked((long?)Convert.ToUInt64(value, CultureInfo.InvariantCulture)));
            }
#if WINDOWS_RT || NETCORE
            else if (type.GetTypeInfo().IsEnum)
#else
            else if (type.IsEnum)
#endif
            {
                return new EntityProperty(value.ToString());
            }
            else if (type == typeof(TimeSpan))
            {
                return new EntityProperty(value.ToString());
            }
            else if (type == typeof(TimeSpan?))
            {
                return new EntityProperty(value != null ? value.ToString() : null);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Sets the property given with the property path on the passed in object.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="propertyPath">The full property path formed by the name of properties from root object to the target property(included), appended by '.'.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="entityPropertyConverterOptions">A <see cref="EntityPropertyConverterOptions"/> object that specifies options for the entity property conversion.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The updated <see cref="object"/>.</returns>
        private static object SetProperty(
            object root,
            string propertyPath,
            object propertyValue,
            EntityPropertyConverterOptions entityPropertyConverterOptions,
            OperationContext operationContext)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            if (propertyPath == null)
            {
                throw new ArgumentNullException("propertyPath");
            }

            try
            {
                string propertyNameDelimiter = entityPropertyConverterOptions != null ? entityPropertyConverterOptions.PropertyNameDelimiter : DefaultPropertyNameDelimiter;

                Stack<Tuple<object, object, PropertyInfo>> valueTypePropertyHierarchy = new Stack<Tuple<object, object, PropertyInfo>>();
                string[] properties = propertyPath.Split(new[] { propertyNameDelimiter }, StringSplitOptions.RemoveEmptyEntries);

                object parentProperty = root;
                bool valueTypeDetected = false;

                for (int i = 0; i < properties.Length - 1; i++)
                {
#if WINDOWS_RT || NETCORE
                    PropertyInfo propertyToGet = parentProperty.GetType().GetRuntimeProperty(properties[i]);
#else
                    PropertyInfo propertyToGet = parentProperty.GetType().GetProperty(properties[i]);
#endif
                    object temp = propertyToGet.GetValue(parentProperty, null);
                    Type type = propertyToGet.PropertyType;

                    if (temp == null)
                    {
#if WINDOWS_DESKTOP || WINDOWS_RT || NETCORE
                        temp = Activator.CreateInstance(type);
#else
                        temp = FormatterServices.GetUninitializedObject(type);
#endif
                        propertyToGet.SetValue(parentProperty, ChangeType(temp, propertyToGet.PropertyType), index: null);
                    }

#if WINDOWS_RT || NETCORE
                    if (valueTypeDetected || type.GetTypeInfo().IsValueType)
#else
                    if (valueTypeDetected || type.IsValueType)
#endif
                    {
                        valueTypeDetected = true;
                        valueTypePropertyHierarchy.Push(new Tuple<object, object, PropertyInfo>(temp, parentProperty, propertyToGet));
                    }

                    parentProperty = temp;
                }

#if WINDOWS_RT || NETCORE
                PropertyInfo propertyToSet = parentProperty.GetType().GetRuntimeProperty(properties.Last());
#else
                PropertyInfo propertyToSet = parentProperty.GetType().GetProperty(properties.Last());
#endif
                propertyToSet.SetValue(parentProperty, ChangeType(propertyValue, propertyToSet.PropertyType), index: null);

                object termValue = parentProperty;
                while (valueTypePropertyHierarchy.Count != 0)
                {
                    Tuple<object, object, PropertyInfo> propertyTuple = valueTypePropertyHierarchy.Pop();
                    propertyTuple.Item3.SetValue(propertyTuple.Item2, ChangeType(termValue, propertyTuple.Item3.PropertyType), index: null);
                    termValue = propertyTuple.Item2;
                }

                return root;
            }
            catch (Exception ex)
            {
                Logger.LogError(operationContext, SR.TraceSetPropertyError, propertyPath, propertyValue, ex.Message);
                throw;
            }
        }

        /// <summary>Creates an object of specified propertyType from propertyValue.</summary>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="propertyType">The property type.</param>
        /// <returns>The <see cref="object"/>.</returns>
        private static object ChangeType(object propertyValue, Type propertyType)
        {
            Type underlyingType = Nullable.GetUnderlyingType(propertyType);
            Type type = underlyingType ?? propertyType;

#if WINDOWS_RT || NETCORE
            if (type.GetTypeInfo().IsEnum)
#else
            if (type.IsEnum)
#endif
            {
                return Enum.Parse(type, propertyValue.ToString());
            }

            if (type == typeof(DateTimeOffset))
            {
                return new DateTimeOffset((DateTime)propertyValue);
            }

            if (type == typeof(TimeSpan))
            {
                return TimeSpan.Parse(propertyValue.ToString(), CultureInfo.InvariantCulture);
            }

            if (type == typeof(uint))
            {
                return unchecked((uint)(int)propertyValue);
            }

            if (type == typeof(ulong))
            {
                return unchecked((ulong)(long)propertyValue);
            }

            return Convert.ChangeType(propertyValue, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Indicates whether the object member should be skipped from being flattened
        /// </summary>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="objectPath">The object path.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The <see cref="bool"/> to indicate whether the object member should be skipped from being flattened.</returns>
        private static bool ShouldSkip(PropertyInfo propertyInfo, string objectPath, OperationContext operationContext)
        {
            if (!propertyInfo.CanWrite)
            {
                Logger.LogInformational(operationContext, SR.TraceNonExistingSetter, propertyInfo.Name, objectPath);
                return true;
            }

            if (!propertyInfo.CanRead)
            {
                Logger.LogInformational(operationContext, SR.TraceNonExistingGetter, propertyInfo.Name, objectPath);
                return true;
            }

#if WINDOWS_RT || NETCORE
            return propertyInfo.GetCustomAttribute(typeof(IgnorePropertyAttribute)) != null;
#else
            return Attribute.IsDefined(propertyInfo, typeof(IgnorePropertyAttribute));
#endif
        }

        /// <summary>
        /// The object reference equality comparer.
        /// </summary>
        private class ObjectReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}