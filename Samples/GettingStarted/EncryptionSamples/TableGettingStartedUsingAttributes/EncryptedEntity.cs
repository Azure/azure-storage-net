//----------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------
namespace TableGettingStartedUsingAttributes
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EncryptedEntity : TableEntity
    {
        public EncryptedEntity()
        {
        }

        public EncryptedEntity(string pk, string rk)
            : base(pk, rk)
        {
        }

        public void Populate()
        {
            this.EncryptedProperty1 = string.Empty;
            this.EncryptedProperty2 = "foo";
            this.NotEncryptedProperty = "b";
            this.NotEncryptedIntProperty = 1234;
        }

        [EncryptProperty]
        public string EncryptedProperty1 { get; set; }

        [EncryptProperty]
        public string EncryptedProperty2 { get; set; }

        public string NotEncryptedProperty { get; set; }
    
        public int NotEncryptedIntProperty { get; set; }
    }
}
