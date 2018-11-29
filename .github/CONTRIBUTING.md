If you intend to contribute to the project, please make sure you've reviewed the [Microsoft Open Source Community Resources][resources].

## Project Setup
The Azure Storage development team uses Visual Studio so instructions will be tailored to that preference. However, any preferred IDE or other toolset should be usable.

### Install
* .NET 4.5.2 or later
* [Visual Studio 2017][visual studio] or later. Use of Visual Studio 2015 and earlier has been deprecated.
* [StyleCop][] checks your code’s style – spacing, comments, etc. You can run this from the Tools menu in Visual Studio or by right clicking the project/folder you’d like to run it on.
* Clone the source code from [GitHub][azure-storage-net].

### Open Solution
You can open the project from VS using File->Open->Project/Solution and navigating to the solution file in the repo base folder:
* Microsoft.Azure.Storage.sln

## Tests

### Configuration
Add a TestConfigurations.xml to the [Test/Common/][test-common] folder. You should insert your storage account information into the file using [this][testconfigurationstemplate] as a template.
You will also need to download [Fiddler][] to get the files needed to run tests. Fiddler dll & xml files for .Net2 & .Net4 should be placed in their respective folders (Test/FaultInjection/Dependencies/DotNet2, Test/FaultInjection/Dependencies/DotNet4).

### Running
To actually run tests, right click the individual test or test class in the Test Explorer panel.

> Note:
> Running all tests will take many hours, so you should run a subset of tests that validate your change.

### Testing Features
As you develop a feature, you'll need to write tests to ensure quality. You should also run existing tests related to your change to address any unexpected breaks. Please do so for each platform in the solution.

## Pull Requests

### Guidelines
The following are the minimum requirements for any pull request that must be met before contributions can be accepted.
* Make sure you've signed the CLA before you start working on any change.
* Discuss any proposed contribution with the team via a GitHub issue **before** starting development.
* Code must be professional quality
	* No style issues, StyleCop doesn't report any *new* issues related to your changes
	* You should strive to mimic the style with which we have written the library
	* Clean, well-commented, well-designed code
	* Try to limit the number of commits for a feature to 1-2. If you end up having too many we may ask you to squash your changes into fewer commits.
* The Changelog.txt of each affected project needs to be updated describing the new change
* Thoroughly test your feature

### Branching Policy
Non-breaking changes should be based on the dev branch whereas breaking changes should be based on the dev_breaking branch. Each breaking change should be recorded in the appropriate BreakingChanges.txt file.
We generally release any breaking changes in the next major version (e.g. 6.0, 7.0) and non-breaking changes in the next minor or major version (e.g. 6.0, 6.1, 6.2).

### Adding Features for All Platforms
We strive to release each new feature for each of our environments at the same time. Therefore, we ask that all contributions be written for both our Desktop SDK and our .NET Core SDKs. This includes testing work for all each platform as well.

### Review Process
We expect all guidelines to be met before accepting a pull request. As such, we will work with you to address issues we find by leaving comments in your code. Please understand that it may take a few iterations before the code is accepted as we maintain high standards on code quality. Once we feel comfortable with a contribution, we will validate the change and accept the pull request.

Thank you for any contributions! Please let the team know if you have any questions or concerns about our contribution policy.

[resources]: https://opensource.microsoft.com/resources
[StyleCop]: https://github.com/StyleCop
[Visual Studio]: https://visualstudio.microsoft.com/
[Fiddler]: https://www.telerik.com/download/fiddler
[azure-storage-net]: https://github.com/Azure/azure-storage-net/
[testconfigurationstemplate]: ../Test/Common/TestConfigurationsTemplate.xml
[test-common]: ../Test/Common/