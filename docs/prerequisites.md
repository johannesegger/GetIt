---
title: Prerequisites
---

Before you can start to create projects with *Get It* you need to setup a proper development environment, which basically means installing an advanced text editor and the tools to build and run your projects. To do so open a command terminal (e.g. Windows Terminal) as administrator and run the following script:

```powershell
# 1
winget install Microsoft.VisualStudioCode # on Windows
brew install --cask visual-studio-code # on macOS

# 2
code --install-extension ms-dotnettools.csdevkit

# 3
winget install Microsoft.DotNet.SDK.8 # on Windows
brew install --cask dotnet-sdk # on macOS

# 4 (optional, Windows only)
winget install wkhtmltopdf.wkhtmltox
winget install SumatraPDF.SumatraPDF
```

1. Install [VSCode](https://code.visualstudio.com/), a universal development environment.
1. Install an extension for VSCode that helps with writing C# programs.
1. Install the [.NET SDK](https://www.microsoft.com/net/) which basically contains all you need to run your own C# programs.
1. If you intend to print screenshots of your programs you'll need `wkhtmltopdf` and `sumatrapdf` installed.
