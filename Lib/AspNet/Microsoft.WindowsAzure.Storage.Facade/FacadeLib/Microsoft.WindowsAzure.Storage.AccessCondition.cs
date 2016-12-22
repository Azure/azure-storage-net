using System;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class AccessCondition
{
    public string IfMatchETag
    {
        get; set;
    }

    public string IfNoneMatchETag
    {
        get; set;
    }

    public DateTimeOffset? IfModifiedSinceTime
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

    public DateTimeOffset? IfNotModifiedSinceTime
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

    public long? IfMaxSizeLessThanOrEqual
    {
        get; set;
    }

    public long? IfAppendPositionEqual
    {
        get; set;
    }

    public long? IfSequenceNumberLessThanOrEqual
    {
        get; set;
    }

    public long? IfSequenceNumberLessThan
    {
        get; set;
    }

    public long? IfSequenceNumberEqual
    {
        get; set;
    }

    public string LeaseId
    {
        get; set;
    }

    internal bool IsConditional
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public static AccessCondition GenerateEmptyCondition()
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfNotExistsCondition()
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfExistsCondition()
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfMatchCondition(string etag)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfModifiedSinceCondition(DateTimeOffset modifiedTime)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfNoneMatchCondition(string etag)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfNotModifiedSinceCondition(DateTimeOffset modifiedTime)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfMaxSizeLessThanOrEqualCondition(long maxSize)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfAppendPositionEqualCondition(long appendPosition)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfSequenceNumberLessThanOrEqualCondition(long sequenceNumber)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfSequenceNumberLessThanCondition(long sequenceNumber)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateIfSequenceNumberEqualCondition(long sequenceNumber)
    {
        throw new System.NotImplementedException();
    }
    public static AccessCondition GenerateLeaseCondition(string leaseId)
    {
        throw new System.NotImplementedException();
    }
    internal static AccessCondition CloneConditionWithETag(AccessCondition accessCondition, string etag)
    {
        throw new System.NotImplementedException();
    }
}

}