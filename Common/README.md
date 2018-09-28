# Microsoft Azure Storage Common SDK for .NET (9.4.0)

The Microsoft Azure Storage Common SDK for .NET is referenced by Azure Storage Blob/Queue/File SDKs and Azure CosmosDB Table SDK
and should not be referenced directly by your application.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

## Target Frameworks

- .NET Framework 4.5.2: As of December 2016, Storage Client Libraries for .NET supports primarily the desktop .NET Framework 4.5.2 release and above.
- Netstandard1.3: Storage Client Libraries for .NET are available to support Netstandard application development including Xamarin/UWP applications. 
- Netstandard2.0: Storage Client Libraries for .NET are available to support Netstandard2.0 application development including Xamarin/UWP applications. 

## Versioning Information

- The Storage Client Libraries use [the semantic versioning scheme.](http://semver.org/)


## Use with the Azure Storage Emulator

- The Client Library uses a particular Storage Service version. In order to use the Storage Client Library with the Storage Emulator, a corresponding minimum version of the Azure Storage Emulator must be used. Older versions of the Storage Emulator do not have the necessary code to successfully respond to new requests.
- Currently, the minimum version of the Azure Storage Emulator needed for this library is 5.4. If you encounter a `VersionNotSupportedByEmulator` (400 Bad Request) error, please [update the Storage Emulator.](https://azure.microsoft.com/en-us/downloads/)

## Download & Install

The Storage Client Library ships with the Microsoft Azure SDK for .NET and also on NuGet. 
You'll find the latest version and hotfixes on NuGet via the `Azure.Storage.Common` package.  

This version of the Storage Client Library ships with the storage version 2018-03-28.

## Dependencies


### Newtonsoft Json

The libraries depend on Newtonsoft Json, which can be downloaded directly or referenced by your code project through Nuget.

- [Newtonsoft.Json](http://www.nuget.org/packages/Newtonsoft.Json)

### Key Vault

The client-side encryption support depends on the KeyVault.Core package, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Core](http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Core)

Tests for the client-side encryption support also depend on KeyVault.Extensions, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Extensions](http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Extensions)

## Code Samples

> Note:
> How-Tos focused around accomplishing specific tasks are available on the [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/).

## Need Help?
Be sure to check out the Microsoft Azure [Developer Forums on MSDN](http://go.microsoft.com/fwlink/?LinkId=234489) if you have trouble with the provided code or use StackOverflow.

## Collaborate & Contribute

We gladly accept community contributions.

- Issues: Please report bugs using the Issues section of GitHub
- Forums: Interact with the development teams on StackOverflow or the Microsoft Azure Forums
- Source Code Contributions: Please see [CONTRIBUTING.md](CONTRIBUTING.md) for instructions on how to contribute code.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For general suggestions about Microsoft Azure please use our [UserVoice forum](http://feedback.azure.com/forums/34192--general-feedback).

# Learn More

- [Microsoft Azure .NET Developer Center](http://azure.microsoft.com/en-us/develop/net/)
- [Storage Client Library Reference for .NET - MSDN](http://msdn.microsoft.com/en-us/library/wa_storage_30_reference_home.aspx)
- [Azure Storage Team Blog](http://blogs.msdn.com/b/windowsazurestorage/)
