pushd %~dp0
call ..\Tools\nuget3.4.exe install Microsoft.NETCore.Runtime.CoreCLR -Version 1.1.0
cd ..\Lib\Facade.Split\Microsoft.Azure.Storage.Blob.Facade
del project.lock.json
call dotnet restore --source ..\packages
call dotnet build --configuration release
cd ..\..\NetStandard.Split\Microsoft.Azure.Storage.Blob
del project.lock.json
call dotnet restore --source ..\..\packages
call dotnet build --configuration release
popd
