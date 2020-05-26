using System;
using System.Collections.Generic;
using System.Linq;

namespace GetIt.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            TurtleGraphics();
            // TemporaryKeyboardEventHandlers();
            // MouseEnterHandlers();
            // MouseClickHandler();
            // SpeechBubbleWithUnicodeCharactersAndDynamicSize();
            // MouseDistanceAndDirection();
            // PingPong();
            // AskString();
            // WaitForMouseClick();
            // WaitForKeyDown();
            // MultipleKeysDown();
            // PredefinedPlayers();
            // KeyDownWhileSpeechBubble();
            // SnakeLight();
            // MousePosition();
            // SelectRandomColor();
            // WindowTitle();
            // PressedKeys();
            // PlayerLayer();
            // Snake();
            // DistanceAndDirectionToOtherPlayer();
            // AskBool();
            // PenLinesPerformance();
            // SpeechBubbleAlignment();
        }

        private static void TurtleGraphics()
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

        private static void TemporaryKeyboardEventHandlers()
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

        private static void MouseEnterHandlers()
        {
            Game.ShowSceneAndAddTurtle();
            Turtle.Say("Try and catch me");
            Turtle.OnMouseEnter(player => player.ShutUp());
            Turtle.OnMouseEnter(player => player.MoveToRandomPosition());
        }

        private static void MouseClickHandler()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Try and hit me, sucker!", 2);
            Turtle.OnClick((player, mouseButton) => player.Say("Ouch, that hurts!", 2));
        }

        private static void SpeechBubbleWithUnicodeCharactersAndDynamicSize()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Sleep(1000);

            Turtle.Say("🎉✔👍👋👍🏽", 2);

            for (int i = 0; i < 500; i++)
            {
                Turtle.Say(new string(Enumerable.Range(0, i).Select(j => (char)('A' + j)).ToArray()));
                Turtle.Sleep(20);
            }
        }

        private static void MouseDistanceAndDirection()
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

        private static void PingPong()
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

        private static void AskString()
        {
            Game.ShowSceneAndAddTurtle();

            int age;
            string input = Turtle.Ask("How old are you?");
            while (!int.TryParse(input, out age))
            {
                Turtle.Sleep(500);
                input = Turtle.Ask("Are you kidding? That's not a number. How old are you?");
            }
            Turtle.Say($"{age}? You're looking good for your age!");
        }

        private static void WaitForMouseClick()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Click somewhere");
            var clickEvent = Game.WaitForMouseClick();
            Turtle.Say($"You clicked with mouse button {clickEvent.Button} at {clickEvent.Position}");
        }

        private static void WaitForKeyDown()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("Press any key to start");
            var key = Game.WaitForAnyKeyDown();
            Turtle.Say($"You started with <{key}>. Press <Space> to stop.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            Turtle.Say("Game over.");
        }

        private static void MultipleKeysDown()
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

        private static void PredefinedPlayers()
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

        private static void KeyDownWhileSpeechBubble()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.OnKeyDown(KeyboardKey.Left, p => p.MoveLeft(10));

            Turtle.Say("Sleeping");
            Turtle.Sleep(5000);

            var name = Turtle.Ask("What's your name?");

            Turtle.Say($"Hi, {name}");
        }

        private static void SnakeLight()
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

        private static void MousePosition()
        {
            Game.ShowSceneAndAddTurtle();
            while (true)
            {
                Turtle.Say($"Mouse position: {Game.MousePosition}");
                Game.Sleep(50);
            }
        }

        private static void SelectRandomColor()
        {
            Game.ShowSceneAndAddTurtle();

            var color = RGBAColor.SelectRandom(RGBAColors.Red, RGBAColors.Green, RGBAColors.Blue);
            Turtle.Say($"Chose color {color}");
            Turtle.TurnOnPen();
            Turtle.SetPenColor(color);
            Turtle.SetPenWeight(5);
            Turtle.MoveRight(100);
        }

        private static void WindowTitle()
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

        private static void PressedKeys()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveRight(100);
            Turtle.Say("Press and hold <Space>.");
            Turtle.OnKeyDown(KeyboardKey.Space, TimeSpan.FromSeconds(1), (p, i) => p.Say($"Event handler called {i} time(s)."));

            var player = Game.AddPlayer(PlayerData.Turtle.WithPosition(-100, 0));
            player.Say("Press and hold any key.");
            player.OnAnyKeyDown(TimeSpan.FromSeconds(1), (p, key, i) => p.Say($"Event handler called {i} time(s) with key {key}."));
        }

        private static void PlayerLayer()
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

        private static void Snake()
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
                using (Game.BatchCommands())
                {
                    MoveSnake(10);
                }
                CheckIfSnakeEatsFood();
                if (CheckIfSnakeHitsWall())
                {
                    break;
                }

                Game.Sleep(50);
            }

            snakeHead.Say("Game over.");
        }

        private static void DistanceAndDirectionToOtherPlayer()
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

        private static void AskBool()
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

        private static void PenLinesPerformance()
        {
            Game.ShowScene();
            Player player = Game.AddPlayer(PlayerData.Ant);
            player.SetSizeFactor(3);

            while (true)
            {
                player.MoveToRandomPosition();
                player.TurnOnPen();
                player.SetPenColor(RGBAColors.Red);
                player.SetPenWeight(3);
                for (int i = 0; i < 100; i++)
                {
                    player.MoveInDirection(1);
                    player.ShiftPenColor(1);
                }
                player.SetDirection(Directions.Down);
                for (int i = 0; i < 200; i++)
                {
                    player.MoveInDirection(1);
                    player.ShiftPenColor(1);
                }
                while (player.Direction > 90)
                {
                    player.MoveInDirection(1);
                    player.RotateClockwise(1);
                    player.ShiftPenColor(1);
                }
                player.TurnOffPen();
                player.SetDirection(Directions.Right);
                player.Sleep(1000);
            }
        }

        private static void SpeechBubbleAlignment()
        {
            Game.ShowSceneAndAddTurtle();
            Game.AddPlayer(PlayerData.Ant.WithPosition(100, 100));
            string text = "";
            Turtle.OnAnyKeyDown(TimeSpan.FromMilliseconds(200), (p, key, i) =>
            {
                if (key == KeyboardKey.Left)
                {
                    Turtle.MoveLeft(10);
                }
                else if (key == KeyboardKey.Right)
                {
                    Turtle.MoveRight(10);
                }
                else if (key == KeyboardKey.Up)
                {
                    Turtle.MoveUp(10);
                }
                else if (key == KeyboardKey.Down)
                {
                    Turtle.MoveDown(10);
                }
                else if (key == KeyboardKey.Escape)
                {
                    text = text.Substring(0, Math.Max(text.Length - 1, 0));
                }
                else if (key == KeyboardKey.A)
                {
                    text += "A";
                }
                else if (key == KeyboardKey.S)
                {
                    text += new string('S', 10);
                }
                else if (key == KeyboardKey.Enter)
                {
                    text += Environment.NewLine;
                }
                Turtle.Say(text);
            });
        }
    }
}
