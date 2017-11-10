using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
 
 
namespace Microsoft.Azure.Storage.Table
{
public sealed class EntityProperty
{

    public object PropertyAsObject
    {
        get
        {
            throw new System.NotImplementedException();
        }
        internal set
        {
            throw new System.NotImplementedException();
        }
    }

    public EdmType PropertyType
    {
        get; private set;
    }

    public byte[] BinaryValue
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

    public bool? BooleanValue
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

    public DateTimeOffset? DateTimeOffsetValue
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

    public double? DoubleValue
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

    public Guid? GuidValue
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

    public int? Int32Value
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

    public long? Int64Value
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

    public string StringValue
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

    internal bool IsNull
    {
        get; set;
    }

    internal bool IsEncrypted
    {
        get; set;
    }

    public EntityProperty(string input)
      : this(EdmType.String)
    {
        throw new System.NotImplementedException();
    }
    private EntityProperty(EdmType propertyType)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForDateTimeOffset(DateTimeOffset? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForByteArray(byte[] input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForBool(bool? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForDouble(double? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForGuid(Guid? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForInt(int? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForLong(long? input)
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty GeneratePropertyForString(string input)
    {
        throw new System.NotImplementedException();
    }
    public override bool Equals(object obj)
    {
        throw new System.NotImplementedException();
    }
    public bool Equals(EntityProperty other)
    {
        throw new System.NotImplementedException();
    }
    public override int GetHashCode()
    {
        throw new System.NotImplementedException();
    }
    public static EntityProperty CreateEntityPropertyFromObject(object entityValue)
    {
        throw new System.NotImplementedException();
    }
    internal static EntityProperty CreateEntityPropertyFromObject(object value, bool allowUnknownTypes)
    {
        throw new System.NotImplementedException();
    }
    internal static EntityProperty CreateEntityPropertyFromObject(object value, Type type)
    {
        throw new System.NotImplementedException();
    }
    internal static EntityProperty CreateEntityPropertyFromObject(object value, EdmType type)
    {
        throw new System.NotImplementedException();
    }
    private void EnforceType(EdmType requestedType)
    {
        throw new System.NotImplementedException();
    }
}

}