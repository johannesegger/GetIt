---
title: Setting up a new project
---

Everytime you create a new C# project with *Get It* you need to run some commands to setup the project.
This will be the same procedure everytime.
So while you can copy and paste the following instructions, I find it better to type them by hand.
After setting up a few projects you won't need the help anymore.

* Open PowerShell or any other terminal and run the following commands to create a new C# project called `GetItStarted`, reference the latest stable version of `GetIt` and open the project with VSCode:

    ```powershell
    dotnet new console -o GetItStarted
    cd .\GetItStarted
    dotnet add package GetIt
    code .
    ```

* Open `Program.cs`, add `using GetIt;` to the list of `using` declarations at the top of the file and replace `Console.WriteLine("Hello World!");` with `Game.ShowSceneAndAddTurtle();`
* Press <kbd>F5</kbd> to launch the program and you should see a window with a turtle at its center.
  * If VSCode asks you to select an environment, choose `.NET Core`.
* Congratulations, you just created and ran a brand new C# program with *Get It*.
