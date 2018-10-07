using System;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

namespace PlayAndLearn.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.ShowSceneAndAddTurtle();
            // Game.SetSlowMotion();

            // Program1();
            // Program2();
            // Program3();
        }

        private static void Program1()
        {
            Turtle.Default.GoTo(0, 0);
            Turtle.Default.SetPenWeight(1.5);
            Turtle.Default.SetPenColor(RGBColor.Cyan);
            Turtle.Default.TurnOnPen();
            var n = 5;
            while (n < 400)
            {
                Turtle.Default.Go(n);
                Turtle.Default.RotateCounterClockwise(89.5);

                Turtle.Default.ShiftPenColor(10.0 / 360);
                n++;

                Game.Sleep(10);
            }
        }

        private static void Program2()
        {
            Turtle.Default.GoTo(0, 0);
            for (int i = 0; i < 36; i++)
            {
                Turtle.Default.RotateClockwise(10);
                Turtle.Default.Go(10);
            }
        }

        private static void Program3()
        {
            Turtle.Default.GoTo(0, 0);
            Turtle.Default.Say("Let's do it", 2);
            for (var i = 0; i < 10; i++)
            {
                Turtle.Default.Go(10);
            }
            Turtle.Default.Say("Nice one");
            for (var i = 0; i < 10; i++)
            {
                Turtle.Default.Go(-10);
            }
            Turtle.Default.ShutUp();
            for (var i = 0; i < 10; i++)
            {
                Turtle.Default.Go(10);
            }
            Turtle.Default.Say("Done");
        }
    }
}
