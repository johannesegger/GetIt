---
title: Draw polygon
---

<style>
#notes {
    width: auto;
    margin: 10px 0;
}
#notes th:not(:last-child), #notes td:not(:last-child) {
    border-right: 1px solid #a4d337;
}
#notes th:first-child {
    border-right: 2px solid #a4d337;
}
</style>

In this challenge you'll use loops to create regular polygons with an arbitrary number of sides.

1. Draw a regular triangle with a side length of 200 using `Turtle.MoveInDirection` and `Turtle.RotateCounterClockwise`.
    > Don't forget to turn on the pen at the beginning.
1. Insert a pause command before every movement so that you can follow the movement during execution.
1. Write down the number of movements and the rotation of the turtle. You can for example use the following template:
    <table id="notes">
        <thead>
            <tr>
                <th></th>
                <th>Number of movements</th>
                <th>Rotation [degrees]</th>
                <th>Total rotation [degrees]</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <th>Triangle</th>
                <td></td>
                <td></td>
                <td></td>
            </tr>
            <tr>
                <th>Square</th>
                <td></td>
                <td></td>
                <td></td>
            </tr>
            <tr>
                <th>Pentagon</th>
                <td></td>
                <td></td>
                <td></td>
            </tr>
        </tbody>
    </table>
1. Change the program so that the turtle draws a square instead of a triangle.
1. Again write down the number of movements and the rotation of the turtle.
1. Change the program again so that the turtle draws a regular pentagon instead of a triangle.
1. For the last time write down the number of movements and the rotation of the turtle.

You now have a fully working program that draws a regular pentagon, however the current approach of drawing a regular n-gon is not applicable if for example we wanted to draw 100 sides.

That's where loops can help us. You certainly already see the commands that are repeated and how many times they are repeated. The tricky part here is to calculate the rotation after each side. Use your notes to come up with a formula that given the number of sides calculates the rotation after drawing one side.

1. At the beginning of the program introduce a variable `numberOfSides` and store the number of sides that the turtle should draw in it.
    > `numberOfSides` should be able to store whole numbers, so we use `int` as data type.
1. Use your formula to calculate the rotation after drawing one side and store it in another variable `rotation`. Use this variable for all rotation commands.
    > Because the angle might not be an integer, use a data type that can store real numbers and ensure that you don't cut the fractional part by doing [integer division](https://en.wikipedia.org/wiki/Division_(mathematics)#Of_integers){:target="_blank"}.
1. Create a loop that repeats moving and rotating until all sides are drawn.
1. Change the length of the pause after each movement.
    > Note that you only have to change it once. Without loops it would be once per movement.
1. Test your program with 3, 4 and 5 sides and check that you always get a regular n-gon with the correct number of sides.
1. Test your program with 10 sides (*decagon*). You should see that the geometry is too large to fit on the scene. Try to come up with a formula that given the number of sides calculates the side length of the n-gon. Use this formula *before* the loop to store the result in a variable `sideLength` and use the variable as argument to `Turtle.MoveInDirection`. Verify that -- no matter how many sides are drawn -- the geometries roughly have the same size.
    > Again you probably need a data type that can store real numbers and you should make sure that you don't do integer division.

    > While it's not wrong to do the calculation *inside* the loop, calculating it once before the loop works as well and doesn't waste precious CPU time.
1. The next thing you should notice when drawing an n-gon with many sides is that the pause between two movements becomes too long. Again use a formula to calculate the pause given the number of sides of the n-gon, store the calculated value in a variable `sleepTimeInMilliseconds` and use it as the argument for the pause command. Verify that -- no matter how many sides are drawn -- the total draw duration doesn't change.
1. Nicely done! Pat yourself on the back and celebrate the challenge by drawing a 100-gon which should look almost like a circle.
