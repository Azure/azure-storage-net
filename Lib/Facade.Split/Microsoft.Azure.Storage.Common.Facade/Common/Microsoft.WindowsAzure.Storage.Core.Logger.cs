using System;
using System.Globalization;
namespace Microsoft.Azure.Storage.Core
{
internal static class Logger
{
    private const string TraceFormat = "{0}: {1}";

    internal static void LogError(OperationContext operationContext, string format, params object[] args)
    {
        throw new System.NotImplementedException();
    }
    internal static void LogWarning(OperationContext operationContext, string format, params object[] args)
    {
        throw new System.NotImplementedException();
    }
    internal static void LogInformational(OperationContext operationContext, string format, params object[] args)
    {
        throw new System.NotImplementedException();
    }
    internal static void LogVerbose(OperationContext operationContext, string format, params object[] args)
    {
        throw new System.NotImplementedException();
    }
    private static string FormatLine(OperationContext operationContext, string format, object[] args)
    {
        throw new System.NotImplementedException();
    }
}

}