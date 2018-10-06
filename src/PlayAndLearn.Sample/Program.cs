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
            //Game.SetSlowMotion();

            //Program1();
            Program2();
        }

        private static void Program1()
        {
            Turtle.Default.GoTo(0, 0);
            Turtle.Default.SetPenWeight(1.5);
            Turtle.Default.SetPenColor(new RGB(0x00, 0xFF, 0xFF));
            Turtle.Default.TurnOnPen();
            var n = 5;
            while (n < 400)
            {
                Turtle.Default.Go(n);
                Turtle.Default.RotateCounterClockwise(89.5);

                Turtle.Default.ShiftPenColor(10.0 / 360);
                n++;

                Game.SleepMilliseconds(10);
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
    }
}
