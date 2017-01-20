using System;
using System.Globalization;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage.File
{
public sealed class FileContinuationToken : IContinuationToken
{
    private string Version
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    private string Type
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public string NextMarker
    {
        get; set;
    }

    public StorageLocation? TargetLocation
    {
        get; set;
    }
}

}