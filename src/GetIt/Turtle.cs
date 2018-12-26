using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace GetIt
{
    /// <summary>
    /// Defines methods to interact with the default turtle.
    /// </summary>
    public static partial class Turtle
    {
        internal static readonly Player DefaultPlayer = Player.CreateTurtle();

        private static PlayerOnScene @default;
        internal static PlayerOnScene Default
        {
            get
            {
                return @default ?? throw new Exception($"Default player hasn't been added to the scene. Consider calling `{nameof(Game)}.{nameof(Game.ShowSceneAndAddTurtle)}` at the beginning.");
            }

            set
            {
                if (@default != null)
                {
                    throw new Exception("Default player has already been set.");
                }
                @default = value;
            }
        }

        /// <summary>
        /// The actual size of the player.
        /// </summary>
        public static Size Size => Default.Size;

        /// <summary>
        /// The factor that is used to resize the player.
        /// </summary>
        public static double SizeFactor => Default.SizeFactor;

        /// <summary>
        /// The position of the player.
        /// </summary>
        public static Position Position => Default.Position;

        /// <summary>
        /// The position of the player.
        /// </summary>
        public static Rectangle Bounds => Default.Bounds;

        /// <summary>
        /// The direction of the player.
        /// </summary>
        public static Degrees Direction => Default.Direction;

        /// <summary>
        /// The pen of the player.
        /// </summary>
        public static Pen Pen => Default.Pen;
    }
}