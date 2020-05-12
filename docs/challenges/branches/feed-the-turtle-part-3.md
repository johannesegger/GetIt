---
title: Feed the turtle - Part 3
---

The third part of this challenge series finalizes the project you created in the first two parts.
In this part we'll implement a score, lives and do some polishing. The final version will look like this:

<video controls>
  <source src="feed-the-turtle-part-3.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

Let's start with the greeting, because that's also one of the first things in our game:

1. After `Game.ShowSceneAndAddTurtle();` greet the user, wait until the space key is pressed, then make the turtle shut up.

Your fingers should be warmed up by now, so we continue with the score:

1. Before the endless loop, define a new variable `score` and initialize it with `0`.
1. Inside the loop, tell the user her score. This must be done for every loop iteration because the score is going to change.
1. Before saying the score, but after moving the turtle as well as the food, check if the turtle and the food touch each other.
    > While you could find that out using both coordinates and sizes, `Turtle.TouchesPlayer` might be handier.
1. If so, increase the score by 1 because that means that the turtle caught the food and move the food to a random position at the top.

The last part is the lives, which is quite similar to the score:

1. Define a variable `lives` and initialize it with `3`.
1. Tell the user her lives additionally to the score.
1. Decrease `lives` by one if the food hits the bottom (we already check that).
1. Change the endless loop so that it continues only if there are lives left.

After the loop - when the game is over because there are no lives left - tell the user her score.

Now reward yourself by playing your first self-written C# game. Congratulations!
