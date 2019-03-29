using System;

namespace GetIt.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Program1();
            // Program2();
            // Program3();
            // Program4();
            Program5();
            // Program8();
        }

        private static void Program1()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.SetPenWeight(1.5);
            Turtle.SetPenColor(RGBAColor.Cyan.WithAlpha(0x80));
            Turtle.TurnOnPen();
            var n = 5;
            while (n < 200)
            {
                Turtle.MoveInDirection(n);
                Turtle.RotateCounterClockwise(89.5);

                Turtle.ShiftPenColor(10);
                n++;

                Turtle.Sleep(50);
            }
        }

        private static void Program2()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            for (int i = 0; i < 36; i++)
            {
                Turtle.RotateClockwise(10);
                Turtle.MoveInDirection(10);
                Turtle.Sleep(50);
            }
        }

        private static void Program3()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.Say("Let's do it", 2);
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(10);
                Turtle.Sleep(50);
            }
            Turtle.Say("Nice one");
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(-10);
                Turtle.Sleep(50);
            }
            Turtle.ShutUp();
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(10);
                Turtle.Sleep(50);
            }
            Turtle.Say("Done");
        }

        private static void Program4()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.SetPenWeight(1.5);
            Turtle.SetPenColor(RGBAColor.Cyan);
            Turtle.TurnOnPen();
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(10);
                Turtle.Sleep(50);
            }
            Game.ClearScene();
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(-10);
                Turtle.Sleep(50);
            }
            Game.ClearScene();
            for (var i = 0; i < 10; i++)
            {
                Turtle.MoveInDirection(10);
                Turtle.Sleep(50);
            }
            Game.ClearScene();
        }

        private static void Program5()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Move me with arrow keys");
            using (Turtle.OnKeyDown(KeyboardKey.Up, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Down, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Left, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Right, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Up, player => player.MoveUp(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Down, player => player.MoveDown(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Left, player => player.MoveLeft(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Right, player => player.MoveRight(10)))
            {
                Turtle.Sleep(5000);
            }
            Turtle.Say("Game over");
        }

        private static void Program8()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Sleep(1000);

            for (int i = 0; i < 500; i++)
            {
                Turtle.Say(new string('A', i));
                Turtle.Sleep(20);
            }
        }
    }
}
