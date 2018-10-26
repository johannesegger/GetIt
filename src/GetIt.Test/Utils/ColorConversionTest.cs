using System.Collections.Generic;
using FluentAssertions;
using FsCheck.Xunit;
using GetIt.Models;
using GetIt.Utils;
using Xunit;

namespace GetIt.Test.Utils
{
    public class ColorConversionTest
    {
        public static IEnumerable<object[]> Colors
        {
            get
            {
                yield return new object[] { new RGBA(0xFF, 0xFF, 0x00, 0xff), new HSLA(1.0/6.0, 1, 0.5, 1) };
            }
        }

        [Theory]
        [MemberData(nameof(Colors))]
        internal void ShouldConvertCorrectlyToHSV(RGBA rgb, HSLA hsl)
        {
            rgb.ToHSLA().Should().Be(hsl);
        }

        [Theory]
        [MemberData(nameof(Colors))]
        internal void ShouldConvertCorrectlyToRGB(RGBA rgb, HSLA hsl)
        {
            hsl.ToRGBA().Should().Be(rgb);
        }

        [Property(Skip = "Rounding errors")]
        internal void ConversionShouldBeIsomorphic(RGBA rgb)
        {
            rgb.ToHSLA().ToRGBA().Should().Be(rgb);
        }
    }
}
