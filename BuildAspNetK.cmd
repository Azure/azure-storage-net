pushd %~dp0
call Tools\nuget3.4.exe install Microsoft.NETCore.Runtime.CoreCLR -Version 1.0.2-rc2-24027 -Prerelease
call dotnet restore
cd Lib\AspNet\Microsoft.WindowsAzure.Storage
call dotnet build --configuration release
popd
