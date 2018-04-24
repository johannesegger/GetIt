using System;
using PlayAndLearn.Models;

namespace PlayAndLearn.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.ShowScene();
            var player = Zombie.CreateDefault();
            Game.AddSprite(player);

            for (int i = 0; i < 3; i++)
            {
                player.MoveRight(10);
                Game.SleepSeconds(1);
                player.MoveUp(10);
                Game.SleepSeconds(1);
                player.MoveLeft(10);
                Game.SleepSeconds(1);
                player.MoveDown(10);
                Game.SleepSeconds(1);
            }
        }
    }
}
