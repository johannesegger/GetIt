using System;
using System.Linq;

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
            // Program5();
            // Program6();
            // Program7();
            // Program8();
            Program9();
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

        private static void Program6()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.OnMouseEnter(player => player.MoveToRandomPosition());
        }

        private static void Program7()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Try and hit me, sucker!", 2);
            Turtle.OnClick((player, mouseButton) => player.Say("Ouch, that hurts!", 2));
        }

        private static void Program8()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Sleep(1000);

            for (int i = 0; i < 500; i++)
            {
                Turtle.Say(new string(Enumerable.Range(0, i).Select(j => (char)('A' + j)).ToArray()));
                Turtle.Sleep(20);
            }
        }

        private static void Program9()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.SetPenWeight(5);

            Turtle.TurnOnPen();
            Turtle.MoveTo(33, 33);
            Turtle.Sleep(100);
            Turtle.TurnOffPen();
            Turtle.MoveTo(66, 66);
            Turtle.Sleep(100);
            Turtle.TurnOnPen();
            Turtle.MoveTo(100, 100);
            Turtle.Sleep(100);

            Turtle.MoveTo(100, 33);
            Turtle.Sleep(100);
            Turtle.TurnOffPen();
            Turtle.MoveTo(100, -33);
            Turtle.Sleep(100);
            Turtle.TurnOnPen();
            Turtle.MoveTo(100, -100);
            Turtle.Sleep(100);
            
            Turtle.MoveTo(66, -66);
            Turtle.Sleep(100);
            Turtle.TurnOffPen();
            Turtle.MoveTo(33, -33);
            Turtle.Sleep(100);
            Turtle.TurnOnPen();
            Turtle.MoveToCenter();

            Turtle.TurnOffPen();
            Turtle.MoveLeft(100);
        }
    }
}
