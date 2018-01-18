pushd %~dp0
call ..\Tools\nuget3.4.exe install Microsoft.NETCore.Runtime.CoreCLR -Version 1.1.0
cd ..\Lib\Facade.Split\Microsoft.Azure.Storage.File.Facade
del project.lock.json
call dotnet restore
call dotnet build --configuration release
cd ..\..\NetStandard.Split\Microsoft.Azure.Storage.File
del project.lock.json
call dotnet restore
call dotnet build --configuration release
popd
