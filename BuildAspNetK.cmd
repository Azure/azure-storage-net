pushd %~dp0
call Tools\nuget.exe install KRE-CoreCLR-x86 -Version 1.0.0-beta1 -Prerelease
call Tools\nuget.exe install KRE-CLR-x86 -Version 1.0.0-beta1 -Prerelease
call KRE-CoreCLR-x86.1.0.0-beta1\bin\kpm restore
call KRE-CLR-x86.1.0.0-beta1\bin\kpm restore
cd Lib\AspNet\Microsoft.WindowsAzure.Storage
call ..\..\..\KRE-CoreCLR-x86.1.0.0-beta1\bin\kpm build --configuration release
popd
