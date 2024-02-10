dotnet publish -c Release --self-contained -r win-x64 -o .\publish
vpk pack -u YourAppId -v 1.0.0 -p .\publish -e yourMainApp.exe