# Get It

.NET library to help introduce programming in a funnier way. Inspired by [Scratch](https://scratch.mit.edu/) and [Turtle graphics](https://en.wikipedia.org/wiki/Turtle_graphics).

[![Build Status](https://dev.azure.com/eggerhansi/GetIt/_apis/build/status/johannesegger.GetIt?branchName=develop)](https://dev.azure.com/eggerhansi/GetIt/_build/latest?definitionId=1&branchName=develop)
![Nuget](https://img.shields.io/nuget/v/GetIt.svg)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/GetIt.svg)

## Prerequisites

> Warning: The current version of `GetIt` only supports Windows >= 7.

* Setup a development environment, e.g. using the following script:

    ```powershell
    # 1
    iex (new-object net.webclient).downloadstring('https://get.scoop.sh')
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

## Setup a new project

* Open PowerShell or any other terminal and run the following commands to create a new C# project called `GetItStarted`, reference the latest stable version of `GetIt` and open the project with VSCode:

    ```powershell
    dotnet new console -o GetItStarted
    cd .\GetItStarted
    dotnet add package GetIt
    code .
    ```

* Open `Program.cs`, add `using GetIt;` to the list of `using` declarations at the top of the file and replace the contents of the `Main` method with `Game.ShowSceneAndAddTurtle();`
* Press F5, eventually configure how VSCode should launch the program and you should see a window with a turtle at its center.
* Congratulations, you just created and ran your first C# program with `GetIt`.

## Features

* For simplicity, the only namespace you need to open is `GetIt`.
* Functionality that is related to the whole game is contained in the static class `Game`.
* Functionality for the default player added by `Game.ShowSceneAndAddTurtle` can be accessed via the static class `Turtle`. All other players have extension methods with the same set of methods.
* Drawing
  * Every player has a pen that can be turned on or off, has a color and a weight. Type `Turtle.Pen` and VSCode shows a list of commands that do something with the pen.
* Speaking
  * Every player has a `Say` method as well as a `ShutUp` method. Note that there is no audio output but only a speech bubble next to the player.
* Asking
  * Every player can ask the user to answer questions. The player can ask for string or bool answers.
* Adding/removing players
  * Players can be added by calling `Game.AddPlayer`. `AddPlayer` takes a definition of a player of type `PlayerData` and adds it to the scene. You can add the same `PlayerData` multiple times (think of a template). The most important part of `PlayerData` is one or more costumes, which are vector graphics in [SVG](https://www.w3.org/Graphics/SVG/) format. While there are predefined players, you can also create players with simple shapes by using helper methods or create players with any complex costume by loading SVG files.
* Events
  * You can stop the program and wait for an event to occur (e.g. `Game.WaitForKeyDown`) as well as define callbacks that are invoked as soon as an event occurs (e.g. `Turtle.OnKeyDown`, `Turtle.WhileKeyDown`).
* Printing
  * With `Game.Print` you can print a screenshot of the current scene. The screenshot is included in an HTML file that is typically created by an instructor. The HTML file can contain additional placeholders to customize the printed file. The print config (location of the HTML template, printer name, placeholder values) can be loaded from the environment as well as created/extended before printing.
  * Note that `wkhtmltopdf` and `sumatrapdf` must be installed.

## Sample

![Sample](docs/sample.gif)

> Note that this uses an older version of `GetIt` where some commands have a different name.

## Credits

* Turtle icon created by [vectorportal.com](https://www.vectorportal.com).
* Ant, bug and spider icon created by [Vecteezy](https://www.vecteezy.com).
* Backgrounds exported from [Scratch](https://scratch.mit.edu).
