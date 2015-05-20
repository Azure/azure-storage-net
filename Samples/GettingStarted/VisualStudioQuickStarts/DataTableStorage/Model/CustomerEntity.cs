//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
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
namespace DataTableStorageSample.Model
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Define a Customer entity for demonstrating the Table Service. For the purposes of the sample we use the 
    /// customer's first name as the row key and last name as the partition key. In reality this would not be a good
    /// PK and RK combination as it would likely not be gauranteed to be unique which is one of the requirements for an entity. 
    /// <summary>
    public class CustomerEntity : TableEntity
    {
        // Your entity type must expose a parameter-less constructor
        public CustomerEntity() { }

        // Define the PK and RK
        public CustomerEntity(string lastName, string firstName)
        {
            this.PartitionKey = lastName;
            this.RowKey = firstName;
        }

        //For any property that should be stored in the table service, the property must be a public property of a supported type that exposes both get and set.        
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
