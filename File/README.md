# Microsoft Azure Storage File SDK for .NET (11.0.1)

The Microsoft Azure Storage File SDK for .NET allows you to build Azure applications that take advantage of scalable cloud computing resources.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

_For general information pertaining to all services, please see the top-level [README.md][readme-main] file in this repository._

## Features

- Files [(Change Log)][changelog]
    - Create/Update/Delete Directories
    - Create/Read/Update/Delete Files

## Getting Started

The complete Microsoft Azure SDK can be downloaded from the [Microsoft Azure Downloads Page][] and ships with support for building deployment packages, integrating with tooling, rich command line tooling, and more.

Please review [Get started with Azure Storage][] if you are not familiar with Azure Storage.

For the best development experience, developers should use the official Microsoft NuGet packages for libraries. NuGet packages are regularly updated with new functionality and hotfixes. 

- NuGet package for [File][]
- [Azure Storage APIs for .NET][]
- Quickstart for [File][file-quickstart]

## Download & Install

The Storage Client Libraries ship with the Microsoft Azure SDK for .NET and also on NuGet. 
You'll find the latest version and hotfixes on NuGet via the `Microsoft.Azure.Storage.File` package.

### Via Git

To get the source code of the SDK via git just type:

```bash
git clone git://github.com/Azure/azure-storage-net.git
cd azure-storage-net
```

### Via NuGet

To get the binaries of this library as distributed by Microsoft, ready for use
within your project you can also have them installed by the .NET package manager [NuGet][file].

Please note that the minimum NuGet client version requirement has been updated to 2.12 in order to support multiple .NET Standard targets in the NuGet package.

```
Install-Package Microsoft.Azure.Storage.File
```

The `Microsoft.Azure.Storage.Common` package should be automatically entailed by NuGet.

## Code Samples

How-Tos focused around accomplishing specific tasks are available on the [Microsoft Azure .NET Developer Center][].


[changelog]: Changelog.txt
[readme-main]: ../README.md
[Microsoft Azure Downloads Page]: http://azure.microsoft.com/en-us/downloads/?sdk=net
[Get started with Azure Storage]: https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-blobs
[Azure Storage APIs for .NET]: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage?view=azure-dotnet
[Microsoft Azure .NET Developer Center]: http://azure.microsoft.com/en-us/develop/net/
[File]: https://www.nuget.org/packages/Microsoft.Azure.Storage.File/
[file-quickstart]: https://docs.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files
