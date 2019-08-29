---
title: Calculator
---

This challenge is all about [*arithmetic*](https://en.wikipedia.org/wiki/Arithmetic){:target="_blank"}. While most of it will be familiar from maths, in programming there are some odds and ends that we need to talk about.

1. Insert a command that lets the turtle show a simple calculation: `Turtle.Say("5 + 2 = 7");`.
    > That's good, but we don't learn C# to do calculations by hand, do we?
1. Change the command so that the program does the calculation: `Turtle.Say($"5 + 2 = {5 + 2}")`.
    > The first time we used string interpolation we simply put a variable inside the curly braces `{` and `}`. But in fact we can put *any expression* inside the curly braces -- the result of the expression is then interpolated into the string.
1. In C# we can do all four basic arithmetic operations: addition (`+`), subtraction (`-`), multiplication (`*`) and division (`/`). Try them all and see if they work as expected.
    > Add `Game.WaitForMouseClick();` after every command. Otherwise the turtle would have said everything in a blink of a second. This command pauses execution until you click somewhere at the scene.
1. Check if operator precedence rules apply. Is multiplication and division calculated before addition and subtraction?
    > You can even change the precedence by using parentheses `(` and `)`.
1. That's nice, isn't it? No surprises so far. Try some calculations with real numbers as well.
    > Notice that C# uses a dot `.` as decimal separator. Depending on your origin this might be odd.
1. Everything seems to work fine, right? Just out of curiosity try to calculate `17 / 7` and check the result.
    > Wait, what just happend? That can't be right.
    >
    > C# knows two types of division, and in our case it used the one that we are probably less familiar with. The two types are:
    > 1. [Division of real numbers](https://en.wikipedia.org/wiki/Division_(mathematics)#Of_real_numbers){:target="_blank"}: This is the one you most likely are familiar with. The result of this division is a real number.
    > 1. [Division of integers](https://en.wikipedia.org/wiki/Division_(mathematics)#Of_integers){:target="_blank"}: The result of this division is an integer where the fractional part of the real quotient is cut off.
    >
    > C# decides to do integer division if both operands are integers. If at least one operand is a real number, it does real division and we get a real number as result.
    > An integer can be turned into a real number quite easily by appending `.0` at the end, so in the above example we get the real result if we wrote `17.0 / 7`.
    > For variables it's slightly more verbose. If you have `a / b` and both `a` and `b` are of type `int` -- shorthand for *integer* -- you need to convert at least one of them to a `double` by prepending `(double)`, for example `(double)a / b`. This conversion is typically called *cast*. Here is the full example:
    >
    >
   ```csharp
   int a = 17;
   int b = 7;
   double result = (double)a / b;
   ```

1. Maybe integer division is not as unfamiliar as it initially seemed. When doing division by hand you might have learned to calculate the integer quotient as well as the remainder. C# has an easy to use operator to calculate the remainder because there are many use cases for remainders. To calculate the remainder of a division you use the modulo operator `%`, for example `17 % 7`.

As you probably know from maths there is an infinite number of integer values and an [even larger number](https://en.wikipedia.org/wiki/Uncountable_set){:target="_blank"} of real numbers. But in C# we only have four bytes to represent an `int` and eight bytes for a `double`, so the number of different values is limited. Let's explore those limits and see what happens when we exceed them.

1. Let the turtle tell us the minimum and maximum value for `int` using `Turtle.Say($"Minimum int: {int.MinValue}\r\nMaximum int: {int.MaxValue}");`.
    > You can think of `int.MinValue` and `int.MaxValue` as variables except that you can't store another value in them. They are *constant*.
1. If `int.MaxValue` is the maximum number that we can respresent with four bytes, what happens when we increase it by 1? Try `Turtle.Say($"Maximum int + 1: {int.MaxValue + 1}");`?
    > C# apparently is smart enough to detect an *overflow* and prevents us from running the program. We can tell C# to ignore overflows by using `Turtle.Say($"Maximum int + 1: {unchecked(int.MaxValue + 1)}");`. While very useful for demonstration purposes it's very unlikely that you'll ever need the `unchecked` operator. If you look at the *binary representation* of an `int` value in C# -- which we are not going to do because this challenge is already complicated enough -- it's very obvious why `int.MaxValue + 1` equals `int.MinValue`.
1. Try the same experiment with `int.MinValue`.

The internal representation of real numbers is much more complicated because of the fractional part. The most obvious thing to do would be splitting the eight bytes of a `double` into four bytes for the integer part and four bytes for the fractional part -- this is called [fixed-point representation](https://en.wikipedia.org/wiki/Fixed-point_arithmetic){:target="_blank"}. However most systems use [floating-point representation](https://en.wikipedia.org/wiki/Floating-point_arithmetic){:target="_blank"} which gives a much wider range of possible values but in exchange suffers from a loss of precision which we are going to explore now.

1. Add the command `Turtle.Say($"0.1 + 0.2 = {0.1 + 0.2:R}");` and check the result.
    > `:R` after the calculation prevents rounding the value. Again we use this here for demonstration purposes. In real life this is needed very rarely.

    > It's not important why exactly we have a rounding error in this calculation, but the take-away from this lesson is that a computer can't represent all real numbers (not even close) and that we have to be very careful when working with such numbers.
1. Check the bounds of `double` as we did before with `int`.
1. Try to overflow it by multiplying `double.MaxValue` by 2 and see what happens.
1. Try the same experiment with `double.MinValue`.
