using System;
using System.Collections.Generic;
using System.Globalization;
 
 
using System.Text;
namespace Microsoft.Azure.Storage.Core.Util
{
internal static class AuthenticationUtility
{
    private const int ExpectedResourceStringLength = 100;
    private const int ExpectedHeaderNameAndValueLength = 50;
    private const char HeaderNameValueSeparator = ':';
    private const char HeaderValueDelimiter = ',';

    public static string GetCanonicalizedHeaderValue(DateTimeOffset? value)
    {
        throw new System.NotImplementedException();
    }
    private static string GetAbsolutePathWithoutSecondarySuffix(Uri uri, string accountName)
    {
        throw new System.NotImplementedException();
    }
    public static string GetCanonicalizedResourceString(Uri uri, string accountName, bool isSharedKeyLiteOrTableService = false)
    {
        throw new System.NotImplementedException();
    }
}

}