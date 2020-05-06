---
title: Comparisons
---

This challenge shows you how to compare values as well as how to combine two or more comparisons.
You'll need this whenever you work with loops and branches.

1. Let the turtle ask the users age.
1. Convert the answer to `int` and store it in a variable `age`.
    > We still have to hope that the user enters a number, otherwise the program crashes.

    > While the exact age is typically some real number -- e.g. `17.2` -- we say that the user should only be able to enter the integer age here -- e.g. `17`.
1. Tell the user if she is grown-up by adding the command `Turtle.Say($"You're grown-up: {age > 18}");` and try the program with different ages.
    > You probably know the operator `>` from maths. It checks if the left side of the operator is greater than the right side. The full expression can be either *true* or *false* (either you are grown-up or you're not).
1. Add a break by adding `Game.WaitForMouseClick();` because the turtle is going to tell the user more about her age.
1. Tell the user something about her age by trying all *comparison operators*: less than (`<`), greater than (`>`), less than or equal (`<=`), greater than or equal (`>=`), equal (`==`), not equal (`!=`).
    > In math you would rather write `≤` instead of `<=`, `≥` instead of `>=` and `≠` instead of `!=` resp., but the latter ones are a bit easier to write on a typical keyboard.

    > Never ever confuse the equality operator `==` with the assignment operator `=`. The former is for comparing values, the latter is for storing a value in a variable -- two totally different things.

    > That's nice, isn't it? We can already tell the user much about her age, but we can't for example tell if the user is between 6 and 19, right? This is where we need to combine two or more comparisons.
1. Tell the user if she probably goes to school by adding the command `Turtle.Say($"You probably go to school: {age >= 6 && age <= 19}");`.
    > `&&` is the [*logical AND*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators#conditional-logical-and-operator-){:target="_blank"} operator and combines two *true/false expressions* to a resulting *true/false expression* according to the following table:
    >
   | `age >= 6` | `age <= 19` | `age >= 6 && age <= 19` |
   | ---------- | ----------- | ----------------------- |
   | false      | false       | false                   |
   | false      | true        | false                   |
   | true       | false       | false                   |
   | true       | true        | true                    |
    >
    > As you can see the full expression is only true if both sub-expressions are true.
1. `||` is the [*logical OR*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators#conditional-logical-or-operator-){:target="_blank"} operator and is written with two [vertical bar characters `|`](https://en.wikipedia.org/wiki/Vertical_bar){:target="_blank"}. Depending on your keyboard layout you might need some time to find it. It returns true if at least one of the sub-expressions return true. Tell the user something about her age by using *logical OR*.
1. `!` is the [*logical negation*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators#logical-negation-operator-) operator and simply converts true to false and false to true. While that sounds a bit useless and can sometimes complicate expressions it can also make programs more readable. Tell the user something about her age by using *logical negation*.

You might have already guessed it, but many languages including C# have a data type for *true* and *false*. It is called [*Boolean*](https://en.wikipedia.org/wiki/Boolean_data_type){:target="_blank"} and in C# it is abbreviated with `bool`. Expressions that use comparison operators or logical operators are either `true` or `false` and therefore are of type `bool`.

For longer boolean expressions or if you need the same expression a couple of times it might help to introduce a separate variable, like `bool getsDiscount = age > 6 && age < 18 || age > 65;`.
You can then use that variable for example inside a *string interpolation* expression: `Turtle.Say($"You get a discount: {getsDiscount}");`.

> If you use a separate variable or not is very much personal preference, but the goal is always to make it as easily readable as possible.

The turtle furthermore has the ability to ask the user a question which she has to confirm or decline.
This is done via `Turtle.AskBool` which returns `true` if the user confirms and otherwise `false`.

1. Ask the user if she wants cake and store the answer in a variable `wantsCake`.
    > You don't have to convert the answer to `bool` because `Turtle.AskBool` already returns `bool`.
1. Tell the user if she gets cake. The user only gets cake if she wants to and if she is more than ten years old.
