using System;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public class PageRange
{
    public long StartOffset
    {
        get; internal set;
    }

    public long EndOffset
    {
        get; internal set;
    }

    public PageRange(long start, long end)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}