---
title: Rainbow drawing
---

The next big topic we're going to discuss is *branching*.
As with loops, you already know branches from lots of other areas.
For example streets split up and you have to decide which branch you want to take.
Or in your career you can choose lots of different branches during education.

In software programs, branching is about executing different commands based on one or more conditions.
So as with loops we again need conditions that decide what branch is executed.

But let's first have a look at what we're going to build:

<video controls>
  <source src="rainbow-drawing.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

As you can see the pen color changes based on the X coordinate of the turtle.
In other words the X coordinate decides what pen-color-setting command is executed.

Follow the steps below to create the rainbow drawing app:

1. It's always good advice to first explain to the user of your program what is going to happen and what the user should do.

    ```csharp
    Turtle.Say("Let's create a rainbow drawing. I'm following your mouse.\r\nPress <Space> to start.");
    ```

1. We tell the user to press <kbd>Space</kbd> to start, so our program should wait for the space key to be pressed.

    ```csharp
    Game.WaitForKeyDown(KeyboardKey.Space);
    ```

1. After the user pressed <kbd>Space</kbd> we want the speech bubble to disappear.

    ```csharp
    Turtle.ShutUp();
    ```

1. We want the turtle to always draw, so let's turn on the pen and make the pen draw a bit thicker.

    ```csharp
    Turtle.TurnOnPen();
    Turtle.SetPenWeight(5);
    ```

1. Good. There are ready-to-use commands to follow the user's mouse, so let's use them.

    ```csharp
    Degrees direction = Turtle.GetDirectionToMouse();
    Turtle.SetDirection(direction);
    Turtle.MoveInDirection(5);
    ```

    > The above commands will move the turtle 5 steps closer to the mouse. If we repeat them, we get a smooth movement of the turtle:
    >
    > ```csharp
    > while (true)
    > {
    >     Degrees direction = Turtle.GetDirectionToMouse();
    >     Turtle.SetDirection(direction);
    >     Turtle.MoveInDirection(5);
    >
    >     Turtle.Sleep(25);
    > }
    > ```

1. There might have been some new commands, but overall this should have been straightforward so far. Now we need to do the branching, because we want to execute different commands based on the X coordinate of the turtle. Put the following at the beginning of the loop (in the line after `{`):

    ```csharp
    if (Turtle.Position.X < -250)
    {
        Turtle.SetPenColor(RGBAColors.Red);
    }
    ```

    > The C# keyword `if` needs a condition inside `(` and `)` and zero or more commands inside `{` and `}`. This is exactly the same as with the `while` loop, the only difference between `if` and `while` is that the commands of `if` are executed zero times or once, the commands of `while` are executed zero or more times.
    >
    > You can translate the above command to plain english: "If the turtle's X position is less than -250, set the pen color to red."

1. Additionally it's possible to execute another set of commands when the `if` condition is not met.

    ```csharp
    else
    {
        Turtle.SetPenColor(RGBAColors.Orange);
    }
    ```

    > The whole command can now be translated to: "If the turtle's X position is less than -250, set the pen color to red. Otherwise, set it to orange."
    >
    > While this is also possible using another `if` and the negated condition (`if (!(Turtle.Position.X < -250))` or simply `if (Turtle.Position.X >= -250)`), `else` is typically preferred because of less repetition.

1. The `else` branch above is not completely correct, we only want to use orange when the X coordinate is less than -150. After `else` we can use another `if` with a different condition:

    ```csharp
    else if (Turtle.Position.X < -150)
    {
        Turtle.SetPenColor(RGBAColors.Orange);
    }
    ```

    > The pen color is now set to orange if the X coordinate is between -250 (that condition comes from `else`) and -150.

1. Use the same logic to conditionally set the color to yellow, green and blue resp.
1. The last color purple should be set when all the other conditions don't apply, so we only need `else`.

    ```csharp
    else
    {
        Turtle.SetPenColor(RGBAColors.Purple);
    }
    ```

While `if` at first looks very similar to `while`, `else` and especially the use of `else if` bring in some peculiarities which you should get familiar with by playing around with the above example.
