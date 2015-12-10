# Microsoft Azure Storage SDK for .NET (6.2.0)

The Microsoft Azure Storage SDK for .NET allows you to build Azure applications 
that take advantage of scalable cloud computing resources.

This repository contains the open source subset of the .NET SDK. For documentation of the 
complete Azure SDK, please see the [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/).

## Features

- Tables
    - Create/Delete Tables
    - Query/Create/Read/Update/Delete Entities
- Blobs
    - Create/Read/Update/Delete Blobs
- Files
	- Create/Update/Delete Directories
	- Create/Read/Update/Delete Files
- Queues
    - Create/Delete Queues
    - Insert/Peek Queue Messages
    - Advanced Queue Operations
	
## Getting Started

The complete Microsoft Azure SDK can be downloaded from the [Microsoft Azure Downloads Page](http://azure.microsoft.com/en-us/downloads/?sdk=net) and ships with support for building deployment packages, integrating with tooling, rich command line tooling, and more.

Please review [Get started with Azure Storage in five minutes](http://azure.microsoft.com/en-us/documentation/articles/storage-getting-started-guide/) if you are not familiar with Azure Storage.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

## Target Frameworks

- .NET Framework 4.0: As of October 2012, Storage Client Libraries for .NET supports primarily the desktop .NET Framework 4 release and above.
- Windows 8 and 8.1 for Windows Store app development: Storage Client Libraries are available for Windows Store applications.
- Windows Phone 8 and 8.1 app development: Storage Client Libraries are available for Windows Phone applications including Universal applications.
- CoreCLR (rc1): Storage Client Libraries for .NET are available to support CoreCLR application development. This support is currently in preview. 
- Portable Class Library Profile 111: Storage Client Libraries are available to support building PCL Profile 111 applications, as well as for Xamarin application development. This support is currently in preview.
 
## Requirements

- Microsoft Azure Subscription: To call Microsoft Azure services, you need to first [create an account](https://account.windowsazure.com/Home/Index). Sign up for a free trial or use your MSDN subscriber benefits.
- Hosting: To host your .NET code in Microsoft Azure, you additionally need to download the full Microsoft Azure SDK for .NET - which includes packaging,
    emulation, and deployment tools, or use Microsoft Azure Web Sites to deploy ASP.NET web applications.

## Versioning Information

- The Storage Client Library uses [the semantic versioning scheme.](http://semver.org/)
- The Storage Client Library currently ships twinned versions -- for each release, the WindowsAzure.Storage Nuget package will have an X.Y.Z version and a matching X.Y.(Z+1)-preview version. The preview version includes support for the target frameworks that are currently in preview (DNX & DNXCore, PCL/Xamarin).

## Use with the Azure Storage Emulator

- The Client Library uses a particular Storage Service version. In order to use the Storage Client Library with the Storage Emulator, a corresponding minimum version of the Azure Storage Emulator must be used. Older versions of the Storage Emulator do not have the necessary code to successfully respond to new requests. 
- Currently, the minimum version of the Azure Storage Emulator needed for this library is 4.2. If you encounter a `VersionNotSupportedByEmulator` (400 Bad Request) error, please [update the Storage Emulator.](https://azure.microsoft.com/en-us/downloads/)

## Download & Install

The Storage Client Library ships with the Microsoft Azure SDK for .NET and also on NuGet. You'll find the latest version and hotfixes on NuGet via the `WindowsAzure.Storage` package. 

This version of the Storage Client Library ships with the storage version 2015-02-21. This storage version provides a preview of the Microsoft Azure File Service. For more details,
please see the [Introducing Microsoft Azure File Service blog on MSDN] (http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/11/introducing-microsoft-azure-file-service.aspx).

### Via Git

To get the source code of the SDK via git just type:

```bash
git clone git://github.com/Azure/azure-storage-net.git
cd azure-storage-net
```

### Via NuGet

To get the binaries of this library as distributed by Microsoft, ready for use
within your project you can also have them installed by the .NET package manager [NuGet](https://www.nuget.org/packages/WindowsAzure.Storage/).

`Install-Package WindowsAzure.Storage`

## Dependencies

### OData

This version depends on three libraries (collectively referred to as ODataLib), which are resolved through the ODataLib (version 5.6.4) packages available through NuGet and not the WCF Data Services installer which currently contains 5.0.0 versions.

The ODataLib libraries can be downloaded directly or referenced by your code project through NuGet.  

The specific ODataLib packages are:

- [Microsoft.Data.OData](http://nuget.org/packages/Microsoft.Data.OData/)
- [Microsoft.Data.Edm](http://nuget.org/packages/Microsoft.Data.Edm/)
- [System.Spatial](http://nuget.org/packages/System.Spatial)

### Newtonsoft Json

The desktop and phone libraries depend on Newtonsoft Json, which can be downloaded directly or referenced by your code project through Nuget.

- [Newtonsoft.Json] (http://www.nuget.org/packages/Newtonsoft.Json)

### WCF Data Services Client

The desktop library depends on WCF Data Services Client, which can be downloaded directly or referenced by your code project through Nuget.

- [Microsoft.Data.Services.Client] (http://www.nuget.org/packages/Microsoft.Data.Services.Client/)

### Key Vault

The client-side encryption support depends on the KeyVault.Core package, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Core] (http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Core)

### Test Dependencies

FiddlerCore is required by:

- Test\FaultInjection\HttpMangler
- Test\FaultInjection\AzureStoreMangler
- Test\WindowsDesktop

This dependency is not included and must be downloaded from [http://www.telerik.com/fiddler/fiddlercore](http://www.telerik.com/fiddler/fiddlercore).

Once installed:

- Copy `FiddlerCore.dll` `\azure-storage-net\Test\FaultInjection\Dependencies\DotNet2`
- Copy `FiddlerCore4.dll` to `azure-storage-net\Test\FaultInjection\Dependencies\DotNet4`

Tests for the client-side encryption preview support also depend on KeyVault.Extensions, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Extensions] (http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Extensions)

## Code Samples

> Note:
> How-Tos focused around accomplishing specific tasks are available on the [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/).

### Creating a Table

First, include the classes you need (in this case we'll include the Storage and Table
and further demonstrate creating a table):

```csharp
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
```

To perform an operation on any Microsoft Azure resource you will first instantiate
a *client* which allows performing actions on it. The resource is known as an 
*entity*. To do so for Table you also have to authenticate your request:

```csharp
var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
var tableClient = storageAccount.CreateCloudTableClient();
```

Now, to create a table entity using the client:

```csharp
CloudTable peopleTable = tableClient.GetTableReference("people");
peopleTable.Create();
```

## Need Help?
Be sure to check out the Microsoft Azure [Developer Forums on MSDN](http://go.microsoft.com/fwlink/?LinkId=234489) if you have trouble with the provided code or use StackOverflow.

## Collaborate & Contribute

We gladly accept community contributions.

- Issues: Please report bugs using the Issues section of GitHub
- Forums: Interact with the development teams on StackOverflow or the Microsoft Azure Forums
- Source Code Contributions: Please follow the [contribution guidelines for Microsoft Azure open source](http://azure.github.io/guidelines.html) that details information on onboarding as a contributor 

For general suggestions about Microsoft Azure please use our [UserVoice forum](http://feedback.azure.com/forums/34192--general-feedback).

# Learn More

- [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/)
- [Storage Client Library Reference for .NET - MSDN](http://msdn.microsoft.com/en-us/library/wa_storage_30_reference_home.aspx)
- [Azure Storage Team Blog] (http://blogs.msdn.com/b/windowsazurestorage/)
