---
title: Greeting
---

This challenge introduces variables -- what they are and why we need them.
You'll also learn about a special feature of commands.
They can not only expect some input parameters, but also return a result.
We'll see how variables help to use those results.

1. Greet the user with a message by adding the command `Turtle.Say("Hi Marie! Nice to meet you.");`
    > Don't forget to put quotes `"` around the text. This is different from numbers because they don't need to be enclosed by any characters at all.
1. Run the program and see how the turtle greets you.
    > Does it really greet you? Probably not, except your name is Marie. But we want our program to be able to greet anyone. This is where variables come in very handy.
1. Create a new line before the greeting command, insert `var user = "Marie";` to define a variable and store the value `"Marie"` in it.
    > Although it's slightly more complicated, for now we can safely assume that new variables are defined using `var`. In the next challenge we'll see what `var` really is and how it helps simplifying variable definitions.

    > Every variable must have a name, which in our case is `user`. Valid variable names must obey some rules, but if you only use letters you're on the safe side.

    > A variable is just a container for a value. We can store a new value in the container and also see what's stored in the container -- this is typically called ***reading from** and **writing to** a variable*.
1. Now we want to put the value of the variable into the greeting. It's very common to put variable values inside some longer text, which is why in C# there is a simple way to do so (Warning: special characters ahead). Replace the argument to `Turtle.Say` with `$"Hi {user}! Nice to meet you."`. This expression inserts the value of the variable `user` into the text and is accurately related to as [*string interpolation*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated){:target="_blank"}.
1. Run the program and verify that absolutely nothing changed, because the turtle still only greets Maries.
    > While from the users point of view nothing changed, introducing a variable was a necessary intermediate step to rewrite the program so that anyone can be greeted.
1. The turtle can not only say something, it can also ask the user to input something. Replace the value `"Marie"` with the command `Turtle.Ask("Hi! My name is Oscar. What's your name?")` in the line where `user` is defined.
    > Now it's not `"Marie"` that is stored in `user`, it's *the result of `Turtle.Ask`* that is stored in `user`.

    > Some commands *return* values after they've been invoked. It's very common to store returned values in variables for later use -- as we did. But you can also use the returned value directly (e.g. between `{` and `}` in string interpolation expressions) or ignore it altogether.
