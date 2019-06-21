using System;
using System.Collections.Generic;
using System.Linq;

namespace GetIt.Sample.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            // Program1();
            // Program2();
            Program3();
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
            // Program20();
            // Program21();
            // Program22();
            // Program23();
            // Program24();
            // Program25();
            // Program26();
            // Program27();
            // Program28();
            // Program29();
            // Program30();
            // Program31();
            // Program32();
            // Program33();
            // Program34();
            // Program35();
            // Program36();
        }

        private static void Program1()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.SetPenWeight(1.5);
            Turtle.SetPenColor(RGBAColors.Cyan.WithAlpha(0x40));
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
            Turtle.SetPenColor(RGBAColors.Cyan);
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

            Turtle.Say("Move me with arrow keys. Press <Space> to quit.");
            using (Turtle.OnKeyDown(KeyboardKey.Up, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Down, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Left, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Right, player => player.ShutUp()))
            using (Turtle.OnKeyDown(KeyboardKey.Up, player => player.MoveUp(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Down, player => player.MoveDown(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Left, player => player.MoveLeft(10)))
            using (Turtle.OnKeyDown(KeyboardKey.Right, player => player.MoveRight(10)))
            {
                Game.WaitForKeyDown(KeyboardKey.Space);
            }
            Turtle.Say("Game over");
        }

        private static void Program6()
        {
            Game.ShowSceneAndAddTurtle();
            Turtle.Say("Try and catch me");
            Turtle.OnMouseEnter(player => player.ShutUp());
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

        private static void Program10()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.TurnOnPen();
            Turtle.SetPenColor(RGBAColors.Red);
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

            Game.Sleep(1000);

            var isGameOver = false;

            void controlLeftPlayer(Player player)
            {
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
                PlayerData
                    .Create(
                        SvgImage.CreateRectangle(
                            RGBAColors.DarkMagenta,
                            new Size(20, 150)))
                    .WithPosition(Game.SceneBounds.Left + 20, 0),
                controlLeftPlayer);

            void controlRightPlayer(Player player)
            {
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
                PlayerData
                    .Create(
                        SvgImage.CreateRectangle(
                            RGBAColors.Magenta,
                            new Size(20, 150)))
                    .WithPosition(Game.SceneBounds.Right - 20, 0),
                controlRightPlayer);

            var rand = new Random();
            void controlBall(Player player)
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
                PlayerData.Create(
                    SvgImage.CreateCircle(RGBAColors.Black.WithAlpha(128), 10)),
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
            var clickEvent = Game.WaitForMouseClick();
            Turtle.Say($"You clicked with mouse button {clickEvent.Button} at {clickEvent.Position}");
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
                PlayerData.Create(
                    SvgImage.CreatePolygon(
                        RGBAColors.Pink,
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
            Game.ShowScene();

            var turtle = Game.AddPlayer(PlayerData.Turtle);
            var ant = Game.AddPlayer(PlayerData.Ant.WithPosition(100, 0));
            var bug = Game.AddPlayer(PlayerData.Bug.WithPosition(200, 0));
            var spider = Game.AddPlayer(PlayerData.Spider.WithPosition(300, 0));

            foreach (var player in new[] { turtle, ant, bug, spider })
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

        private static void Program21()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.SetDirection(Directions.Right);
            Turtle.Say("Try to eat some ants", 1);

            void updateDirection(Player p, KeyboardKey key)
            {
                if (key == KeyboardKey.Right && p.Direction != Directions.Left)
                {
                    p.SetDirection(Directions.Right);
                }
                else if (key == KeyboardKey.Up && p.Direction != Directions.Down)
                {
                    p.SetDirection(Directions.Up);
                }
                else if (key == KeyboardKey.Left && p.Direction != Directions.Right)
                {
                    p.SetDirection(Directions.Left);
                }
                else if (key == KeyboardKey.Down && p.Direction != Directions.Up)
                {
                    p.SetDirection(Directions.Down);
                }
            }

            IReadOnlyCollection<Player> points = Enumerable
                .Range(0, 5)
                .Select(_ =>
                {
                    var point = Game.AddPlayer(PlayerData.Ant);
                    point.MoveToRandomPosition();
                    return point;
                })
                .ToList();

            var score = 0;
            using (Turtle.OnAnyKeyDown(updateDirection))
            {
                var delay = 200.0;
                while (!Turtle.TouchesEdge())
                {
                    foreach (var point in points.Where(Turtle.TouchesPlayer))
                    {
                        score++;
                        point.MoveToRandomPosition();
                        delay = delay * 2/3;
                    }
                    Turtle.MoveInDirection(10);
                    Turtle.Sleep(delay);
                }
            }
            Turtle.MoveToCenter();
            Turtle.Say($"Game over. Score: {score}");
        }

        private static void Program22()
        {
            Game.ShowSceneAndAddTurtle();
            Turtle.MoveUp(100);

            using (var food = Game.AddPlayer(PlayerData.Bug))
            {
                food.Say("I'm just here for 5 seconds", 5);
            }
        }

        private static void Program23()
        {
            Game.ShowScene();
            Game.AddPlayer(PlayerData.Create(SvgImage.Load(@"assets\Turtle2.svg")));
        }

        private static void Program24()
        {
            Game.ShowSceneAndAddTurtle();
            while (true)
            {
                Turtle.Say($"Mouse position: {Game.MousePosition}");
                Game.Sleep(50);
            }
        }

        private static void Program25()
        {
            // Game.ShowScene(1000, 350);
            // Game.ShowMaximizedScene();
            // Game.ShowSceneAndAddTurtle(1000, 350);
            Game.ShowMaximizedSceneAndAddTurtle();
        }

        private static void Program26()
        {
            Game.ShowSceneAndAddTurtle(Background.Baseball1.Size.Width * 2, Background.Baseball1.Size.Height * 2);
            Game.SetBackground(Background.Baseball1);
            Turtle.TurnOnPen();
            Turtle.MoveTo(50, 100);
            Turtle.Say("Press <Enter> to reset the background.");
            Game.WaitForKeyDown(KeyboardKey.Enter);
            Turtle.ShutUp();
            Game.SetBackground(Background.None);
        }

        private static void Program27()
        {
            Game.ShowSceneAndAddTurtle();

            var color = RGBAColor.SelectRandom(RGBAColors.Red, RGBAColors.Green, RGBAColors.Blue);
            Turtle.Say($"Chose color {color}");
            Turtle.TurnOnPen();
            Turtle.SetPenColor(color);
            Turtle.SetPenWeight(5);
            Turtle.MoveRight(100);
        }

        private static void Program28()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.SetPenWeight(5);

            Turtle.TurnOnPen();
            Turtle.MoveTo(33, 33);
            Turtle.Sleep(1000);
            Turtle.TurnOffPen();
            Turtle.MoveTo(66, 66);
            Turtle.Sleep(1000);
            Turtle.TurnOnPen();
            Turtle.MoveTo(100, 100);
            Turtle.Sleep(1000);

            using (Game.BatchCommands())
            {
                Turtle.MoveTo(100, 33);
                Turtle.Sleep(1000);
                Turtle.TurnOffPen();
                using (Game.BatchCommands())
                {
                    Turtle.MoveTo(100, -33);
                }
                Turtle.Sleep(1000);
                Turtle.TurnOnPen();
                Turtle.MoveTo(100, -100);
                Turtle.Sleep(1000);
            }

            Turtle.MoveTo(66, -66);
            Turtle.Sleep(1000);
            Turtle.TurnOffPen();
            Turtle.MoveTo(33, -33);
            Turtle.Sleep(1000);
            Turtle.TurnOnPen();
            Turtle.MoveToCenter();

            Turtle.TurnOffPen();
            Turtle.MoveLeft(100);
            Turtle.Say("Awesome");
        }

        private static void Program29()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Window title is changing.", 1);
            Game.SetWindowTitle("1");
            Turtle.Sleep(1000);
            Game.SetWindowTitle(" ");
            Turtle.Sleep(1000);
            Game.SetWindowTitle("2");
            Turtle.Sleep(1000);
            Game.SetWindowTitle("");
            Turtle.Sleep(1000);
            Game.SetWindowTitle("3");
            Turtle.Sleep(1000);
            Game.SetWindowTitle(null);
        }

        private static void Program30()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveRight(100);
            Turtle.Say("Press and hold <Space>.");
            Turtle.OnKeyDown(KeyboardKey.Space, TimeSpan.FromSeconds(1), (p, i) => p.Say($"Event handler called {i} time(s)."));

            var player = Game.AddPlayer(PlayerData.Turtle.WithPosition(-100, 0));
            player.Say("Press and hold any key.");
            player.OnAnyKeyDown(TimeSpan.FromSeconds(1), (p, key, i) => p.Say($"Event handler called {i} time(s) with key {key}."));
        }

        private static void Program31()
        {
            Game.ShowScene();

            var turtleData = PlayerData.Turtle
                .WithSizeFactor(2)
                .WithPosition(-50, 100)
                .WithDirection(135)
                .WithPenOn()
                .WithPenWeight(5)
                .WithPenColor(RGBAColors.SteelBlue);
            var turtle = Game.AddPlayer(turtleData);
            turtle.MoveInDirection(-200);
            turtle.MoveInDirection(200);
        }

        private static void Program32()
        {
            Game.ShowScene();

            var turtle1 = Game.AddPlayer(PlayerData.Turtle.WithPosition(-10, 0));
            var turtle2 = Game.AddPlayer(PlayerData.Turtle.WithPosition(10, 0));
            
            turtle2.Say("Press <Space> to send me to back.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            turtle2.ShutUp();
            turtle2.SendToBack();

            turtle2.Say("Press <Space> to send me to back again.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            turtle2.ShutUp();
            turtle2.SendToBack();

            turtle2.Say("Press <Space> to send me to front.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            turtle2.ShutUp();
            turtle2.BringToFront();

            turtle1.Say("Press <Space> to send me to back.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            turtle1.ShutUp();
            turtle1.SendToBack();

            turtle1.Say("Press <Space> to send me to front.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            turtle1.ShutUp();
            turtle1.BringToFront();
        }

        private static void Program33()
        {
            Game.ShowScene();

            var direction = Directions.Right;

            void UpdateDirection()
            {
                if (Game.IsKeyDown(KeyboardKey.Up) && direction != Directions.Down)
                {
                    direction = Directions.Up;
                }
                if (Game.IsKeyDown(KeyboardKey.Down) && direction != Directions.Up)
                {
                    direction = Directions.Down;
                }
                if (Game.IsKeyDown(KeyboardKey.Left) && direction != Directions.Right)
                {
                    direction = Directions.Left;
                }
                if (Game.IsKeyDown(KeyboardKey.Right) && direction != Directions.Left)
                {
                    direction = Directions.Right;
                }
            }

            var snakeHead = Game.AddPlayer(PlayerData.Turtle);
            var tailPart = PlayerData
                .Create(SvgImage.CreateCircle(RGBAColors.Black.WithAlpha(0x80), 10));
            var snakeTail = Enumerable.Range(1, 3)
                .Select(i => Game.AddPlayer(tailPart.WithPosition(snakeHead.Position.X - i * 10, snakeHead.Position.Y))
                )
                .ToList();

            var food = Game.AddPlayer(PlayerData.Ant);
            food.MoveToRandomPosition();

            void MoveSnakeHead(double distance)
            {
                snakeHead.SetDirection(direction);
                if (direction == Directions.Up)
                {
                    snakeHead.MoveTo(snakeHead.Position.X, snakeHead.Position.Y + distance);
                }
                else if (direction == Directions.Down)
                {
                    snakeHead.MoveTo(snakeHead.Position.X, snakeHead.Position.Y - distance);
                }
                else if (direction == Directions.Left)
                {
                    snakeHead.MoveTo(snakeHead.Position.X - distance, snakeHead.Position.Y);
                }
                else if (direction == Directions.Right)
                {
                    snakeHead.MoveTo(snakeHead.Position.X + distance, snakeHead.Position.Y);
                }
            }

            void MoveSnake(double distance)
            {
                var position = snakeHead.Position;
                foreach (var part in snakeTail)
                {
                    var nextPosition = part.Position;
                    part.MoveTo(position);
                    position = nextPosition;
                }
                MoveSnakeHead(distance);
            }

            void CheckIfSnakeEatsFood()
            {
                if (snakeHead.TouchesPlayer(food))
                {
                    food.MoveToRandomPosition();
                    var tailPosition = snakeTail.Last().Position;
                    var tail = Game.AddPlayer(tailPart.WithPosition(tailPosition));
                    snakeTail.Add(tail);
                }
            }

            bool CheckIfSnakeHitsWall()
            {
                return snakeHead.TouchesEdge();
            }

            while (true)
            {
                UpdateDirection();
                MoveSnake(10);
                CheckIfSnakeEatsFood();
                if (CheckIfSnakeHitsWall())
                {
                    break;
                }

                Game.Sleep(50);
            }

            snakeHead.Say("Game over.");
        }

        private static void Program34()
        {
            Game.ShowSceneAndAddTurtle();

            var player = Game.AddPlayer(PlayerData.Ant.WithPosition(100, 0));
            Turtle.Say("Press <Space> to hide player");
            Game.WaitForKeyDown(KeyboardKey.Space);
            Turtle.ShutUp();
            Turtle.Hide();
            player.Hide();
            Turtle.Say("You won't see that message.", 2);
            Turtle.Show();
            Turtle.Say("Press <Space> to show player.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            player.Show();
            Turtle.Say("Done.");
        }

        private static void Program35()
        {
            Game.ShowSceneAndAddTurtle();

            var player = Game.AddPlayer(PlayerData.Ant.WithPosition(400, 200));
            for (int i = 0; i < 7; i++)
            {
                player.MoveLeft(100);
                Turtle.Say($"Distance: {Turtle.GetDistanceTo(player):F2}\r\nAngle: {Turtle.GetDirectionTo(player):F2}");
                player.Say("Press <Space> to continue.");
                Game.WaitForKeyDown(KeyboardKey.Space);
            }
            Turtle.Say("Done.");
            player.ShutUp();
        }

        private static void Program36()
        {
            Game.ShowSceneAndAddTurtle();

            if (Turtle.AskBool("Continue?"))
            {
                if (Turtle.AskBool("Ok, let's do it. Move right?"))
                {
                    Turtle.MoveRight(100);
                }
            }
            else
            {
                Turtle.Say("Ok. Bye bye.");
            }
        }
    }
}
