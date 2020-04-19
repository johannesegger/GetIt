---
title: Steering wheel
---

In this challenge we're going to use the keyboard as steering wheel for the turtle:

<video controls>
  <source src="steering-wheel.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

Pressing (and holding) a key will trigger execution of different commands. In other words: If a particular key is pressed, one or more commands are executed.

Follow the steps below to create the steering wheel app:

1. Greet the user and tell her to press <kbd>Enter</kbd> to start.
1. Create a loop that runs forever. Inside the loop move the turtle 10 steps forward and sleep for 25 milliseconds.
1. Before those two commands, but still inside the loop, comes the logic to steer the turtle. We want to execute commands based on whether a key is pressed or not. To check if a particular key is pressed, you can use `Game.IsKeyDown(KeyboardKey.A)`. This command - in this case it's more like a question - returns `bool`, i.e. `true` or `false` which means we can use it directly as condition of an `if` statement.

    ```csharp
    if (Game.IsKeyDown(KeyboardKey.A))
    {
        // Insert commands that only run when 'A' is currently pressed.
    }
    ```

1. Rotate the turtle 5 degrees counter-clockwise if <kbd>A</kbd> is pressed.
1. Rotate the turtle 5 degrees clockwise if <kbd>D</kbd> is pressed.

    > Think about what happens when the user presses both <kbd>A</kbd> and <kbd>D</kbd>. Are both rotations executed or only one of them? What would you need to change to switch behavior?

1. You now have the most basic version of the program. Think of some extensions that you can add or use the following list as inspiration:

    * Draw when <kbd>Space</kbd> is pressed, stop drawing when <kbd>Space</kbd> is released.
    * Change the pen color when <kbd>W</kbd> is pressed.

        > Start with red and execute `Turtle.ShiftPenColor(1);` every time<kbd>W</kbd> is pressed.

    * Slow down the turtle when <kbd>S</kbd> is pressed. For example move 10 steps forward by default, but only 5 if <kbd>S</kbd> is pressed.
    * Slow the turtle even more down when both <kbd>S</kbd> and <kbd>J</kbd> are pressed.
