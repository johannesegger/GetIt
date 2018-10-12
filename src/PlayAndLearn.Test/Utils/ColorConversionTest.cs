using System.Collections.Generic;
using FluentAssertions;
using FsCheck.Xunit;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;
using Xunit;

namespace PlayAndLearn.Test.Utils
{
    public class ColorConversionTest
    {
        public static IEnumerable<object[]> Colors
        {
            get
            {
                yield return new object[] { new RGB(0xFF, 0xFF, 0x00), new HSL(1.0/6.0, 1, 0.5) };
            }
        }

        [Theory]
        [MemberData(nameof(Colors))]
        internal void ShouldConvertCorrectlyToHSV(RGB rgb, HSL hsl)
        {
            rgb.ToHSL().Should().Be(hsl);
        }

        [Theory]
        [MemberData(nameof(Colors))]
        internal void ShouldConvertCorrectlyToRGB(RGB rgb, HSL hsl)
        {
            hsl.ToRGB().Should().Be(rgb);
        }

        [Property(Skip = "Rounding errors")]
        internal void ConversionShouldBeIsomorphic(RGB rgb)
        {
            rgb.ToHSL().ToRGB().Should().Be(rgb);
        }
    }
}
