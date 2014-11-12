pushd %~dp0
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/release/kvminstall.ps1'))"
call %userprofile%\.kre\bin\kvm.cmd upgrade -x86 -r CoreCLR
call %userprofile%\.kre\bin\kvm.cmd upgrade -x86 -r CLR
call %userprofile%\.kre\bin\kvm.cmd use 1.0.0-beta1-10674 -r CLR
call kpm restore
call %userprofile%\.kre\bin\kvm.cmd use 1.0.0-beta1-10674 -r CoreCLR
cd Lib\AspNet\Microsoft.WindowsAzure.Storage
call kpm build --configuration release
popd
