# Microsoft Azure Storage SDK for .NET (9.4.0)

The Microsoft Azure Storage SDK for .NET allows you to build Azure applications 
that take advantage of scalable cloud computing resources.

This repository contains the open source subset of the .NET SDK. For documentation of the 
complete Azure SDK, please see the [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/).

## Features

- Blobs
    - Create/Read/Update/Delete Blobs
- Files
    - Create/Update/Delete Directories
    - Create/Read/Update/Delete Files
- Queues
    - Create/Delete Queues
    - Insert/Peek Queue Messages
    - Advanced Queue Operations

As of 9.4.0, Tables are not supported by this library.

## Getting Started

The complete Microsoft Azure SDK can be downloaded from the [Microsoft Azure Downloads Page](http://azure.microsoft.com/en-us/downloads/?sdk=net) and ships with support for building deployment packages, integrating with tooling, rich command line tooling, and more.

Please review [Get started with Azure Storage](https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-blobs) if you are not familiar with Azure Storage.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

## Target Frameworks

- .NET Framework 4.5.2: As of September 2018, Storage Client Libraries for .NET supports primarily the desktop .NET Framework 4.5.2 release and above.
- Netstandard1.3: Storage Client Libraries for .NET are available to support Netstandard application development including Xamarin/UWP applications. 
- Netstandard2.0: Storage Client Libraries for .NET are available to support Netstandard2.0 application development including Xamarin/UWP applications. 

## Requirements

- Microsoft Azure Subscription: To call Microsoft Azure services, you need to first [create an account](https://account.Azure.com/Home/Index). Sign up for a free trial or use your MSDN subscriber benefits.
- Hosting: To host your .NET code in Microsoft Azure, you additionally need to download the full Microsoft Azure SDK for .NET - which includes packaging,
    emulation, and deployment tools, or use Microsoft Azure Web Sites to deploy ASP.NET web applications.

## Versioning Information

- The Storage Client Library uses [the semantic versioning scheme.](http://semver.org/)

## Use with the Azure Storage Emulator

- The Client Library uses a particular Storage Service version. In order to use the Storage Client Library with the Storage Emulator, a corresponding minimum version of the Azure Storage Emulator must be used. Older versions of the Storage Emulator do not have the necessary code to successfully respond to new requests.
- Currently, the minimum version of the Azure Storage Emulator needed for this library is 5.3. If you encounter a `VersionNotSupportedByEmulator` (400 Bad Request) error, please [update the Storage Emulator.](https://azure.microsoft.com/en-us/downloads/)

## Download & Install

The Storage Client Library ships with the Microsoft Azure SDK for .NET and also on NuGet. You'll find the latest version and hotfixes on NuGet via the `Azure.Storage` package. 

This version of the Storage Client Library ships with the storage version 2018-03-28.

### Via Git

To get the source code of the SDK via git just type:

```bash
git clone git://github.com/Azure/azure-storage-net.git
cd azure-storage-net
```

### Via NuGet

To get the binaries of this library as distributed by Microsoft, ready for use
within your project you can also have them installed by the .NET package manager [NuGet](https://www.nuget.org/packages/Azure.Storage/).

Please note that the minimum nuget client version requirement has been updated to 2.12 in order to support multiple netstandard targets in the nuget package.

`Install-Package Azure.Storage`

## Dependencies

### Newtonsoft Json

The libraries depend on Newtonsoft Json, which can be downloaded directly or referenced by your code project through Nuget.

- [Newtonsoft.Json](http://www.nuget.org/packages/Newtonsoft.Json)

### Key Vault

The client-side encryption support depends on the KeyVault.Core package, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Core](http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Core)

### Test Dependencies

FiddlerCore is required by:

- Test\FaultInjection\HttpMangler
- Test\FaultInjection\AzureStoreMangler
- Test\WindowsDesktop

This dependency is not included and must be downloaded from [http://www.telerik.com/fiddler/fiddlercore](http://www.telerik.com/fiddler/fiddlercore).

Once installed:

- Copy `FiddlerCore.dll` `\azure-storage-net\Test\FaultInjection\Dependencies\DotNet2`
- Copy `FiddlerCore4.dll` to `azure-storage-net\Test\FaultInjection\Dependencies\DotNet4`

Tests for the client-side encryption support also depend on KeyVault.Extensions, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Extensions](http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Extensions)

OAuth testing requires the ActiveDirectory identity model also available via NuGet:

- [IdentityModel.Clients.ActiveDirectory](http://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory)

## Code Samples

> Note:
> How-Tos focused around accomplishing specific tasks are available on the [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/).

## Need Help?
Be sure to check out the Microsoft Azure [Developer Forums on MSDN](http://go.microsoft.com/fwlink/?LinkId=234489) if you have trouble with the provided code or use StackOverflow.

## Collaborate & Contribute

We gladly accept community contributions.

- Issues: Please report bugs using the Issues section of GitHub
- Forums: Interact with the development teams on StackOverflow or the Microsoft Azure Forums
- Source Code Contributions: Please see [CONTRIBUTING.md](.github/CONTRIBUTING.md) for instructions on how to contribute code.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For general suggestions about Microsoft Azure please use our [UserVoice forum](http://feedback.azure.com/forums/34192--general-feedback).

# Learn More

- [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/)
- [Storage Client Library Reference for .NET - MSDN](http://msdn.microsoft.com/en-us/library/wa_storage_30_reference_home.aspx)
- [Azure Storage Team Blog](http://blogs.msdn.com/b/Azurestorage/)
