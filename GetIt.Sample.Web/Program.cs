using System;
using GetIt;

namespace GetIt.Sample.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            // Game.ShowScene();
            // Game.ShowSceneAndAddTurtle();
            // Game.ShowScene(300, 200);
            // Game.ShowMaximizedScene();
            // Game.AddPlayer(PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.SteelBlue, new Size(100, 50))).WithDirection(45));
            // Game.AddPlayer(PlayerData.Turtle.WithDirection(45));

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
    }
}
