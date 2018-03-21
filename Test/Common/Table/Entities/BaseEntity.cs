// -----------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;

#if WINDOWS_DESKTOP
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace Microsoft.WindowsAzure.Storage.Table.Entities
{
    public class BaseEntity : TableEntity
    {
        public BaseEntity()
        {
            this.DateTime = dateTimeValue;
        }

        public BaseEntity(string pk, string rk)
            : base(pk, rk)
        {
            this.DateTime = dateTimeValue;
        }

        public void Populate()
        {
            this.foo = "bar";
            this.A = "a";
            this.B = "b";
            this.C = "c";
            this.D = "d";
            this.E = 1234;
            this.Double = (Double)1234.5678;
            this.DoubleEpsilon = Double.Epsilon;
            this.DoubleNan = Double.NaN;
            this.DoublePositiveInfinity = Double.PositiveInfinity;
            this.DoubleNegativeInfinity = Double.NegativeInfinity;
            this.DoubleNullWithValue = (Double?)1234.5678;
            this.DoubleNullWithNan = Double.NaN;
            this.DoubleNullWithPositiveInfinity = Double.PositiveInfinity;
            this.DoubleNullWithNegativeInfinity = Double.NegativeInfinity;
            this.String = "testString";
            this.Int32 = (Int32)1234;
            this.Int32NullWithValue = (Int32?)1234;
            this.Int64 = (Int64)1234;
            this.Int64NullWithValue = (Int64?)1234;
            this.DateTime = dateTimeValue;
            this.DateTimeNullWithValue = (DateTime?)dateTimeValue;
            this.DateTimeOffset = dateTimeOffsetValue;
            this.DateTimeOffsetNullWithValue = (DateTimeOffset?)dateTimeOffsetValue;
            this.Guid = guidValue;
            this.GuidNullWithValue = (Guid?)guidValue;
            this.BooleanTrue = true;
            this.BooleanFalse = false;
            this.BooleanNull = default(Boolean?);
            this.BooleanNullWithTrue = (Boolean?)true;
            this.BooleanNullWithFalse = (Boolean?)false;
            this.Binary = Encoding.Unicode.GetBytes(guidValue.ToString() + dateTimeValue.ToString());
        }

        static DateTime dateTimeValue = DateTime.UtcNow;
        static DateTimeOffset dateTimeOffsetValue = DateTimeOffset.UtcNow;
        static Guid guidValue = new Guid("{CA303396-5D78-42A6-A63E-7A63449E7CAF}");

        public string foo { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public int E { get; set; }
        public Boolean? BooleanNull { get; set; }
        public Boolean? BooleanNullWithFalse { get; set; }
        public Boolean? BooleanNullWithTrue { get; set; }
        public Boolean BooleanFalse { get; set; }
        public Boolean BooleanTrue { get; set; }
        public Byte[] Binary { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime? DateTimeNullWithValue { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTimeOffset? DateTimeOffsetNullWithValue { get; set; }
        public Double Double { get; set; }
        public Double DoubleEpsilon { get; set; }
        public Double DoubleNan { get; set; }
        public Double DoubleNegativeInfinity { get; set; }
        public Double? DoubleNullWithNan { get; set; }
        public Double? DoubleNullWithNegativeInfinity { get; set; }
        public Double? DoubleNullWithPositiveInfinity { get; set; }
        public Double? DoubleNullWithValue { get; set; }
        public Double DoublePositiveInfinity { get; set; }
        public Guid Guid { get; set; }
        public Guid? GuidNullWithValue { get; set; }
        public Int32 Int32 { get; set; }
        public Int32? Int32NullWithValue { get; set; }
        public Int64 Int64 { get; set; }
        public Int64? Int64NullWithValue { get; set; }
        public String String { get; set; }

        public void Validate()
        {
            Assert.AreEqual("bar", this.foo);
            Assert.AreEqual("a", this.A);
            Assert.AreEqual("b", this.B);
            Assert.AreEqual("c", this.C);
            Assert.AreEqual("d", this.D);
            Assert.AreEqual(1234, this.E);

            Assert.AreEqual(this.Double, (Double)1234.5678);
            Assert.AreEqual(this.DoubleEpsilon, Double.Epsilon);
            Assert.AreEqual(this.DoubleNan, Double.NaN);
            Assert.AreEqual(this.DoublePositiveInfinity, Double.PositiveInfinity);
            Assert.AreEqual(this.DoubleNegativeInfinity, Double.NegativeInfinity);
            Assert.AreEqual(this.DoubleNullWithValue, (Double?)1234.5678);
            Assert.AreEqual(this.DoubleNullWithNan, Double.NaN);
            Assert.AreEqual(this.DoubleNullWithPositiveInfinity, Double.PositiveInfinity);
            Assert.AreEqual(this.DoubleNullWithNegativeInfinity, Double.NegativeInfinity);

            Assert.AreEqual(this.String, "testString");

            Assert.AreEqual(this.Int32, (Int32)1234);
            Assert.AreEqual(this.Int32NullWithValue, (Int32?)1234);

            Assert.AreEqual(this.Int64, (Int64)1234);
            Assert.AreEqual(this.Int64NullWithValue, (Int64?)1234);

            Assert.AreEqual(this.DateTime, dateTimeValue);
            Assert.AreEqual(this.DateTimeNullWithValue, dateTimeValue);

            Assert.AreEqual(this.DateTimeOffset, dateTimeOffsetValue);
            Assert.AreEqual(this.DateTimeOffsetNullWithValue, (DateTimeOffset?)dateTimeOffsetValue);

            Assert.AreEqual(this.Guid, guidValue);
            Assert.AreEqual(this.GuidNullWithValue, guidValue);

            Assert.AreEqual(this.BooleanTrue, true);
            Assert.AreEqual(this.BooleanFalse, false);
            Assert.AreEqual(this.BooleanNull, default(Boolean?));
            Assert.AreEqual(this.BooleanNullWithTrue, (Boolean?)true);
            Assert.AreEqual(this.BooleanNullWithFalse, (Boolean?)false);

            Assert.IsTrue(
                Encoding.Unicode.GetBytes(guidValue.ToString() + dateTimeValue.ToString())
                .SequenceEqual(this.Binary)
                );
        }

        public static EdmType BaseEntityPropertyResolver(string partitionKey, string rowKey, string propName, string propValue)
        {
            switch (propName)
            {
                case nameof(foo):
                case nameof(A):
                case nameof(B):
                case nameof(C):
                case nameof(D):
                    return EdmType.String;

                case nameof(E):
                    return EdmType.Int32;

                case nameof(BooleanNull):
                case nameof(BooleanNullWithFalse):
                case nameof(BooleanNullWithTrue):
                case nameof(BooleanFalse):
                case nameof(BooleanTrue):
                    return EdmType.Boolean;

                case nameof(Binary):
                    return EdmType.Binary;

                case nameof(DateTime):
                case nameof(DateTimeNullWithValue):
                case nameof(DateTimeOffset):
                case nameof(DateTimeOffsetNullWithValue):
                    return EdmType.DateTime;

                case nameof(Double):
                case nameof(DoubleEpsilon):
                case nameof(DoubleNan):
                case nameof(DoubleNegativeInfinity):
                case nameof(DoubleNullWithNan):
                case nameof(DoubleNullWithNegativeInfinity):
                case nameof(DoubleNullWithPositiveInfinity):
                case nameof(DoubleNullWithValue):
                case nameof(DoublePositiveInfinity):
                    return EdmType.Double;

                case nameof(Guid):
                case nameof(GuidNullWithValue):
                    return EdmType.Guid;

                case nameof(Int32):
                case nameof(Int32NullWithValue):
                    return EdmType.Int32;

                case nameof(Int64):
                case nameof(Int64NullWithValue):
                    return EdmType.Int64;

                case nameof(String):
                    return EdmType.String;
            }
            
            Assert.Fail("Unexpected property name");

            return default(EdmType); // never reached
        }
    }
}
