---
title: Prerequisites
---

Before you can start to create projects with *Get It* you need to setup a proper development environment, which basically means installing an advanced text editor and the tools to build and run your projects. To do so open Windows PowerShell and run the following script:

```powershell
# 1
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
Invoke-Expression (New-Object System.Net.WebClient).DownloadString('https://get.scoop.sh')
scoop install git
scoop bucket add extras

# 2
scoop install vscode

# 3
code --install-extension ms-vscode.csharp

#4
scoop install dotnet-sdk

#5 (optional)
scoop install wkhtmltopdf
scoop install sumatrapdf
```

1. Install the fabulous package manager [scoop](https://scoop.sh/). Note that scoop currently needs Windows PowerShell 5 or later and .NET Framework 4.5 or later.
1. Install [VSCode](https://code.visualstudio.com/), a universal development environment that is just awesome.
1. Install an extension for VSCode that helps with writing C# programs.
1. Install the [.NET Core SDK](https://www.microsoft.com/net/) which basically contains all you need to run C# programs.
1. If you intend to print screenshots of your programs you'll need `wkhtmltopdf` and `sumatrapdf` installed.
