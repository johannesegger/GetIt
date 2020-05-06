---
title: Draw staircase
---

This challenge will show you how to use finite loops -- loops that don't go on forever.
We'll create the following program that draws a staircase:

<video controls>
    <source src="staircase.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

1. Turn on the pen and set an awesome color.
1. Draw a single step of a staircase by moving the turtle 50 steps up and 80 steps to the right.
    > It's important to use commands that move the turtle relative to its current position, like `Turtle.MoveUp` and `Turtle.MoveRight`.
1. Define a variable `steps` before drawing the step and store the number of steps the turtle should draw in it.
    > Use `int` as data type as it best models natural numbers.
1. Define a variable `drawnSteps` before drawing the step and store `0` in it.
1. Put the commands that draw the step inside a `while` loop and use `drawnSteps < steps` as the condition of the loop.
1. After drawing each step, increase `drawnSteps` by 1 using `drawnSteps = drawnSteps + 1;`.
    > The program first evaluates the expression on the right side of the assignment operator `=` and then stores the result into the variable on the left side. In C# this line is called *assignment statement*.
    >
    > Run the program with different values for `steps`. The following table should help you to understand what exactly is going on when `steps` is `3` at the beginning.
    >
   | `drawnSteps` | Condition in while loop |
   | ------------ | ----------------------- |
   | 0            | `0 < 3` / `true`        |
   | 1            | `1 < 3` / `true`        |
   | 2            | `2 < 3` / `true`        |
   | 3            | `3 < 3` / `false`       |
    >
    > Remember that the loop is exited as soon as the condition evaluates to `false`. Commands *after* the loop are then executed.
1. Close the staircase by moving the turtle down and then to the left.
    > These commands have to be *after* the loop, because you want to draw them after drawing *all* steps.

    > Try to calculate the correct distance by considering `steps`.
1. Move the turtle to a position where it doesn't hide the staircase and tell the user that the staircase is finished.
    > Ensure that no line is drawn for this movement.
1. Change the starting position of the staircase so that the drawing is horizontally and vertically centered at the scene by considering `steps`.
