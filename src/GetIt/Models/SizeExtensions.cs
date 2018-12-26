using System;

namespace GetIt
{
    /// <summary>
    /// Defines extension methods for `Size`.
    /// </summary>
    public static class SizeExtensions
    {
        /// <summary>
        /// Scales a given size so that it fits into a box.
        /// Preserves the original ratio.
        /// </summary>
        /// <param name="size">The size that should be scaled.</param>
        /// <param name="box">The bounding box that constrains the original size.</param>
        /// <returns>The scaled size.</returns>
        public static Size Scale(this Size size, Size box)
        {
            var widthRatio = box.Width / size.Width;
            var heightRatio = box.Height / size.Height;
            var ratio = Math.Min(widthRatio, heightRatio);
            return size * ratio;
        }
    }
}