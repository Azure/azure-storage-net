pushd %~dp0
call Tools\nuget3.4.exe install Microsoft.NETCore.Runtime.CoreCLR -Version 1.1.0
call dotnet restore
cd Lib\AspNet\Microsoft.WindowsAzure.Storage
call dotnet build --configuration release
cd Lib\AspNet\Microsoft.WindowsAzure.Storage.Facade
call dotnet build --configuration release
popd
