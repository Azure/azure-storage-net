using System;
using System.Globalization;
namespace Microsoft.Azure.Storage.File
{
public sealed class FileRange
{
    public long StartOffset
    {
        get; internal set;
    }

    public long EndOffset
    {
        get; internal set;
    }

    public FileRange(long start, long end)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}