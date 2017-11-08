using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage.Core.Util
{
internal static class CommonUtility
{
    private static readonly int[] PathStylePorts = new int[20]
    {
      10000,
      10001,
      10002,
      10003,
      10004,
      10100,
      10101,
      10102,
      10103,
      10104,
      11000,
      11001,
      11002,
      11003,
      11004,
      11100,
      11101,
      11102,
      11103,
      11104
    };

    internal static CommandLocationMode GetListingLocationMode(IContinuationToken token)
    {
        throw new System.NotImplementedException();
    }
    public static TimeSpan MaxTimeSpan(TimeSpan val1, TimeSpan val2)
    {
        throw new System.NotImplementedException();
    }
    public static string GetFirstHeaderValue<T>(IEnumerable<T> headerValues) where T : class
    {
        throw new System.NotImplementedException();
    }
    internal static void AssertNotNullOrEmpty(string paramName, string value)
    {
        throw new System.NotImplementedException();
    }
    internal static void AssertNotNull(string paramName, object value)
    {
        throw new System.NotImplementedException();
    }
    internal static void ArgumentOutOfRange(string paramName, object value)
    {
        throw new System.NotImplementedException();
    }
     
    internal static void AssertInBounds<T>(string paramName, T val, T min, T max) where T : IComparable
    {
        throw new System.NotImplementedException();
    }
     
    internal static void AssertInBounds<T>(string paramName, T val, T min) where T : IComparable
    {
        throw new System.NotImplementedException();
    }
    internal static void CheckStringParameter(string paramName, bool canBeNullOrEmpty, string value, int maxSize)
    {
        throw new System.NotImplementedException();
    }
    internal static int RoundUpToSeconds(this TimeSpan timeSpan)
    {
        throw new System.NotImplementedException();
    }
    internal static byte[] BinaryAppend(byte[] arr1, byte[] arr2)
    {
        throw new System.NotImplementedException();
    }
    internal static bool UsePathStyleAddressing(Uri uri)
    {
        throw new System.NotImplementedException();
    }
    internal static string ReadElementAsString(string elementName, XmlReader reader)
    {
        throw new System.NotImplementedException();
    }
    internal static IEnumerable<T> LazyEnumerable<T>(Func<IContinuationToken, ResultSegment<T>> segmentGenerator, long maxResults)
    {
        throw new System.NotImplementedException();
    }
    internal static void RunWithoutSynchronizationContext(Action actionToRun)
    {
        throw new System.NotImplementedException();
    }
    internal static T RunWithoutSynchronizationContext<T>(Func<T> actionToRun)
    {
        throw new System.NotImplementedException();
    }
}

}