# Microsoft Azure Storage File SDK for .NET (8.7.0-preview)

The Microsoft Azure Storage File SDK for .NET allows you to build Azure applications that take advantage of scalable cloud computing resources.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

## Features

- Files
    - Create/Update/Delete Directories
    - Create/Read/Update/Delete Files

## Getting Started

The complete Microsoft Azure SDK can be downloaded from the [Microsoft Azure Downloads Page](http://azure.microsoft.com/en-us/downloads/?sdk=net) and ships with support for building deployment packages, integrating with tooling, rich command line tooling, and more.

Please review [Get started with Azure Storage](https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-Files) if you are not familiar with Azure Storage.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

## Target Frameworks

- .NET Framework 4.5: As of December 2016, Storage Client Libraries for .NET supports primarily the desktop .NET Framework 4.5.0 release and above.
- Windows 8.1 for Windows Store app development: Storage Client Libraries are available for Windows Store applications.
- Windows Phone 8.1 app development: Storage Client Libraries are available for Windows Phone applications including Universal applications.
- Netstandard1.3: Storage Client Libraries for .NET are available to support Netstandard application development including Xamarin/UWP applications. 
- Netstandard1.0: Storage Client Libraries support PCL through a Netstandard Façade targeting netstandard1.0.

### Netstandard1.0 (Façade)

As the lowest TFM supported by all our implementations, 1.0 is selected to provide support for maximum platforms. The support is provided through a façade reference assembly targeting netstandard1.0. This assembly consists of a common set of APIs between Win8 and Wpa with no API implementations.
Through the bait and switch technique, the reference assembly enables other portable class libraries to reference Storage Client Library, while the correct implementation assembly will be picked when the package is referenced by the project.json file.


## Versioning Information

- The Storage Client Libraries use [the semantic versioning scheme.](http://semver.org/)

## Download & Install

You'll find the latest version and hotfixes on NuGet via the `Azure.Storage.File` package. 

This version of the Storage Client Library File package ships with the storage version 2017-04-17.

### Via Git

To get the source code of the SDK via git just type:

```bash
git clone git://github.com/Azure/azure-storage-net.git
cd azure-storage-net
```

### Via NuGet

To get the binaries of this library as distributed by Microsoft, ready for use
within your project you can also have them installed by the .NET package manager [NuGet](https://www.nuget.org/packages/Microsoft.Azure.Storage.File/).

Please note that the minimum nuget client version requirement has been updated to 2.12 in order to support multiple netstandard targets in the nuget package.

`Install-Package Microsoft.Azure.Storage.File`

## Dependencies

### Newtonsoft Json

The desktop and phone libraries depend on Newtonsoft Json, which can be downloaded directly or referenced by your code project through Nuget.

- [Newtonsoft.Json] (http://www.nuget.org/packages/Newtonsoft.Json)

### Key Vault

The client-side encryption support depends on the KeyVault.Core package, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Core] (http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Core)

Tests for the client-side encryption support also depend on KeyVault.Extensions, which can be downloaded directly or referenced by your code project through Nuget.

- [KeyVault.Extensions] (http://www.nuget.org/packages/Microsoft.Azure.KeyVault.Extensions)

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
