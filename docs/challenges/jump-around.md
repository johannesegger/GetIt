---
title: Jump around
---

This challenge will introduce you to loops -- a way to repeat one or more commands.

1. Add the command `Turtle.MoveToRandomPosition();` that moves the turtle to a random position on the scene.
1. Repeat this command forever.
    > While technically you could just copy and paste the above command, you won't be able to repeat it *forever*.
    > A better way to repeat an arbitrary number of commands is to use a `while` loop.
    > The general form of a `while` loop is always the same. The following snippet shows it with two placeholders `<condition>` and `<commands>` that must be replaced for every loop.
    >
   ```csharp
   while (<condition>)
   {
       <commands>
   }
   ```
    >
    > `<condition>` must be replaced with an expression of type `bool`. As you probably remember `bool` has only two possible values: `true` and `false`. If the condition evaluates to `true` the commands inside the curly braces are executed. Then the condition is checked again. If it evaluates to `true` again, all commands inside the curly braces are executed again. So as long as the condition evaluates to `true` the commands inside the curly braces are executed. If the condition evaluates to `false` the commands inside the curly braces are skipped and looping is finished. Program execution continues with the commands *after* the loop.
    >
    > `<commands>` must be replaced with an arbitrary number of commands. These commands are repeated as long as the condition of the loop evaluates to `true`.
    >
    > To repeat some commands forever we can simply use `true` as our condition. The full loop should then look like this:
   ```csharp
   while (true)
   {
       Turtle.MoveToRandomPosition();
   }
   ```
    >
    > While in C# indentation is not necessary, it helps a lot to see which commands belong to the loop.
1. Run the program.
    > Wow, what a fast turtle. Let's slow it down a bit.
1. Add the command `Turtle.Sleep(500);` after the movement.
    > Because sleeping has to occur after *every* movement, ensure that you put the command *before* the closing curly brace `}`, not after.
    > In this program it wouldn't make much sense to put any commands *after* the loop because our loop runs forever.
    > We'll take a look at finite loops in the next challenge.
