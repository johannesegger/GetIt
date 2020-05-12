---
title: Feed the turtle - Part 2
---

The second part of this challenge series extends the project you created in the first part.
In this part we'll implement the falling objects. At the end of it we'll have something like this:

<video controls>
  <source src="feed-the-turtle-part-2.mp4" type="video/mp4">
Your browser does not support the video tag.
</video>

As you can see there is exactly one falling object visible at a time and we don't handle if the turtle catches the falling object.

For the falling object there are at least two strategies to consider:

1. Create a new object and destroy the old object everytime the old object falls off the scene.
1. Use only a single falling object and when that object is at the bottom of the scene move it back to the top.

For this challenge we'll use - for no particular reason - #1.

1. After moving the turtle to the bottom of the scene, but before the endless loop, add a new player to the scene.

   > The command `Game.AddPlayer` does exactly that. It takes a single parameter of type `PlayerData`.
   > You can think of `PlayerData` as the full description of a player - the look, the position, its pen and so on.
   > `Game.AddPlayer` adds a new player on the scene that corresponds to that description and returns us a `Player` object that we can use like the turtle.
   > The full command in this case might be `Player food = Game.AddPlayer(PlayerData.Ant);`.
   > After this command you can control the newly added player by using `food.MoveTo(...)`, `food.Say(...)` and so on.

1. Turn the food so that it looks down.
1. Move the food at a random position.

   > While we can use `food.MoveToRandomPosition()` the food should always start at the top.
   > So in fact we only want the x coordinate to be random.
   > There's no special command to do that.
   > However after moving the food to a completely random position we can use `food.MoveTo` to move the food to the top while keeping the x coordinate the same.
   > To do that use `food.Position.X` as the x coordinate and the top of the scene as the y coordinate.

1. Nice. Now that the food is at a random position at the top we can start letting it fall. Inside the endless loop move the food 5 steps down.

   > Our food is falling now, but it's falling too far. At the bottom of the scene it should go back to the top.

1. Check if the food is below the scene bottom and if so, move it back to the top with another random x coordinate.

That's it. In the next part we'll finish this series by adding a score, lives and doing some polishing.
