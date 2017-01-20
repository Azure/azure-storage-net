using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Globalization;
 
namespace Microsoft.WindowsAzure.Storage
{
public class IPAddressOrRange
{
    public string Address
    {
        get; private set;
    }

    public string MinimumAddress
    {
        get; private set;
    }

    public string MaximumAddress
    {
        get; private set;
    }

    public bool IsSingleAddress
    {
        get; private set;
    }

    public IPAddressOrRange(string address)
    {
        throw new System.NotImplementedException();
    }
    public IPAddressOrRange(string minimum, string maximum)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
    private static void AssertIPv4(string address)
    {
        throw new System.NotImplementedException();
    }
}

}