---
title: Features
---

Here is a summary of what you can do with *Get It* and some hints on how to use it.

* For simplicity, the only `using` declaration you need to add is `using GetIt;`.
* Functionality that is related to the whole game is contained in the static class `Game`.
* Functionality for the default player added by `Game.ShowSceneAndAddTurtle` and similar can be accessed via the static class `Turtle`. All other players have extension methods with the same set of methods.
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
