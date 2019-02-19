using System;

namespace GetIt.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Program1();
        }

        private static void Program1()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            // Turtle.SetPenWeight(1.5);
            // Turtle.SetPenColor(RGBAColor.Cyan.WithAlpha(0x40));
            // Turtle.TurnOnPen();
            // var n = 5;
            // while (n < 400)
            // {
            //     Turtle.MoveInDirection(n);
            //     Turtle.RotateCounterClockwise(89.5);

            //     Turtle.ShiftPenColor(10);
            //     n++;

            //     Turtle.Sleep(10);
            // }
        }
    }
}
