using System;
using GetIt;

namespace GetIt.Sample.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.ShowScene();
            // Game.ShowSceneAndAddTurtle();
            // Game.ShowScene(300, 200);
            // Game.ShowMaximizedScene();
            Game.AddPlayer(PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.SteelBlue, new Size(100, 50))).WithDirection(45));
            // Game.AddPlayer(PlayerData.Turtle.WithDirection(45));
        }
    }
}
