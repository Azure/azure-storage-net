using System;
using System.Collections.Generic;
using System.Reflection;
namespace Microsoft.WindowsAzure.Storage.Core.Util
{
internal static class PlatformAgnosticReflectionExtensions
{
    public static IEnumerable<MethodInfo> FindStaticMethods(this Type type, string name)
    {
        throw new System.NotImplementedException();
    }
    public static IEnumerable<MethodInfo> FindMethods(this Type type)
    {
        throw new System.NotImplementedException();
    }
    public static MethodInfo FindMethod(this Type type, string name, Type[] parameters)
    {
        throw new System.NotImplementedException();
    }
    public static PropertyInfo FindProperty(this Type type, string name)
    {
        throw new System.NotImplementedException();
    }
    public static MethodInfo FindGetProp(this PropertyInfo property)
    {
        throw new System.NotImplementedException();
    }
    public static MethodInfo FindSetProp(this PropertyInfo property)
    {
        throw new System.NotImplementedException();
    }
}

}