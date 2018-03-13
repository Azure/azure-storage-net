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
            this.Double = 1234.5678;
            this.DoubleNan = double.NaN;
            this.DoubleInf = double.PositiveInfinity;
            this.DoubleNegInf = double.NegativeInfinity;
            this.NotNullDouble = 5678.1234;
            this.NotNullDoubleNan = double.NaN;
            this.NotNullDoubleInf = double.PositiveInfinity;
            this.NotNullDoubleNegInf = double.NegativeInfinity;
            this.NullDouble = default(double?);
            this.Int32 = 1234;
            this.Int64 = 5678;
            this.Guid = guidValue;
            this.DateTime = dateTimeValue;
            this.True = true;
            this.False = false;
            this.Binary = Encoding.Unicode.GetBytes(guidValue.ToString() + dateTimeValue.ToString());
        }

        static DateTime dateTimeValue = DateTime.UtcNow;
        static Guid guidValue = new Guid("{CA303396-5D78-42A6-A63E-7A63449E7CAF}");

        public string foo { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public int E { get; set; }
        public double Double { get; set; }
        public double DoubleNan { get; set; }
        public double DoubleInf { get; set; }
        public double DoubleNegInf { get; set; }
        public double? NotNullDouble { get; set; }
        public double? NotNullDoubleNan { get; set; }
        public double? NotNullDoubleInf { get; set; }
        public double? NotNullDoubleNegInf { get; set; }
        public double? NullDouble { get; set; }
        public Int32 Int32 { get; set; }
        public Int64 Int64 { get; set; }
        public Guid Guid { get; set; }
        public DateTime DateTime { get; set; }
        public bool True { get; set; }
        public bool False { get; set; }
        public byte[] Binary { get; set; }

        public void Validate()
        {
            Assert.AreEqual("bar", this.foo);
            Assert.AreEqual("a", this.A);
            Assert.AreEqual("b", this.B);
            Assert.AreEqual("c", this.C);
            Assert.AreEqual("d", this.D);
            Assert.AreEqual(1234, this.E);
            Assert.AreEqual(1234.5678, this.Double);
            Assert.AreEqual(Double.NaN, this.DoubleNan);
            Assert.AreEqual(Double.PositiveInfinity, this.DoubleInf);
            Assert.AreEqual(Double.NegativeInfinity, this.DoubleNegInf);
            Assert.AreEqual(5678.1234, this.NotNullDouble);
            Assert.AreEqual(Double.NaN, this.NotNullDoubleNan);
            Assert.AreEqual(Double.PositiveInfinity, this.NotNullDoubleInf);
            Assert.AreEqual(Double.NegativeInfinity, this.NotNullDoubleNegInf);
            Assert.IsFalse(this.NullDouble.HasValue);
            Assert.AreEqual(1234, this.Int32);
            Assert.AreEqual(5678, this.Int64);
            Assert.AreEqual(guidValue, this.Guid);
            Assert.AreEqual(dateTimeValue, this.DateTime);
            Assert.AreEqual(true, this.True);
            Assert.AreEqual(false, this.False);
            Assert.IsTrue(
                Encoding.Unicode.GetBytes(guidValue.ToString() + dateTimeValue.ToString())
                .SequenceEqual(this.Binary)
                );
        }

        public static EdmType BaseEntityPropertyResolver(string partitionKey, string rowKey, string propName, string propValue)
        {
            switch (propName)
            {
                case "foo":
                case "A":
                case "B":
                case "C":
                case "D":
                    return EdmType.String;
                case "E":
                    return EdmType.Int32;
                case "Double":
                case "DoubleNan":
                case "DoubleInf":
                case "DoubleNegInf":
                    return EdmType.Double;
                case "Int32":
                    return EdmType.Int32;
                case "Int64":
                    return EdmType.Int64;
                case "Guid":
                    return EdmType.Guid;
                case "DateTime":
                    return EdmType.DateTime;
                case "True":
                case "False": return EdmType.Boolean;
                case "Binary":
                    return EdmType.Binary;
            }
            
            Assert.Fail("Unexpected property name");
            return default(EdmType); // never reached
        }
    }
}
