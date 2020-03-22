---
title: Draw patterns
---

In the following video the turtle draws several patterns.
Create a new project for each of them and try to reproduce the patterns as closely as possible.

<video controls>
  <source src="patterns.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

Here are some hints for each pattern that should get you started:

* Horizontal saw
  * At the beginning move the turtle to the left edge. Use `Game.SceneBounds.Left` to get the x coordinate of the left edge. The y coordinate is the center of the scene.
  * Turn on the pen and alternately move the turtle diagonally up and down.
  * Repeat the movement until the turtle's x position is greater than `Game.SceneBounds.Right`.
* Vertical saw
  * Same as above, but the turtle should move from `Game.SceneBounds.Top` to `Game.SceneBounds.Bottom`.
* Cross saw
  * Independent of the scene size the turtle should move from the top left corner to the bottom right corner.
  * Imagine a line between the two corners that the turtle should follow. It might help to have the turtle look along that line.
  * Now turn about 45 degrees counter-clockwise and draw the first line.
  * Turn back and move to your imaginary line so that the turtle draws a *normal* on it.
  * Continue drawing the two lines until the turtle is at the bottom right corner.
* Sine wave - Version 1
  * Make sure you know how a [sine wave](https://en.wikipedia.org/wiki/Sine) looks like before you start.
  * Start at the left edge and calculate the sine of the current x coordinate using `Math.Sin`.
    > `Math.Sin` works with [radians](https://en.wikipedia.org/wiki/Radian), but we want our x coordinates to be interpreted as [degrees](https://en.wikipedia.org/wiki/Degree_(angle)).
    > To do that we have to convert the x coordinate to radians before passing it to `Math.Sin`.
    > The value we get back from `Math.Sin` is between -1 and 1 (that's how sine is defined).
    > Basically this is now the value that we use as y coordinate. However we should scale the value (i.e. multiply it by e.g. 100) before moving the turtle to that position.
  * Calculate the y coordinate for the next x coordinate (you get a better resolution the smaller x steps you make) according to the above procedure until you hit the right edge.
* Sine wave - Version 2
  * Instead of simply moving the turtle along the curve we draw a straight line from the scene bottom to the y value.
* Sine wave - Version 3
  * In addition to the line from the bottom edge to the y value we draw another line with a different color from the y value to the top edge.
* Cosine wave
  * Find the command that calculates the cosine and draw the curve according to "Sine wave - Version 1".
* Tangent wave
  * Find the command that calculates the tangent and draw the curve according to "Sine wave - Version 1".
* Christmas tree - Version 1
  * Similar to "Vertical saw", but the horizontal line is not as long.
  * You probably need two loops to draw both sides of the tree.
* Christmas tree - Version 2
  * Continuously increase the x distance until you hit the bottom edge. The y distance should be constant.
* Sawing blade
  * You have multiple options here:
    1. Use a loop to draw a single blade. Use another loop to draw several blades. You'll end up having a loop inside a loop.
    1. Use a single loop to draw the whole saw. Continuously increase the length of the line. When the length of the line reaches a certain threshold, reset the length to its original value.
  * The easiest way to draw a single line is probably having the turtle to look in the right direction and then move forward and backward.
* Flower
  * You again have multiple options:
    1. Use a loop to draw half of a *petal* and another loop next to it to draw the other half. Use a third loop to draw multiple petals. You'll end up having a loop with two loops in it.
    1. Use a loop to draw a *petal*. Inside the loop the length of the line is continuously increased. When the length hits a certain threshold it is continuously decreased (Hint: Adding a positive value increases the length, adding a negative value decreases the length). Use another loop to draw several petals. You'll end up having a loop inside a loop.
    1. Use a loop to draw the whole flower. You'll need the same logic as before to increase and decrease the length of the line.
