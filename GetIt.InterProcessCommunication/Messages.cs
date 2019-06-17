using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace GetIt.Message
{
    public static class Size
    {

        public static Ui.Size FromDomain(GetIt.Size p)
        {
            return new Ui.Size { Width = p.Width, Height = p.Height };
        }

        public static GetIt.Size ToDomain(Ui.Size p)
        {
            return new GetIt.Size(p.Width, p.Height);
        }
    }

    public static class WindowSize
    {
        public static Ui.WindowSize FromDomain(GetIt.WindowSize p)
        {
            if (p.IsSpecificSize)
            {
                var size = ((GetIt.WindowSize.SpecificSize)p).Item;
                return new Ui.WindowSize { Size = Size.FromDomain(size) };
            }
            else if (p.IsMaximized)
            {
                return new Ui.WindowSize { IsNone = true };
            }
            throw new ArgumentException("Unknown scene size type", nameof(p));
        }

        public static GetIt.WindowSize ToDomain(Ui.WindowSize p)
        {
            if (p.WindowSizeCase == Ui.WindowSize.WindowSizeOneofCase.Size)
            {
                return GetIt.WindowSize.NewSpecificSize(Size.ToDomain(p.Size));
            }
            else if (p.WindowSizeCase == Ui.WindowSize.WindowSizeOneofCase.IsNone)
            {
                return GetIt.WindowSize.Maximized;
            }
            throw new ArgumentException("Unknown scene size type", nameof(p));
        }
    }

    public static class Position
    {
        public static Ui.Position FromDomain(GetIt.Position p)
        {
            return new Ui.Position { X = p.X, Y = p.Y };
        }

        public static GetIt.Position ToDomain(Ui.Position p)
        {
            return new GetIt.Position(p.X, p.Y);
        }
    }

    public static class Rectangle
    {
        public static Ui.Rectangle FromDomain(GetIt.Rectangle p)
        {
            return new Ui.Rectangle { Position = Position.FromDomain(p.Position), Size = Size.FromDomain(p.Size) };
        }

        public static GetIt.Rectangle ToDomain(Ui.Rectangle p)
        {
            return new GetIt.Rectangle(Position.ToDomain(p.Position), Size.ToDomain(p.Size));
        }
    }

    public static class WindowTitle
    {
        public static Ui.WindowTitle FromDomain(FSharpOption<string> p)
        {
            if (FSharpOption<string>.get_IsSome(p))
            {
                return new Ui.WindowTitle { Title = p.Value };
            }
            return new Ui.WindowTitle { IsNone = true };
        }

        public static FSharpOption<string> ToDomain(Ui.WindowTitle p)
        {
            return OptionModule.OfObj(p.Title);
        }
    }

    public static class SvgImage
    {
        public static Ui.SvgImage FromDomain(GetIt.SvgImage p)
        {
            return new Ui.SvgImage { Size = Size.FromDomain(p.Size), Data = p.SvgData };
        }

        public static GetIt.SvgImage ToDomain(Ui.SvgImage p)
        {
            return new GetIt.SvgImage(Size.ToDomain(p.Size), p.Data);
        }
    }

    public static class PngImage
    {
        public static Ui.PngImage FromDomain(GetIt.PngImage p)
        {
            return new Ui.PngImage { Data = Google.Protobuf.ByteString.CopyFrom(p.Item) };
        }

        public static GetIt.PngImage ToDomain(Ui.PngImage p)
        {
            return GetIt.PngImage.NewPngImage(p.Data.ToByteArray());
        }
    }

    public static class PlayerId
    {
        public static Ui.PlayerId FromDomain(GetIt.PlayerId p)
        {
            return new Ui.PlayerId { Value = p.Item.ToString() };
        }

        public static GetIt.PlayerId ToDomain(Ui.PlayerId p)
        {
            return GetIt.PlayerId.NewPlayerId(Guid.Parse(p.Value));
        }
    }

    public static class RGBAColor
    {
        public static Ui.RGBAColor FromDomain(GetIt.RGBAColor p)
        {
            return new Ui.RGBAColor { Red = p.Red, Green = p.Green, Blue = p.Blue, Alpha = p.Alpha };
        }

        public static GetIt.RGBAColor ToDomain(Ui.RGBAColor p)
        {
            return new GetIt.RGBAColor((byte)p.Red, (byte)p.Green, (byte)p.Blue, (byte)p.Alpha);
        }
    }

    public static class Pen
    {
        public static Ui.Pen FromDomain(GetIt.Pen p)
        {
            return new Ui.Pen { IsOn = p.IsOn, Weight = p.Weight, Color = RGBAColor.FromDomain(p.Color) };
        }

        public static GetIt.Pen ToDomain(Ui.Pen p)
        {
            return new GetIt.Pen(p.IsOn, p.Weight, RGBAColor.ToDomain(p.Color));
        }
    }

    public static class SpeechBubble
    {
        public static Ui.SpeechBubble FromDomain(GetIt.SpeechBubble p)
        {
            if (p.IsSay)
            {
                return new Ui.SpeechBubble { SayText = ((GetIt.SpeechBubble.Say)p).Item };
            }
            else if (p.IsAskString)
            {
                return new Ui.SpeechBubble { AskStringText = ((GetIt.SpeechBubble.AskString)p).Item };
            }
            else if (p.IsAskBool)
            {
                return new Ui.SpeechBubble { AskBoolText = ((GetIt.SpeechBubble.AskBool)p).Item };
            }
            throw new ArgumentException("Unknown speech bubble type", nameof(p));
        }

        public static GetIt.SpeechBubble ToDomain(Ui.SpeechBubble p)
        {
            if (p.SpeechBubbleTypeCase == Ui.SpeechBubble.SpeechBubbleTypeOneofCase.SayText)
            {
                return GetIt.SpeechBubble.NewSay(p.SayText);
            }
            else if (p.SpeechBubbleTypeCase == Ui.SpeechBubble.SpeechBubbleTypeOneofCase.AskStringText)
            {
                return GetIt.SpeechBubble.NewAskString(p.AskStringText);
            }
            else if (p.SpeechBubbleTypeCase == Ui.SpeechBubble.SpeechBubbleTypeOneofCase.AskBoolText)
            {
                return GetIt.SpeechBubble.NewAskBool(p.AskBoolText);
            }
            throw new ArgumentException("Unknown speech bubble type", nameof(p));
        }
    }

    public static class OptionalSpeechBubble
    {
        public static Ui.OptionalSpeechBubble FromDomain(FSharpOption<GetIt.SpeechBubble> p)
        {
            if (FSharpOption<GetIt.SpeechBubble>.get_IsSome(p))
            {
                return new Ui.OptionalSpeechBubble { Data = SpeechBubble.FromDomain(p.Value) };
            }
            return new Ui.OptionalSpeechBubble { IsNone = true };
        }

        public static FSharpOption<GetIt.SpeechBubble> ToDomain(Ui.OptionalSpeechBubble p)
        {
            if (p.SpeechBubbleCase == Ui.OptionalSpeechBubble.SpeechBubbleOneofCase.IsNone)
            {
                return FSharpOption<GetIt.SpeechBubble>.None;
            }
            else if (p.SpeechBubbleCase == Ui.OptionalSpeechBubble.SpeechBubbleOneofCase.Data)
            {
                return FSharpOption<GetIt.SpeechBubble>.Some(SpeechBubble.ToDomain(p.Data));
            }
            throw new ArgumentException("Unknown speech bubble type", nameof(p));
        }
    }

    public static class Degrees
    {
        public static double FromDomain(GetIt.Degrees p)
        {
            return DegreesModule.value(p);
        }

        public static GetIt.Degrees ToDomain(double p)
        {
            return p;
        }
    }

    public static class PlayerData
    {
        public static Ui.PlayerData FromDomain(GetIt.PlayerData p)
        {
            return new Ui.PlayerData {
                SizeFactor = p.SizeFactor,
                Position = Position.FromDomain(p.Position),
                Direction = Degrees.FromDomain(p.Direction),
                Pen = Pen.FromDomain(p.Pen),
                SpeechBubble = OptionalSpeechBubble.FromDomain(p.SpeechBubble),
                Costumes = { p.Costumes.Select(SvgImage.FromDomain) },
                CostumeIndex = p.CostumeIndex,
                Layer = p.Layer,
                IsVisible = p.IsVisible
            };
        }

        public static GetIt.PlayerData ToDomain(Ui.PlayerData p)
        {
            return new GetIt.PlayerData(
                p.SizeFactor,
                Position.ToDomain(p.Position),
                Degrees.ToDomain(p.Direction),
                Pen.ToDomain(p.Pen),
                OptionalSpeechBubble.ToDomain(p.SpeechBubble),
                SeqModule.ToList(p.Costumes.Select(SvgImage.ToDomain)),
                p.CostumeIndex,
                p.Layer,
                p.IsVisible
            );
        }
    }

    public static class Player
    {
        public static Ui.Player FromDomain(Tuple<GetIt.PlayerId, GetIt.PlayerData> p)
        {
            return new Ui.Player {
                PlayerId = PlayerId.FromDomain(p.Item1),
                PlayerData = PlayerData.FromDomain(p.Item2)
            };
        }

        public static Tuple<GetIt.PlayerId, GetIt.PlayerData> ToDomain(Ui.Player p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                PlayerData.ToDomain(p.PlayerData)
            );
        }
    }

    public static class PlayerPosition
    {
        public static Ui.PlayerPosition FromDomain(Tuple<GetIt.PlayerId, GetIt.Position> p)
        {
            return new Ui.PlayerPosition {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Position = Position.FromDomain(p.Item2)
            };
        }

        public static Tuple<GetIt.PlayerId, GetIt.Position> ToDomain(Ui.PlayerPosition p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                Position.ToDomain(p.Position)
            );
        }
    }

    public static class PlayerDirection
    {
        public static Ui.PlayerDirection FromDomain(Tuple<GetIt.PlayerId, GetIt.Degrees> p)
        {
            return new Ui.PlayerDirection {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Direction = Degrees.FromDomain(p.Item2)
            };
        }

        public static Tuple<GetIt.PlayerId, GetIt.Degrees> ToDomain(Ui.PlayerDirection p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                Degrees.ToDomain(p.Direction)
            );
        }
    }

    public static class PlayerText
    {
        public static Ui.PlayerText FromDomain(Tuple<GetIt.PlayerId, string> p)
        {
            return new Ui.PlayerText {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Text = p.Item2
            };
        }

        public static Tuple<GetIt.PlayerId, string> ToDomain(Ui.PlayerText p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                p.Text
            );
        }
    }

    public static class StringAnswer
    {
        public static Ui.StringAnswer FromDomain(string p)
        {
            return new Ui.StringAnswer { Text = p };
        }

        public static string ToDomain(Ui.StringAnswer p)
        {
            return p.Text;
        }
    }

    public static class BoolAnswer
    {
        public static Ui.BoolAnswer FromDomain(bool p)
        {
            return new Ui.BoolAnswer { Value = p };
        }

        public static bool ToDomain(Ui.BoolAnswer p)
        {
            return p.Value;
        }
    }

    public static class PlayerPenState
    {
        public static Ui.PlayerPenState FromDomain(Tuple<GetIt.PlayerId, bool> p)
        {
            return new Ui.PlayerPenState {
                PlayerId = PlayerId.FromDomain(p.Item1),
                IsOn = p.Item2
            };
        }

        public static Tuple<GetIt.PlayerId, bool> ToDomain(Ui.PlayerPenState p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                p.IsOn
            );
        }
    }

    public static class PlayerPenColor
    {
        public static Ui.PlayerPenColor FromDomain(Tuple<GetIt.PlayerId, GetIt.RGBAColor> p)
        {
            return new Ui.PlayerPenColor {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Color = RGBAColor.FromDomain(p.Item2)
            };
        }

        public static Tuple<GetIt.PlayerId, GetIt.RGBAColor> ToDomain(Ui.PlayerPenColor p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                RGBAColor.ToDomain(p.Color)
            );
        }
    }

    public static class PlayerPenColorShift
    {
        public static Ui.PlayerPenColorShift FromDomain(Tuple<GetIt.PlayerId, GetIt.Degrees> p)
        {
            return new Ui.PlayerPenColorShift {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Degrees = Degrees.FromDomain(p.Item2)
            };
        }

        public static Tuple<GetIt.PlayerId, GetIt.Degrees> ToDomain(Ui.PlayerPenColorShift p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                Degrees.ToDomain(p.Degrees)
            );
        }
    }

    public static class PlayerPenWeight
    {
        public static Ui.PlayerPenWeight FromDomain(Tuple<GetIt.PlayerId, double> p)
        {
            return new Ui.PlayerPenWeight {
                PlayerId = PlayerId.FromDomain(p.Item1),
                Weight = p.Item2
            };
        }

        public static Tuple<GetIt.PlayerId, double> ToDomain(Ui.PlayerPenWeight p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                p.Weight
            );
        }
    }

    public static class PlayerSizeFactor
    {
        public static Ui.PlayerSizeFactor FromDomain(Tuple<GetIt.PlayerId, double> p)
        {
            return new Ui.PlayerSizeFactor {
                PlayerId = PlayerId.FromDomain(p.Item1),
                SizeFactor = p.Item2
            };
        }

        public static Tuple<GetIt.PlayerId, double> ToDomain(Ui.PlayerSizeFactor p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                p.SizeFactor
            );
        }
    }

    public static class PlayerVisibility
    {
        public static Ui.PlayerVisibility FromDomain(Tuple<GetIt.PlayerId, bool> p)
        {
            return new Ui.PlayerVisibility {
                PlayerId = PlayerId.FromDomain(p.Item1),
                IsVisible = p.Item2
            };
        }

        public static Tuple<GetIt.PlayerId, bool> ToDomain(Ui.PlayerVisibility p)
        {
            return Tuple.Create(
                PlayerId.ToDomain(p.PlayerId),
                p.IsVisible
            );
        }
    }

    public static class MouseButton
    {
        public static Ui.MouseButton FromDomain(GetIt.MouseButton p)
        {
            if (p == GetIt.MouseButton.Primary)
            {
                return Ui.MouseButton.Primary;
            }
            else if (p == GetIt.MouseButton.Secondary)
            {
                return Ui.MouseButton.Secondary;
            }
            throw new ArgumentException("Unknown mouse button", nameof(p));
        }

        public static GetIt.MouseButton ToDomain(Ui.MouseButton p)
        {
            if (p == Ui.MouseButton.Primary)
            {
                return GetIt.MouseButton.Primary;
            }
            else if (p == Ui.MouseButton.Secondary)
            {
                return GetIt.MouseButton.Secondary;
            }
            throw new ArgumentException("Unknown mouse button", nameof(p));
        }
    }

    public static class VirtualScreenMouseClick
    {
        public static Ui.VirtualScreenMouseClick FromDomain(GetIt.VirtualScreenMouseClick p)
        {
            return new Ui.VirtualScreenMouseClick {
                Button = MouseButton.FromDomain(p.Button),
                VirtualScreenPosition = Position.FromDomain(p.VirtualScreenPosition)
            };
        }

        public static GetIt.VirtualScreenMouseClick ToDomain(Ui.VirtualScreenMouseClick p)
        {
            return new GetIt.VirtualScreenMouseClick(
                MouseButton.ToDomain(p.Button),
                Position.ToDomain(p.VirtualScreenPosition)
            );
        }
    }

    public static class MouseClick
    {
        public static Ui.MouseClick FromDomain(GetIt.MouseClick p)
        {
            return new Ui.MouseClick {
                Button = MouseButton.FromDomain(p.Button),
                Position = Position.FromDomain(p.Position)
            };
        }

        public static GetIt.MouseClick ToDomain(Ui.MouseClick p)
        {
            return new GetIt.MouseClick(
                MouseButton.ToDomain(p.Button),
                Position.ToDomain(p.Position)
            );
        }
    }
}