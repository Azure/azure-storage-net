using Microsoft.Azure.Storage.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
namespace Microsoft.Azure.Storage.Table
{
public static class EntityPropertyConverter
{
    public const string DefaultPropertyNameDelimiter = "_";

    public static Dictionary<string, EntityProperty> Flatten(object root, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public static Dictionary<string, EntityProperty> Flatten(object root, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public static T ConvertBack<T>(IDictionary<string, EntityProperty> flattenedEntityProperties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public static T ConvertBack<T>(IDictionary<string, EntityProperty> flattenedEntityProperties, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private static bool Flatten(Dictionary<string, EntityProperty> propertyDictionary, object current, string objectPath, HashSet<object> antecedents, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private static EntityProperty CreateEntityPropertyWithType(object value, Type type)
    {
        throw new System.NotImplementedException();
    }
    private static object SetProperty(object root, string propertyPath, object propertyValue, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private static object ChangeType(object propertyValue, Type propertyType)
    {
        throw new System.NotImplementedException();
    }
    private static bool ShouldSkip(PropertyInfo propertyInfo, string objectPath, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private class ObjectReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            throw new System.NotImplementedException();
        }
        public int GetHashCode(object obj)
        {
            throw new System.NotImplementedException();
        }
    }
}

}