---
title: Draw letter
---

This challenge will show you some more commands for moving around the turtle and some more capabilities of the turtle's pen.

1. Move the turtle to (0, -100).
    > Remember that *(0, -100)* denotes a 2D coordinate where the first number is the X position and the second number is the Y position. (0, 0) is at the center of the scene. Negative coordinates are on the left or bottom side of the scene, positive coordinates on the right or top side.

    > *Get It* offers multiple commands to move around the turtle. While it's always up to you to use the command that best suits your needs, take a look at `Turtle.MoveTo`. It simply moves the turtle to an absolute position. The command expects two parameters -- the X and Y coordinate of the position. Commands with multiple parameters are invoked by separating the arguments with a comma `,`. `Turtle.MoveTo(0, -100);` for example moves the turtle to (0, -100).
1. Turn on the pen.
    > If you don't remember how this works, simply start the command by typing `Turtle.` and let VSCode help you.
1. The turtles pen is much more powerful than a traditional pen. For example you can set its weight by invoking `Turtle.SetPenWeight`. Also you can change its color by using `Turtle.SetPenColor`. Experiment with both commands.
    > The value you have to provide to `Turtle.SetPenColor` must be a color. The simplest way to get a color is to use a predefined one. Simply choose one from the list you get when typing `RGBAColors.`. The full command to change the pen color could be something like `Turtle.SetPenColor(RGBAColors.SteelBlue);`.
1. Set the Y coordinate of the turtle to 200.
    > Again there are multiple possibilities, e.g. `Turtle.MoveBy`, `Turtle.MoveUp`, `Turtle.MoveTo`.
1. Move the turtle 100 steps to the left and then 200 steps to the right.
1. Turn off the pen and move to the center of the scene (e.g. with `Turtle.MoveToCenter`).
1. What letter does the turtle write?
    > Try to answer it by only looking at the commands. Then verify your answer by running the program.
1. At the end of the program insert a three-second break and then clear all the pen lines using `Game.ClearScene`.
1. Now draw a letter of your choice, e.g. the first letter of your name.
    > While you don't have to draw perfect curves, the letter should be readable and look neat, e.g. symmetric.
