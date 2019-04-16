using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static MoreLinq.Extensions.ForEachExtension;
using static MoreLinq.Extensions.MaxByExtension;
using static MoreLinq.Extensions.PipeExtension;
using static MoreLinq.Extensions.ScanExtension;
using static MoreLinq.Extensions.ShuffleExtension;
using static MoreLinq.Extensions.WindowExtension;

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
            Program30();
        }

        private static void Program1()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.MoveTo(0, 0);
            Turtle.SetPenWeight(1.5);
            Turtle.SetPenColor(RGBAColors.Cyan.WithAlpha(0x80));
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

            Turtle.Say("I'll try to catch you.", 2);

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
                PlayerData.Create(
                    Costume.CreateRectangle(
                        RGBAColors.DarkMagenta,
                        new Size(20, 150))),
                controlLeftPlayer);

            void controlRightPlayer(Player player)
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
                PlayerData.Create(
                    Costume.CreateRectangle(
                        RGBAColors.Magenta,
                        new Size(20, 150))),
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
                    Costume.CreateCircle(RGBAColors.Black.WithAlpha(128), 10)),
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
            Turtle.Say($"You clicked with mouse button {clickEvent.MouseButton} at {clickEvent.Position}");
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
                    Costume.CreatePolygon(
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

            var ant = Game.AddPlayer(PlayerData.Ant);
            ant.MoveRight(100);

            var bug = Game.AddPlayer(PlayerData.Bug);
            bug.MoveRight(200);

            var spider = Game.AddPlayer(PlayerData.Spider);
            spider.MoveRight(300);

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

        [Equals]
        private class City
        {
            public City(Guid id, Position position)
            {
                Id = id;
                Position = position;
            }

            public Guid Id { get; }
            [IgnoreDuringEquals] public Position Position { get; }
        }

        private class Individual
        {
            public Individual(ImmutableList<City> tour, double fitness)
            {
                Tour = tour;
                Fitness = fitness;
            }

            public ImmutableList<City> Tour { get; }
            public double Fitness { get; }
        }

        private static void Program22()
        {
            Game.ShowSceneAndAddTurtle();

            Turtle.Say("TSP solver", 1);

            var rand = new Random();
            var numberOfCities = 30;
            var cityPlayers = Enumerable
                .Range(0, numberOfCities)
                .Select(_ => Game.AddPlayer(PlayerData.Create(Costume.CreateCircle(RGBAColors.DarkRed, 5))))
                .ToList();
            cityPlayers
                .ForEach(p => p.MoveToRandomPosition());
            var cities = cityPlayers
                .Select(p => new City(Guid.NewGuid(), p.Position))
                .ToList();

            var iterationDelayMs = 500;
            Turtle.OnKeyDown(KeyboardKey.Down, _ => iterationDelayMs *= 2);
            Turtle.OnKeyDown(KeyboardKey.Up, _ => iterationDelayMs /= 2);

            double GetDistance(City a, City b)
            {
                var dx = a.Position.X - b.Position.X;
                var dy = a.Position.Y - b.Position.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            double CalculateFitness(IReadOnlyList<City> individual)
            {
                if (individual.Count < 2)
                {
                    return 0;
                }

                var distance = individual
                    .Concat(new [] { individual[0] })
                    .Window(2)
                    .Sum(b => GetDistance(b[0], b[1]));
                return -distance;
            }

            void DrawTour(IReadOnlyList<City> tour)
            {
                if (tour.Count < 2)
                {
                    return;
                }
                Turtle.TurnOffPen();
                foreach (var city in tour.Concat(new [] { tour[0] }))
                {
                    Turtle.MoveTo(city.Position);
                    Turtle.TurnOnPen();
                }
            }

            Individual TournamentSelect(IEnumerable<Individual> individuals, int size)
            {
                return individuals
                    .Shuffle()
                    .Take(size)
                    .MaxBy(p => p.Fitness)
                    .First();
            }

            Individual OrderCrossover(Individual p1, Individual p2)
            {
                var startIdx = rand.Next(0, p1.Tour.Count);
                var endIdx = rand.Next(0, p1.Tour.Count);
                if (startIdx <= endIdx)
                {
                    var p1Tour = p1.Tour.Skip(startIdx).Take(endIdx - startIdx).ToList();
                    var p2RemainingTour = p2.Tour.Except(p1Tour).ToList();
                    var tour = p2RemainingTour
                        .Take(startIdx)
                        .Concat(p1Tour)
                        .Concat(p2RemainingTour.Skip(startIdx))
                        .ToImmutableList();
                    return new Individual(tour, CalculateFitness(tour));
                }
                else
                {
                    var p1Tour1 = p1.Tour.Take(endIdx + 1).ToList();
                    var p1Tour2 = p1.Tour.Skip(startIdx).ToList();
                    var p2RemainingTour = p2.Tour.Except(p1Tour1).Except(p1Tour2).ToList();
                    var tour = p1Tour1
                        .Concat(p2RemainingTour)
                        .Concat(p1Tour2)
                        .ToImmutableList();
                    return new Individual(tour, CalculateFitness(tour));
                }
            }

            Individual TwoOptChange(Individual p, double probability)
            {
                if (rand.NextDouble() < probability)
                {
                    var idx1 = rand.Next(0, p.Tour.Count);
                    var idx2 = (idx1 + 1) % p.Tour.Count;
                    var tour = p.Tour
                        .SetItem(idx1, p.Tour[idx2])
                        .SetItem(idx2, p.Tour[idx1]);
                    return new Individual(tour, CalculateFitness(tour));
                }
                return p;
            }

            double GetGeneticDiversity(IReadOnlyCollection<Individual> population)
            {
                var maxEdges = Enumerable.Range(0, numberOfCities).Sum();
                var minEdges = numberOfCities;
                var edges = population
                    .SelectMany(individual => individual.Tour
                        .Append(individual.Tour.First())
                        .Window(2)
                        .Select(b => b[0].Id.CompareTo(b[1].Id) < 0 ? (b[0], b[1]) : (b[1], b[0])))
                    .Distinct()
                    .Count();
                return (double)(edges - minEdges) / (maxEdges - minEdges);
            }

            var populationSize = 500;
            var iterations = 1000;
            var mutationProbability = 0.05;

            var initialPopulation = Enumerable
                .Range(0, populationSize)
                .Select(_ => cities.Shuffle().ToImmutableList())
                .Select(tour => new Individual(tour, CalculateFitness(tour)))
                .ToImmutableList();

            Enumerable
                .Range(0, iterations)
                .Scan(initialPopulation, (population, iteration) =>
                {
                    return Enumerable
                        .Range(0, populationSize)
                        .Select(_ =>
                        {
                            var tournamentSize = 3;
                            var parent1 = TournamentSelect(population, tournamentSize);
                            var parent2 = TournamentSelect(population, tournamentSize);
                            var child = OrderCrossover(parent1, parent2);
                            return TwoOptChange(child, mutationProbability);
                        })
                        .ToImmutableList();
                })
                .ForEach((population, index) =>
                {
                    Game.ClearScene();
                    var fittest = population.MaxBy(individual => individual.Fitness).First();
                    var lines = new[] {
                        $"Iteration: {index}",
                        $"Min distance: {Math.Abs(fittest.Fitness)}",
                        $"Genetic diversity: {GetGeneticDiversity(population):P2}"
                    };
                    Turtle.Say(string.Join(Environment.NewLine, lines));
                    DrawTour(fittest.Tour);
                    Turtle.Sleep(iterationDelayMs);
                });
        }

        private static void Program23()
        {
            Game.ShowSceneAndAddTurtle();

            using (var food = Game.AddPlayer(PlayerData.Bug))
            {
                food.Say("I'm just here for 2 more seconds", 1);
                food.MoveToRandomPosition();
                food.Sleep(2000);
            }
        }

        private static void Program24()
        {
            Game.ShowScene();
            Game.AddPlayer(PlayerData.Create(Costume.Load(@"assets\Turtle2.svg")));
        }

        private static void Program25()
        {
            Game.ShowSceneAndAddTurtle();
            while (true)
            {
                Turtle.Say($"Mouse position: {Game.MousePosition}");
                Game.Sleep(50);
            }
        }

        private static void Program26()
        {
            Game.ShowScene(1000, 350);
        }

        private static void Program27()
        {
            Game.ShowMaximizedScene();
        }

        private static void Program28()
        {
            Game.ShowSceneAndAddTurtle(1000, 350);
        }

        private static void Program29()
        {
            Game.ShowMaximizedSceneAndAddTurtle();
        }

        private static void Program30()
        {
            Game.ShowSceneAndAddTurtle(Background.Baseball1.Size.Width * 2, Background.Baseball1.Size.Height * 2);
            Game.SetBackground(Background.Baseball1);
            Turtle.TurnOnPen();
            Turtle.MoveTo(50, 100);
            Turtle.Say("Press <Space> to reset the background.");
            Game.WaitForKeyDown(KeyboardKey.Space);
            Turtle.ShutUp();
            Game.SetBackground(Background.None);
        }
    }
}
