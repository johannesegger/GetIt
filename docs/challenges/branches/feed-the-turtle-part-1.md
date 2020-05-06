---
title: Feed the turtle - Part 1
---

This challenge is split up into multiple parts because we're building something slightly bigger - a first game. The game itself is well-known and I wouldn't be surprised if you already played it. The goal is to catch falling objects by moving the player at the bottom to the left or to the right. We'll also track the number of caught objects and stop the game if the player missed too many objects.

In the first part we'll implement the logic to move the player. At the end of it we'll have something like this:

<video controls>
  <source src="feed-the-turtle-part-1.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

1. Move the turtle to the bottom of the scene.

   > We don't really know where *bottom* actually is, and we also want to support different scene sizes (the user should be able to resize the window and the turtle should still be at the bottom). That's why it's best to use `Game.SceneBounds.Bottom` which contains the y coordinate of the bottom of the scene. Move the turtle slightly above the bottom line so that it's fully visible.

1. Create an endless loop where the turtle sleeps for 25 milliseconds at the end and insert the following commands:
   1. Move the turtle according to the following conditions:
      * If the left arrow key is pressed and the right one is not, turn left, move 5 steps forward and switch to the next costume.
      * If the right arrow key is pressed and the left one is not, move to the right.
      * If both arrow keys are pressed or none of them is pressed, turn up (so it can better catch the falling objects).
      > Test that logic by pressing no arrow key, only the left arrow key, only the right arrow key, and both the left and the right arrow key.
   1. Wonderful. We didn't think about what should happen when the turtle moves outside the scene. Currently that's possible, but we can do better. Ensure that the turtle can't move outside the scene by just stopping it at the edges. Use `Turtle.Position.X`, `Game.SceneBounds.Left` and `Game.SceneBounds.Right` to check whether the turtle is outside the scene.
      > While there are many ways to accomplish this, try not to the change the code you created in the previous step. It'll be simpler to test the alternative strategy in the next step.
   1. Comment out the code you created in the previous step and instead move the turtle to the opposite edge when it moves outside the scene. This will make it simpler to catch objects that are otherwise too far away.

We now have a fully working turtle that is ready to catch some objects.
