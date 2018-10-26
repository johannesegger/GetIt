using System;

namespace GetIt
{
    public static class SizeExtensions
    {
        public static Size Scale(this Size size, Size box)
        {
            var widthRatio = box.Width / size.Width;
            var heightRatio = box.Height / size.Height;
            var ratio = Math.Min(widthRatio, heightRatio);
            return size * ratio;
        }
    }
}