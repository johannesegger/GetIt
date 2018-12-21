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
            // Program5();
            // Program6();
            // Program7();
            // Program8();
            // Program9();
            // Program10();
            // Program11();
            // Program12();
            // Program13();
            // Program14();
            // Program15();
            // Program16();
            // Program17();
            // Program18();
            // Program19();
            Program20();
        }

        private static void Program1()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.SetPenWeight(1.5);
            Turtle.SetPenColor(RGBAColor.Cyan.WithAlpha(0x40));
            Turtle.TurnOnPen();
            var n = 5;
            while (n < 400)
            {
                Turtle.MoveInDirection(n);
                Turtle.RotateCounterClockwise(89.5);

                Turtle.ShiftPenColor(10);
                n++;

                Turtle.Sleep(10);
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
            Turtle.OnClick(player => player.Say("Ouch, that hurts!", 2));
        }

        private static void Program8()
        {
            Game.ShowSceneAndAddTurtle();

            for (int i = 0; i < 500; i++)
            {
                Turtle.Say(new string('A', i));
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
        }

        private static void Program10()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.TurnOnPen();
            Turtle.SetPenColor(RGBAColor.Red);
            while (Turtle.GetDistanceToMouse() > 10)
            {
                Turtle.ShiftPenColor(10);
                var direction = Turtle.GetDirectionToMouse();
                Turtle.SetDirection(direction);
                Turtle.MoveInDirection(10);
                Turtle.NextCostume();
                Turtle.Sleep(50);
            }
            Turtle.Say("Geschnappt :-)");
        }

        private static void Program11()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.TurnOnPen();
            Turtle.SetPenWeight(50);
            Turtle.MoveInDirection(100);
            Turtle.Sleep(1000);
            Turtle.MoveToCenter();
        }

        private static void Program12()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.OnKeyDown(KeyboardKey.Down, player => player.ChangeSizeFactor(-0.1));
            Turtle.OnKeyDown(KeyboardKey.Up, player => player.ChangeSizeFactor(0.1));
            Turtle.OnKeyDown(KeyboardKey.Left, player => player.RotateCounterClockwise(5));
            Turtle.OnKeyDown(KeyboardKey.Right, player => player.RotateClockwise(5));
            Turtle.OnKeyDown(KeyboardKey.Space, player => player.NextCostume());
        }

        private static void Program13()
        {
            Game.ShowScene();

            var isGameOver = false;

            void controlLeftPlayer(PlayerOnScene player)
            {
                player.MoveTo(Game.SceneBounds.Left + 20, 0);
                using (player.OnKeyDown(KeyboardKey.W, p => p.MoveUp(10)))
                using (player.OnKeyDown(KeyboardKey.S, p => p.MoveDown(10)))
                {
                    while (!isGameOver)
                    {
                        player.Sleep(50);
                    }
                }
            }

            var leftPlayer = Game.AddPlayer(
                Player.Create(
                    Costumes.CreateRectangle(
                        RGBAColor.DarkMagenta,
                        new Size(20, 150))),
                controlLeftPlayer);

            void controlRightPlayer(PlayerOnScene player)
            {
                player.MoveTo(Game.SceneBounds.Right - 20, 0);
                using (player.OnKeyDown(KeyboardKey.Up, p => p.MoveUp(10)))
                using (player.OnKeyDown(KeyboardKey.Down, p => p.MoveDown(10)))
                {
                    while (!isGameOver)
                    {
                        player.Sleep(50);
                    }
                }
            }

            var rightPlayer = Game.AddPlayer(
                Player.Create(
                    Costumes.CreateRectangle(
                        RGBAColor.Magenta,
                        new Size(20, 150))),
                controlRightPlayer);

            var rand = new Random();
            void controlBall(PlayerOnScene player)
            {
                player.SetDirection(rand.Next(360));
                while (true)
                {
                    player.MoveInDirection(10);
                    player.BounceOffWall();
                    if (player.Bounds.Left <= Game.SceneBounds.Left
                        || player.Bounds.Right >= Game.SceneBounds.Right)
                    {
                        isGameOver = true;
                        break;
                    }
                    if (player.Bounds.Left <= leftPlayer.Bounds.Right
                        && player.Position.Y <= leftPlayer.Bounds.Top
                        && player.Position.Y >= leftPlayer.Bounds.Bottom)
                    {
                        player.SetDirection(180 - player.Direction);
                    }
                    else if (player.Bounds.Right >= rightPlayer.Bounds.Left
                        && player.Position.Y <= rightPlayer.Bounds.Top
                        && player.Position.Y >= rightPlayer.Bounds.Bottom)
                    {
                        player.SetDirection(180 - player.Direction);
                    }
                    player.Sleep(50);
                }
            }

            Game.AddPlayer(
                Player.Create(
                    Costumes.CreateCircle(RGBAColor.Black, 10)),
                controlBall);
        }

        private static void Program14()
        {
            Game.ShowSceneAndAddTurtle();

            int age;
            string input = Turtle.Ask("How old are you?");
            while (!int.TryParse(input, out age))
            {
                input = Turtle.Ask("Are you kidding? That's not a number. How old are you?");
            }
            Turtle.Say($"{age}? You're looking good for your age!");
        }

        private static void Program15()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Click somewhere");
            var position = Game.WaitForMouseClick();
            Turtle.Say($"You clicked at {position}");
        }

        private static void Program16()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Press any key to start");
            var key = Game.WaitForAnyKeyDown();
            Turtle.Say($"You started with <{key}>. Let's go. Press <Space> to stop.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            Turtle.Say("Game over.");
        }

        private static void Program17()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Move me with arrow keys", 2);
            while (!Game.IsKeyDown(KeyboardKey.Space))
            {
                if (Game.IsKeyDown(KeyboardKey.Left) && Game.IsKeyDown(KeyboardKey.Up))
                {
                    Turtle.SetDirection(135);
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Left) && Game.IsKeyDown(KeyboardKey.Down))
                {
                    Turtle.SetDirection(225);
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Right) && Game.IsKeyDown(KeyboardKey.Up))
                {
                    Turtle.SetDirection(45);
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Right) && Game.IsKeyDown(KeyboardKey.Down))
                {
                    Turtle.SetDirection(315);
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Left))
                {
                    Turtle.TurnLeft();
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Right))
                {
                    Turtle.TurnRight();
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Up))
                {
                    Turtle.TurnUp();
                    Turtle.MoveInDirection(10);
                }
                else if (Game.IsKeyDown(KeyboardKey.Down))
                {
                    Turtle.TurnDown();
                    Turtle.MoveInDirection(10);
                }
                Turtle.Sleep(50);
            }
            Turtle.Say("Game over.");
        }

        private static void Program18()
        {
            Game.ShowScene();

            Game.AddPlayer(
                Player.Create(
                    Costumes.CreatePolygon(
                        RGBAColor.Pink,
                        new Position(50, 0),
                        new Position(150, 50),
                        new Position(250, 0),
                        new Position(200, 100),
                        new Position(300, 150),
                        new Position(200, 150),
                        new Position(150, 250),
                        new Position(100, 150),
                        new Position(0, 150),
                        new Position(100, 100))));
        }

        private static void Program19()
        {
            Game.ShowSceneAndAddTurtle();

            var ant = Game.AddPlayer(Player.CreateAnt());
            ant.MoveRight(100);

            var bug = Game.AddPlayer(Player.CreateBug());
            bug.MoveRight(200);

            var spider = Game.AddPlayer(Player.CreateSpider());
            spider.MoveRight(300);

            foreach (var player in new[] { ant, bug, spider })
            {
                player.OnKeyDown(KeyboardKey.Down, p => p.ChangeSizeFactor(-0.1));
                player.OnKeyDown(KeyboardKey.Up, p => p.ChangeSizeFactor(0.1));
                player.OnKeyDown(KeyboardKey.Left, p => p.RotateCounterClockwise(5));
                player.OnKeyDown(KeyboardKey.Right, p => p.RotateClockwise(5));
                player.OnKeyDown(KeyboardKey.Space, p => p.NextCostume());
            }
        }

        private static void Program20()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.OnKeyDown(KeyboardKey.Left, p => p.MoveLeft(10));

            Turtle.Say("Sleeping");
            Turtle.Sleep(5000);

            var name = Turtle.Ask("What's your name?");

            Turtle.Say($"Hi, {name}");
        }
    }
}
