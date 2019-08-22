---
title: Ask for position
---

In this challenge you'll learn about data types -- what they are, why we need them and how to convert between different data types.

1. We want to move our turtle to a user-defined position. So we have to ask the user for the x and y coordinate. Add a command `Turtle.Ask("Hi! Where should I move?\r\nX coordinate:")` and store the result in a new variable `xCoordinateText`.
    > `\r\n` is a [line break](https://en.wikipedia.org/wiki/Newline){:target="_blank"} -- similar to pressing <kbd>Enter</kbd> in a text document.
1. Run the program, enter a number when the turtle asks you for the x coordinate and press <kbd>Enter</kbd> to confirm your input.
    > Note that you can input any text, not just numbers. `"53"`, `"-163.4"` or `"slightly right"` are all possible inputs that might be stored in `xCoordinateText`. So before we can use the users input as number we have to make sure it actually is a number. We do this by *parsing* the input as number.
1. Define a new variable `xCoordinate` and store `double.Parse(xCoordinateText)` in it. The type of values `xCoordinate` can store is not `string` but `double`.
    > `double` means *real number with [**double precision**](https://en.wikipedia.org/wiki/Double-precision_floating-point_format){:target="_blank"}*. Most of the time you don't really care about the precision and just use *double*.

    > `double.Parse` is a command that checks if a given text is a real number. If it is, it returns the number as result. If not, it crashes the program (until we talk about error handling). At first it seems to be weird that `double.Parse` returns the number although we already have it. The thing is that with `xCoordinateText` we can't do any calculations. You can't add another number to it, you can't multiply it by another number, you can't do any arithmetic operation. This is because the variable is defined to be able to store *all sorts of text* (type of data is `string`). `xCoordinate` on the other hand is defined to store real numbers only (data type is `double`). And that's why we can later use it to move the turtle.
1. Repeat the previous steps to read and parse the y coordinate.
1. Use `xCoordinate` and `yCoordinate` to move the turtle to this position.
