@echo off
dotnet publish -r win-x64 -c Release ^
  -p:PublishDir=..\bin\ ^
  -p:TargetFramework=net8.0 ^
