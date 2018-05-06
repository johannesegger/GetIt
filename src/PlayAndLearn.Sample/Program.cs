using System;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

namespace PlayAndLearn.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.ShowScene();
            var player = Turtle.CreateDefault();
            Game.AddSprite(player);

            player.Position = new Position(0, 0);
            player.Pen = new Pen(1.5, new RGB(0x00, 0xFF, 0xFF));
            var n = 5;
            while (n < 800)
            {
                player.Go(n);
                player.Rotate(89.5);

                player.Pen = player.Pen.WithHueShift(10.0/360);
                n++;

                Game.SleepMilliseconds(10);
            }
        }
    }
}
