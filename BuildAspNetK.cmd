pushd %~dp0
call Tools\nuget.exe install dnx-coreclr-win-x86 -Version 1.0.0-rc1-final -Prerelease
call Tools\nuget.exe install dnx-clr-win-x86 -Version 1.0.0-rc1-final -Prerelease
call dnx-coreclr-win-x86.1.0.0-rc1-final\bin\dnu restore
call dnx-clr-win-x86.1.0.0-rc1-final\bin\dnu restore
cd Lib\AspNet\Microsoft.WindowsAzure.Storage
call ..\..\..\dnx-coreclr-win-x86.1.0.0-rc1-final\bin\dnu build --configuration release
popd
