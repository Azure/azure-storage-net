# Microsoft Azure Storage SDK for .NET (10.0.0)

> Server Version: 2018-11-09

The Microsoft Azure Storage SDK for .NET allows you to build Azure applications 
that take advantage of scalable cloud computing resources.

This repository contains the open source subset of the .NET SDK. For documentation of the 
complete Azure SDK, please see the [Microsoft Azure .NET Developer Center][].

> Note:
> As of 10.0.0, the namespace has changed to Microsoft.Azure.Storage.Common, .Blob, .File, and .Queue.
> This is required for some SxS scenarios.

> Note:
> As of 9.4.0, the Table service is not supported by this library.  
> Table support is being provided by [CosmosDB][Microsoft.Azure.Cosmos.Table].

## Features

- Blobs [(Change Log)][blob-changelog]
    - Create/Read/Update/Delete Blobs
- Files [(Change Log)][file-changelog]
    - Create/Update/Delete Directories
    - Create/Read/Update/Delete Files
- Queues [(Change Log)][queue-changelog]
    - Create/Delete Queues
    - Insert/Peek Queue Messages
    - Advanced Queue Operations

## Getting Started

The complete Microsoft Azure SDK can be downloaded from the [Microsoft Azure Downloads Page][] and ships with support for building deployment packages, integrating with tooling, rich command line tooling, and more.

Please review [Get started with Azure Storage][] if you are not familiar with Azure Storage.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

- NuGet packages for [Blob][], [File][], [Queue][]
- [Azure Storage APIs for .NET][]
- Quickstart for [Blob][blob-quickstart], [File][file-quickstart], [Queue][queue-quickstart]

## Target Frameworks

- .NET Framework 4.5.2: As of September 2018, Storage Client Libraries for .NET supports primarily the desktop .NET Framework 4.5.2 release and above.
- Netstandard1.3: Storage Client Libraries for .NET are available to support Netstandard application development including Xamarin/UWP applications. 
- Netstandard2.0: Storage Client Libraries for .NET are available to support Netstandard2.0 application development including Xamarin/UWP applications. 

## Requirements

- Microsoft Azure Subscription: To call Microsoft Azure services, you need to first [create an account][]. Sign up for a free trial or use your MSDN subscriber benefits.
- Hosting: To host your .NET code in Microsoft Azure, you additionally need to download the full Microsoft Azure SDK for .NET - which includes packaging,
    emulation, and deployment tools, or use Microsoft Azure Web Sites to deploy ASP.NET web applications.

## Versioning Information

- The Storage Client Libraries use [the semantic versioning scheme][semver]

## Use with the Azure Storage Emulator

- The Client Libraries use a particular Storage Service version. In order to use the Storage Client Libraries with the Storage Emulator, a corresponding minimum version of the Azure Storage Emulator must be used. Older versions of the Storage Emulator do not have the necessary code to successfully respond to new requests.
- Currently, the minimum version of the Azure Storage Emulator needed for this library is 5.3. If you encounter a `VersionNotSupportedByEmulator` (400 Bad Request) error, please [update the Storage Emulator.][emulator]

## Download & Install

The Storage Client Libraries ship with the Microsoft Azure SDK for .NET and also on NuGet. You'll find the latest version and hotfixes on NuGet via the `Microsoft.Azure.Storage.Blob`, `Microsoft.Azure.Storage.File`, `Microsoft.Azure.Storage.Queue`, and `Microsoft.Azure.Storage.Common` packages. 

### Via Git

To get the source code of the SDK via git just type:

```bash
git clone git://github.com/Azure/azure-storage-net.git
cd azure-storage-net
```

### Via NuGet

To get the binaries of this library as distributed by Microsoft, ready for use
within your project you can also have them installed by the .NET package manager: [Blob][], [File][], [Queue][].

Please note that the minimum NuGet client version requirement has been updated to 2.12 in order to support multiple .NET Standard targets in the NuGet package.

```
Install-Package Microsoft.Azure.Storage.Blob
Install-Package Microsoft.Azure.Storage.File
Install-Package Microsoft.Azure.Storage.Queue
```

The `Microsoft.Azure.Storage.Common` package should be automatically entailed by NuGet.

## Dependencies

### Newtonsoft Json

The libraries depend on Newtonsoft.Json, which can be downloaded directly or referenced by your code project through Nuget.

- [Newtonsoft.Json][]

### Key Vault

The client-side encryption support depends on the KeyVault.Core package, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Core][]

### Test Dependencies

#### FiddlerCore

FiddlerCore is required by:

- Test\FaultInjection\HttpMangler
- Test\FaultInjection\AzureStoreMangler
- Microsoft.Azure.Storage.Test.NetFx
- Microsoft.Azure.Storage.Test.NetCore2

This dependency is not included and must be downloaded from [Telerik][FiddlerCore].

Once obtained:

- Copy `FiddlerCore.dll` to `Test\FaultInjection\Dependencies\DotNet2`
- Copy `FiddlerCore4.dll` to `Test\FaultInjection\Dependencies\DotNet4`

#### Key Vault

Tests for the client-side encryption support also depend on KeyVault.Extensions, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Extensions][]

#### ActiveDirectory

OAuth testing requires the ActiveDirectory identity model also available via NuGet:

- [IdentityModel.Clients.ActiveDirectory][]

## Code Samples

How-Tos focused around accomplishing specific tasks are available on the [Microsoft Azure .NET Developer Center][].

## Need Help?
Be sure to check out the [Azure Community Support][] page if you have trouble with the provided code or use StackOverflow.

## Collaborate & Contribute

We gladly accept community contributions.

- Issues: Please report bugs using the Issues section of GitHub
- Forums: Interact with the development teams on StackOverflow or the Microsoft Azure Forums
- Source Code Contributions: Please see [CONTRIBUTING.md][contributing] for instructions on how to contribute code.

This project has adopted the [Microsoft Open Source Code of Conduct][code of conduct]. For more information see the [Code of Conduct FAQ][] or contact [opencode@microsoft.com][opencode-email] with any additional questions or comments.

For general suggestions about Microsoft Azure please use our [UserVoice forum][].

# Learn More

- [Microsoft Azure .NET Developer Center][]
- [Azure Storage APIs for .NET][]
- [Azure Storage Team Blog][blog]
- [Azure Management Libraries for CRUD Storage Accounts][azure-sdk-for-net]


[contributing]: .github/CONTRIBUTING.md
[code of conduct]: https://opensource.microsoft.com/codeofconduct/
[code of conduct faq]: https://opensource.microsoft.com/codeofconduct/faq/
[opencode-email]: mailto:opencode@microsoft.com
[UserVoice forum]: http://feedback.azure.com/forums/34192--general-feedback
[blog]: https://azure.microsoft.com/en-us/blog/topics/storage-backup-and-recovery/

[Azure Storage APIs for .NET]: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage?view=azure-dotnet
[Microsoft Azure .NET Developer Center]: http://azure.microsoft.com/en-us/develop/net/
[Azure Community Support]: http://go.microsoft.com/fwlink/?LinkId=234489
[Microsoft Azure Downloads Page]: http://azure.microsoft.com/en-us/downloads/?sdk=net
[Get started with Azure Storage]: https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-blobs
[azure-sdk-for-net]: https://github.com/Azure/azure-sdk-for-net
[create an account]: https://account.Azure.com/Home/Index
[semver]: http://semver.org/
[emulator]: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator

[blob-changelog]: Blob/Changelog.txt
[file-changelog]: File/Changelog.txt
[queue-changelog]: Queue/Changelog.txt

[blob-quickstart]: https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
[file-quickstart]: https://docs.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files
[queue-quickstart]: https://docs.microsoft.com/en-us/azure/storage/queues/storage-dotnet-how-to-use-queues

[Blob]: https://www.nuget.org/packages/Microsoft.Azure.Storage.Blob/
[File]: https://www.nuget.org/packages/Microsoft.Azure.Storage.File/
[Queue]: https://www.nuget.org/packages/Microsoft.Azure.Storage.Queue/
[WindowsAzure.Storage]: https://www.nuget.org/packages/WindowsAzure.Storage/
[Microsoft.Azure.Cosmos.Table]: https://www.nuget.org/packages/Microsoft.Azure.Cosmos.Table

[Newtonsoft.Json]: https://www.nuget.org/packages/Newtonsoft.Json/
[IdentityModel.Clients.ActiveDirectory]: https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/
[KeyVault.Core]: https://www.nuget.org/packages/Microsoft.Azure.KeyVault.Core/
[KeyVault.Extensions]: https://www.nuget.org/packages/Microsoft.Azure.KeyVault.Extensions/

[FiddlerCore]: http://www.telerik.com/fiddler/fiddlercore
