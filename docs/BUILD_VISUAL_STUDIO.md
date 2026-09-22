# Visual Studio Build Guide

Open this solution in Visual Studio 2022:

```text
ProcessVideoAnalyzer.sln
```

Required Visual Studio Installer workload:

```text
.NET desktop development
```

Required components:

```text
.NET 8 SDK
Windows Presentation Foundation
NuGet package manager
```

Build settings:

```text
Configuration: Debug or Release
Platform: x64
```

The project targets:

```text
net8.0-windows
```

The application uses WebView2 and OpenCvSharp. NuGet restore must complete before build.

Command line build:

```cmd
build_vs.cmd
```

Manual command:

```cmd
dotnet restore ProcessVideoAnalyzer.sln
dotnet build ProcessVideoAnalyzer.sln -c Debug -p:Platform=x64
```

Publish command:

```cmd
dotnet publish src\ProcessVideoAnalyzer\ProcessVideoAnalyzer.csproj -c Release -r win-x64 --self-contained true
```

If `dotnet --list-sdks` does not show an 8.x SDK, the project cannot build on that PC even if the .NET runtime exists.
