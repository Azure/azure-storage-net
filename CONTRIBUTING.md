If you intend to contribute to the project, please make sure you've followed the instructions provided in the [Azure Projects Contribution Guidelines](http://azure.github.io/guidelines/).
## Project Setup
The Azure Storage development team uses Visual Studio so instructions will be tailored to that preference. However, any preferred IDE or other toolset should be usable.

### Install
* .Net 4.0
* Visual Studio 2013. If you want to work with the CoreCLR SDK, you will need Visual Studio 2015. Note not all of the non-CoreCLR SDKs work in VS 2015, so you may need to develop with both 2013 and 2015 if you take a dependency on multiple runtimes.
* [StyleCop](http://stylecop.codeplex.com/) checks your code’s style – spacing, comments, etc. You can run this from the Tools menu in Visual Studio or by right clicking the project/folder you’d like to run it on.
* Clone the source code from GitHub.

### Open Solution
For non-CoreCLR solutions, you can open the project from VS using File->Open->Project/Solution and navigating to the Microsoft.WindowsAzure.Storage.sln solution file in the repo base folder.
For the CoreCLR solution, you can open the project using the NETCORE.sln solution in the repo base folder.

## Tests

### Configuration
Add a TestConfigurations.xml to the Test/Common/ folder. You should insert your storage account information into the file using [this](Test/Common/TestConfigurationsTemplate.xml) as a template.
You will also need to download [Fiddler 2 & 4](https://www.telerik.com/download/fiddler) to get the files needed to run tests. Fiddler dll & xml files for .Net2 & .Net4 should be placed in their respective folders (Test/FaultInjection/Dependencies/DotNet2, Test/FaultInjection/Dependencies/DotNet4).

### Running
To actually run tests, right click the individual test or test class in the Test Explorer panel.
*Note*: Running all tests will take many hours, so you should run a subset of tests that validate your change.

### Testing Features
As you develop a feature, you'll need to write tests to ensure quality. You should also run existing tests related to your change to address any unexpected breaks.

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
* [changelog.txt](changelog.txt) needs to be updated describing the new change
* Thoroughly test your feature

### Branching Policy
Non-breaking changes should be based on the dev branch whereas breaking changes should be based on the dev_breaking branch. Each breaking change should be recorded in [BreakingChanges.txt](BreakingChanges.txt).
We generally release any breaking changes in the next major version (e.g. 6.0, 7.0) and non-breaking changes in the next minor or major version (e.g. 6.0, 6.1, 6.2).

### Adding Features for All Platforms
We strive to release each new feature for each of our environments at the same time. Therefore, we ask that all contributions be written for both our Desktop SDK and our Window Runtime/CoreCLR SDK. This includes testing work for both platforms as well.

### Review Process
We expect all guidelines to be met before accepting a pull request. As such, we will work with you to address issues we find by leaving comments in your code. Please understand that it may take a few iterations before the code is accepted as we maintain high standards on code quality. Once we feel comfortable with a contribution, we will validate the change and accept the pull request.


Thank you for any contributions! Please let the team know if you have any questions or concerns about our contribution policy.