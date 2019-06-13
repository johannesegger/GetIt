using System;

namespace GetIt.Sample.Print
{
    class Program
    {
        static void Main(string[] args)
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
            }

            // Game.Print(PrintConfig.Create("print-template.html", "Brother HL-5140 series").Set("name", "Johannes Egger"));
            Environment.SetEnvironmentVariable("GET_IT_PRINT_CONFIG", "{ \"templatePath\": \"GetIt.Sample.Print\\\\print-template.html\", \"printerName\": \"Microsoft Print to PDF\" }");
            Game.Print(PrintConfig.CreateFromEnvironment().Set("name", "Johannes Egger"));
            Turtle.Say("Done.");
        }
    }
}
