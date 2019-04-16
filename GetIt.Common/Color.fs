namespace GetIt

open System

type RGBAColor =
    {
        /// The red part of the color.
        Red: byte
        /// The green part of the color.
        Green: byte
        /// The blue part of the color.
        Blue: byte
        /// The alpha part of the color that defines the opacity.
        Alpha: byte
    }
    override this.ToString() =
        sprintf "rgba(%d, %d, %d, %d)" this.Red this.Green this.Blue this.Alpha
    static member SelectRandom([<ParamArray>] colors) =
        let index = RandomNumberGenerator.``default``.Next(Array.length colors)
        Array.item index colors

module internal RGBAColor =
    let rgbHexNotation v =
        sprintf "#%02x%02x%02x" v.Red v.Green v.Blue
    let transparency v =
        float v.Alpha / float Byte.MaxValue

open System.Runtime.CompilerServices

[<Extension>]
type RGBAExtensions() =
    /// Creates a new color based on an existing one with a different alpha value.
    [<Extension>]
    static member WithAlpha(color: RGBAColor, alpha: byte) =
        { color with Alpha = alpha }

type private HSLA =
    {
        Hue: float
        Saturation: float
        Lightness: float
        Alpha: float
    }
    override this.ToString() =
        sprintf "hsla(%f°, %f, %f, %f)" (this.Hue * 360.) this.Saturation this.Lightness this.Alpha

// No idea what this does, got this from http://www.easyrgb.com/en/math.php
module private HSLA =
    let fromRGBA rgba =
        let r = float rgba.Red / 255.0
        let g = float rgba.Green / 255.0
        let b = float rgba.Blue / 255.0

        let min = Math.Min(r, Math.Min(g, b))
        let max = Math.Max(r, Math.Max(g, b))
        let delta = max - min

        let lightness = (max + min) / 2.

        let (hue, saturation) =
            if delta = 0. // Gray, no chroma
            then (0., 0.)
            else // Chromatic data...
                let saturation =
                    if lightness < 0.5 then delta / (max + min)
                    else delta / (2. - max - min)

                let delR = ( ( ( max - r ) / 6. ) + ( max / 2. ) ) / max
                let delG = ( ( ( max - g ) / 6. ) + ( max / 2. ) ) / max
                let delB = ( ( ( max - b ) / 6. ) + ( max / 2. ) ) / max

                let hue =
                    if       r = max then delB - delG
                    elif g = max then ( 1.0 / 3.0 ) + delR - delB
                    else (*if ( b == max )*) ( 2.0 / 3.0 ) + delG - delR

                let hue =
                    if hue < 0. then hue + 1.
                    elif hue > 1. then hue - 1.
                    else hue
                (hue, saturation)
        { Hue = hue; Saturation = saturation; Lightness = lightness; Alpha = float rgba.Alpha / float System.Byte.MaxValue }

    let toRGBA hsla =
        let alpha = Math.Round(hsla.Alpha * 255.) |> byte
        if hsla.Saturation = 0.
        then
            let value = Math.Round(hsla.Lightness * 255.) |> byte
            { Red = value
              Green = value
              Blue = value
              Alpha = alpha }
        else
            let var2 =
                if hsla.Lightness < 0.5 then hsla.Lightness * (1. + hsla.Saturation)
                else (hsla.Lightness + hsla.Saturation) - (hsla.Saturation * hsla.Lightness)

            let var1 = 2. * hsla.Lightness - var2

            let hueToRgb v1 v2 vH =
                let vH =
                    if vH < 0. then vH + 1.
                    elif vH > 1. then vH - 1.
                    else vH
                if ( 6. * vH ) < 1. then v1 + ( v2 - v1 ) * 6. * vH
                elif ( 2. * vH ) < 1. then v2
                elif ( 3. * vH ) < 2. then v1 + ( v2 - v1 ) * ( ( 2.0 / 3.0 ) - vH ) * 6.
                else v1
            { Red = Math.Round(255. * hueToRgb var1 var2 (hsla.Hue + ( 1.0 / 3.0 ) )) |> byte
              Green = Math.Round(255. * hueToRgb var1 var2 hsla.Hue) |> byte
              Blue = Math.Round(255. * hueToRgb var1 var2 (hsla.Hue - ( 1.0 / 3.0 ) )) |> byte
              Alpha = alpha }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Color =
    let hueShift angle color =
        let hslaColor = HSLA.fromRGBA color
        let shiftedValue = hslaColor.Hue + (Degrees.value angle / 360.)
        { hslaColor with Hue = shiftedValue }
        |> HSLA.toRGBA

module RGBAColors =
    [<CompiledName("AliceBlue")>]
    let aliceBlue = { Red = 0xf0uy; Green = 0xf8uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("AntiqueWhite")>]
    let antiqueWhite = { Red = 0xfauy; Green = 0xebuy; Blue = 0xd7uy; Alpha = 0xffuy }
    [<CompiledName("Aqua")>]
    let aqua = { Red = 0x00uy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Aquamarine")>]
    let aquamarine = { Red = 0x7fuy; Green = 0xffuy; Blue = 0xd4uy; Alpha = 0xffuy }
    [<CompiledName("Azure")>]
    let azure = { Red = 0xf0uy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Beige")>]
    let beige = { Red = 0xf5uy; Green = 0xf5uy; Blue = 0xdcuy; Alpha = 0xffuy }
    [<CompiledName("Bisque")>]
    let bisque = { Red = 0xffuy; Green = 0xe4uy; Blue = 0xc4uy; Alpha = 0xffuy }
    [<CompiledName("Black")>]
    let black = { Red = 0x00uy; Green = 0x00uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("BlanchedAlmond")>]
    let blanchedAlmond = { Red = 0xffuy; Green = 0xebuy; Blue = 0xcduy; Alpha = 0xffuy }
    [<CompiledName("Blue")>]
    let blue = { Red = 0x00uy; Green = 0x00uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("BlueViolet")>]
    let blueViolet = { Red = 0x8auy; Green = 0x2buy; Blue = 0xe2uy; Alpha = 0xffuy }
    [<CompiledName("Brown")>]
    let brown = { Red = 0xa5uy; Green = 0x2auy; Blue = 0x2auy; Alpha = 0xffuy }
    [<CompiledName("BurlyWood")>]
    let burlyWood = { Red = 0xdeuy; Green = 0xb8uy; Blue = 0x87uy; Alpha = 0xffuy }
    [<CompiledName("CadetBlue")>]
    let cadetBlue = { Red = 0x5fuy; Green = 0x9euy; Blue = 0xa0uy; Alpha = 0xffuy }
    [<CompiledName("Chartreuse")>]
    let chartreuse = { Red = 0x7fuy; Green = 0xffuy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("Chocolate")>]
    let chocolate = { Red = 0xd2uy; Green = 0x69uy; Blue = 0x1euy; Alpha = 0xffuy }
    [<CompiledName("Coral")>]
    let coral = { Red = 0xffuy; Green = 0x7fuy; Blue = 0x50uy; Alpha = 0xffuy }
    [<CompiledName("CornflowerBlue")>]
    let cornflowerBlue = { Red = 0x64uy; Green = 0x95uy; Blue = 0xeduy; Alpha = 0xffuy }
    [<CompiledName("Cornsilk")>]
    let cornsilk = { Red = 0xffuy; Green = 0xf8uy; Blue = 0xdcuy; Alpha = 0xffuy }
    [<CompiledName("Crimson")>]
    let crimson = { Red = 0xdcuy; Green = 0x14uy; Blue = 0x3cuy; Alpha = 0xffuy }
    [<CompiledName("Cyan")>]
    let cyan = { Red = 0x00uy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("DarkBlue")>]
    let darkBlue = { Red = 0x00uy; Green = 0x00uy; Blue = 0x8buy; Alpha = 0xffuy }
    [<CompiledName("DarkCyan")>]
    let darkCyan = { Red = 0x00uy; Green = 0x8buy; Blue = 0x8buy; Alpha = 0xffuy }
    [<CompiledName("DarkGoldenrod")>]
    let darkGoldenrod = { Red = 0xb8uy; Green = 0x86uy; Blue = 0x0buy; Alpha = 0xffuy }
    [<CompiledName("DarkGray")>]
    let darkGray = { Red = 0xa9uy; Green = 0xa9uy; Blue = 0xa9uy; Alpha = 0xffuy }
    [<CompiledName("DarkGreen")>]
    let darkGreen = { Red = 0x00uy; Green = 0x64uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("DarkKhaki")>]
    let darkKhaki = { Red = 0xbduy; Green = 0xb7uy; Blue = 0x6buy; Alpha = 0xffuy }
    [<CompiledName("DarkMagenta")>]
    let darkMagenta = { Red = 0x8buy; Green = 0x00uy; Blue = 0x8buy; Alpha = 0xffuy }
    [<CompiledName("DarkOliveGreen")>]
    let darkOliveGreen = { Red = 0x55uy; Green = 0x6buy; Blue = 0x2fuy; Alpha = 0xffuy }
    [<CompiledName("DarkOrange")>]
    let darkOrange = { Red = 0xffuy; Green = 0x8cuy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("DarkOrchid")>]
    let darkOrchid = { Red = 0x99uy; Green = 0x32uy; Blue = 0xccuy; Alpha = 0xffuy }
    [<CompiledName("DarkRed")>]
    let darkRed = { Red = 0x8buy; Green = 0x00uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("DarkSalmon")>]
    let darkSalmon = { Red = 0xe9uy; Green = 0x96uy; Blue = 0x7auy; Alpha = 0xffuy }
    [<CompiledName("DarkSeaGreen")>]
    let darkSeaGreen = { Red = 0x8fuy; Green = 0xbcuy; Blue = 0x8fuy; Alpha = 0xffuy }
    [<CompiledName("DarkSlateBlue")>]
    let darkSlateBlue = { Red = 0x48uy; Green = 0x3duy; Blue = 0x8buy; Alpha = 0xffuy }
    [<CompiledName("DarkSlateGray")>]
    let darkSlateGray = { Red = 0x2fuy; Green = 0x4fuy; Blue = 0x4fuy; Alpha = 0xffuy }
    [<CompiledName("DarkTurquoise")>]
    let darkTurquoise = { Red = 0x00uy; Green = 0xceuy; Blue = 0xd1uy; Alpha = 0xffuy }
    [<CompiledName("DarkViolet")>]
    let darkViolet = { Red = 0x94uy; Green = 0x00uy; Blue = 0xd3uy; Alpha = 0xffuy }
    [<CompiledName("DeepPink")>]
    let deepPink = { Red = 0xffuy; Green = 0x14uy; Blue = 0x93uy; Alpha = 0xffuy }
    [<CompiledName("DeepSkyBlue")>]
    let deepSkyBlue = { Red = 0x00uy; Green = 0xbfuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("DimGray")>]
    let dimGray = { Red = 0x69uy; Green = 0x69uy; Blue = 0x69uy; Alpha = 0xffuy }
    [<CompiledName("DodgerBlue")>]
    let dodgerBlue = { Red = 0x1euy; Green = 0x90uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Firebrick")>]
    let firebrick = { Red = 0xb2uy; Green = 0x22uy; Blue = 0x22uy; Alpha = 0xffuy }
    [<CompiledName("FloralWhite")>]
    let floralWhite = { Red = 0xffuy; Green = 0xfauy; Blue = 0xf0uy; Alpha = 0xffuy }
    [<CompiledName("ForestGreen")>]
    let forestGreen = { Red = 0x22uy; Green = 0x8buy; Blue = 0x22uy; Alpha = 0xffuy }
    [<CompiledName("Fuchsia")>]
    let fuchsia = { Red = 0xffuy; Green = 0x00uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Gainsboro")>]
    let gainsboro = { Red = 0xdcuy; Green = 0xdcuy; Blue = 0xdcuy; Alpha = 0xffuy }
    [<CompiledName("GhostWhite")>]
    let ghostWhite = { Red = 0xf8uy; Green = 0xf8uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Gold")>]
    let gold = { Red = 0xffuy; Green = 0xd7uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("Goldenrod")>]
    let goldenrod = { Red = 0xdauy; Green = 0xa5uy; Blue = 0x20uy; Alpha = 0xffuy }
    [<CompiledName("Gray")>]
    let gray = { Red = 0x80uy; Green = 0x80uy; Blue = 0x80uy; Alpha = 0xffuy }
    [<CompiledName("Green")>]
    let green = { Red = 0x00uy; Green = 0x80uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("GreenYellow")>]
    let greenYellow = { Red = 0xaduy; Green = 0xffuy; Blue = 0x2fuy; Alpha = 0xffuy }
    [<CompiledName("Honeydew")>]
    let honeydew = { Red = 0xf0uy; Green = 0xffuy; Blue = 0xf0uy; Alpha = 0xffuy }
    [<CompiledName("HotPink")>]
    let hotPink = { Red = 0xffuy; Green = 0x69uy; Blue = 0xb4uy; Alpha = 0xffuy }
    [<CompiledName("IndianRed")>]
    let indianRed = { Red = 0xcduy; Green = 0x5cuy; Blue = 0x5cuy; Alpha = 0xffuy }
    [<CompiledName("Indigo")>]
    let indigo = { Red = 0x4buy; Green = 0x00uy; Blue = 0x82uy; Alpha = 0xffuy }
    [<CompiledName("Ivory")>]
    let ivory = { Red = 0xffuy; Green = 0xffuy; Blue = 0xf0uy; Alpha = 0xffuy }
    [<CompiledName("Khaki")>]
    let khaki = { Red = 0xf0uy; Green = 0xe6uy; Blue = 0x8cuy; Alpha = 0xffuy }
    [<CompiledName("Lavender")>]
    let lavender = { Red = 0xe6uy; Green = 0xe6uy; Blue = 0xfauy; Alpha = 0xffuy }
    [<CompiledName("LavenderBlush")>]
    let lavenderBlush = { Red = 0xffuy; Green = 0xf0uy; Blue = 0xf5uy; Alpha = 0xffuy }
    [<CompiledName("LawnGreen")>]
    let lawnGreen = { Red = 0x7cuy; Green = 0xfcuy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("LemonChiffon")>]
    let lemonChiffon = { Red = 0xffuy; Green = 0xfauy; Blue = 0xcduy; Alpha = 0xffuy }
    [<CompiledName("LightBlue")>]
    let lightBlue = { Red = 0xaduy; Green = 0xd8uy; Blue = 0xe6uy; Alpha = 0xffuy }
    [<CompiledName("LightCoral")>]
    let lightCoral = { Red = 0xf0uy; Green = 0x80uy; Blue = 0x80uy; Alpha = 0xffuy }
    [<CompiledName("LightCyan")>]
    let lightCyan = { Red = 0xe0uy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("LightGoldenrodYellow")>]
    let lightGoldenrodYellow = { Red = 0xfauy; Green = 0xfauy; Blue = 0xd2uy; Alpha = 0xffuy }
    [<CompiledName("LightGreen")>]
    let lightGreen = { Red = 0x90uy; Green = 0xeeuy; Blue = 0x90uy; Alpha = 0xffuy }
    [<CompiledName("LightGray")>]
    let lightGray = { Red = 0xd3uy; Green = 0xd3uy; Blue = 0xd3uy; Alpha = 0xffuy }
    [<CompiledName("LightPink")>]
    let lightPink = { Red = 0xffuy; Green = 0xb6uy; Blue = 0xc1uy; Alpha = 0xffuy }
    [<CompiledName("LightSalmon")>]
    let lightSalmon = { Red = 0xffuy; Green = 0xa0uy; Blue = 0x7auy; Alpha = 0xffuy }
    [<CompiledName("LightSeaGreen")>]
    let lightSeaGreen = { Red = 0x20uy; Green = 0xb2uy; Blue = 0xaauy; Alpha = 0xffuy }
    [<CompiledName("LightSkyBlue")>]
    let lightSkyBlue = { Red = 0x87uy; Green = 0xceuy; Blue = 0xfauy; Alpha = 0xffuy }
    [<CompiledName("LightSlateGray")>]
    let lightSlateGray = { Red = 0x77uy; Green = 0x88uy; Blue = 0x99uy; Alpha = 0xffuy }
    [<CompiledName("LightSteelBlue")>]
    let lightSteelBlue = { Red = 0xb0uy; Green = 0xc4uy; Blue = 0xdeuy; Alpha = 0xffuy }
    [<CompiledName("LightYellow")>]
    let lightYellow = { Red = 0xffuy; Green = 0xffuy; Blue = 0xe0uy; Alpha = 0xffuy }
    [<CompiledName("Lime")>]
    let lime = { Red = 0x00uy; Green = 0xffuy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("LimeGreen")>]
    let limeGreen = { Red = 0x32uy; Green = 0xcduy; Blue = 0x32uy; Alpha = 0xffuy }
    [<CompiledName("Linen")>]
    let linen = { Red = 0xfauy; Green = 0xf0uy; Blue = 0xe6uy; Alpha = 0xffuy }
    [<CompiledName("Magenta")>]
    let magenta = { Red = 0xffuy; Green = 0x00uy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("Maroon")>]
    let maroon = { Red = 0x80uy; Green = 0x00uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("MediumAquamarine")>]
    let mediumAquamarine = { Red = 0x66uy; Green = 0xcduy; Blue = 0xaauy; Alpha = 0xffuy }
    [<CompiledName("MediumBlue")>]
    let mediumBlue = { Red = 0x00uy; Green = 0x00uy; Blue = 0xcduy; Alpha = 0xffuy }
    [<CompiledName("MediumOrchid")>]
    let mediumOrchid = { Red = 0xbauy; Green = 0x55uy; Blue = 0xd3uy; Alpha = 0xffuy }
    [<CompiledName("MediumPurple")>]
    let mediumPurple = { Red = 0x93uy; Green = 0x70uy; Blue = 0xdbuy; Alpha = 0xffuy }
    [<CompiledName("MediumSeaGreen")>]
    let mediumSeaGreen = { Red = 0x3cuy; Green = 0xb3uy; Blue = 0x71uy; Alpha = 0xffuy }
    [<CompiledName("MediumSlateBlue")>]
    let mediumSlateBlue = { Red = 0x7buy; Green = 0x68uy; Blue = 0xeeuy; Alpha = 0xffuy }
    [<CompiledName("MediumSpringGreen")>]
    let mediumSpringGreen = { Red = 0x00uy; Green = 0xfauy; Blue = 0x9auy; Alpha = 0xffuy }
    [<CompiledName("MediumTurquoise")>]
    let mediumTurquoise = { Red = 0x48uy; Green = 0xd1uy; Blue = 0xccuy; Alpha = 0xffuy }
    [<CompiledName("MediumVioletRed")>]
    let mediumVioletRed = { Red = 0xc7uy; Green = 0x15uy; Blue = 0x85uy; Alpha = 0xffuy }
    [<CompiledName("MidnightBlue")>]
    let midnightBlue = { Red = 0x19uy; Green = 0x19uy; Blue = 0x70uy; Alpha = 0xffuy }
    [<CompiledName("MintCream")>]
    let mintCream = { Red = 0xf5uy; Green = 0xffuy; Blue = 0xfauy; Alpha = 0xffuy }
    [<CompiledName("MistyRose")>]
    let mistyRose = { Red = 0xffuy; Green = 0xe4uy; Blue = 0xe1uy; Alpha = 0xffuy }
    [<CompiledName("Moccasin")>]
    let moccasin = { Red = 0xffuy; Green = 0xe4uy; Blue = 0xb5uy; Alpha = 0xffuy }
    [<CompiledName("NavajoWhite")>]
    let navajoWhite = { Red = 0xffuy; Green = 0xdeuy; Blue = 0xaduy; Alpha = 0xffuy }
    [<CompiledName("Navy")>]
    let navy = { Red = 0x00uy; Green = 0x00uy; Blue = 0x80uy; Alpha = 0xffuy }
    [<CompiledName("OldLace")>]
    let oldLace = { Red = 0xfduy; Green = 0xf5uy; Blue = 0xe6uy; Alpha = 0xffuy }
    [<CompiledName("Olive")>]
    let olive = { Red = 0x80uy; Green = 0x80uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("OliveDrab")>]
    let oliveDrab = { Red = 0x6buy; Green = 0x8euy; Blue = 0x23uy; Alpha = 0xffuy }
    [<CompiledName("Orange")>]
    let orange = { Red = 0xffuy; Green = 0xa5uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("OrangeRed")>]
    let orangeRed = { Red = 0xffuy; Green = 0x45uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("Orchid")>]
    let orchid = { Red = 0xdauy; Green = 0x70uy; Blue = 0xd6uy; Alpha = 0xffuy }
    [<CompiledName("PaleGoldenrod")>]
    let paleGoldenrod = { Red = 0xeeuy; Green = 0xe8uy; Blue = 0xaauy; Alpha = 0xffuy }
    [<CompiledName("PaleGreen")>]
    let paleGreen = { Red = 0x98uy; Green = 0xfbuy; Blue = 0x98uy; Alpha = 0xffuy }
    [<CompiledName("PaleTurquoise")>]
    let paleTurquoise = { Red = 0xafuy; Green = 0xeeuy; Blue = 0xeeuy; Alpha = 0xffuy }
    [<CompiledName("PaleVioletRed")>]
    let paleVioletRed = { Red = 0xdbuy; Green = 0x70uy; Blue = 0x93uy; Alpha = 0xffuy }
    [<CompiledName("PapayaWhip")>]
    let papayaWhip = { Red = 0xffuy; Green = 0xefuy; Blue = 0xd5uy; Alpha = 0xffuy }
    [<CompiledName("PeachPuff")>]
    let peachPuff = { Red = 0xffuy; Green = 0xdauy; Blue = 0xb9uy; Alpha = 0xffuy }
    [<CompiledName("Peru")>]
    let peru = { Red = 0xcduy; Green = 0x85uy; Blue = 0x3fuy; Alpha = 0xffuy }
    [<CompiledName("Pink")>]
    let pink = { Red = 0xffuy; Green = 0xc0uy; Blue = 0xcbuy; Alpha = 0xffuy }
    [<CompiledName("Plum")>]
    let plum = { Red = 0xdduy; Green = 0xa0uy; Blue = 0xdduy; Alpha = 0xffuy }
    [<CompiledName("PowderBlue")>]
    let powderBlue = { Red = 0xb0uy; Green = 0xe0uy; Blue = 0xe6uy; Alpha = 0xffuy }
    [<CompiledName("Purple")>]
    let purple = { Red = 0x80uy; Green = 0x00uy; Blue = 0x80uy; Alpha = 0xffuy }
    [<CompiledName("Red")>]
    let red = { Red = 0xffuy; Green = 0x00uy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("RosyBrown")>]
    let rosyBrown = { Red = 0xbcuy; Green = 0x8fuy; Blue = 0x8fuy; Alpha = 0xffuy }
    [<CompiledName("RoyalBlue")>]
    let royalBlue = { Red = 0x41uy; Green = 0x69uy; Blue = 0xe1uy; Alpha = 0xffuy }
    [<CompiledName("SaddleBrown")>]
    let saddleBrown = { Red = 0x8buy; Green = 0x45uy; Blue = 0x13uy; Alpha = 0xffuy }
    [<CompiledName("Salmon")>]
    let salmon = { Red = 0xfauy; Green = 0x80uy; Blue = 0x72uy; Alpha = 0xffuy }
    [<CompiledName("SandyBrown")>]
    let sandyBrown = { Red = 0xf4uy; Green = 0xa4uy; Blue = 0x60uy; Alpha = 0xffuy }
    [<CompiledName("SeaGreen")>]
    let seaGreen = { Red = 0x2euy; Green = 0x8buy; Blue = 0x57uy; Alpha = 0xffuy }
    [<CompiledName("SeaShell")>]
    let seaShell = { Red = 0xffuy; Green = 0xf5uy; Blue = 0xeeuy; Alpha = 0xffuy }
    [<CompiledName("Sienna")>]
    let sienna = { Red = 0xa0uy; Green = 0x52uy; Blue = 0x2duy; Alpha = 0xffuy }
    [<CompiledName("Silver")>]
    let silver = { Red = 0xc0uy; Green = 0xc0uy; Blue = 0xc0uy; Alpha = 0xffuy }
    [<CompiledName("SkyBlue")>]
    let skyBlue = { Red = 0x87uy; Green = 0xceuy; Blue = 0xebuy; Alpha = 0xffuy }
    [<CompiledName("SlateBlue")>]
    let slateBlue = { Red = 0x6auy; Green = 0x5auy; Blue = 0xcduy; Alpha = 0xffuy }
    [<CompiledName("SlateGray")>]
    let slateGray = { Red = 0x70uy; Green = 0x80uy; Blue = 0x90uy; Alpha = 0xffuy }
    [<CompiledName("Snow")>]
    let snow = { Red = 0xffuy; Green = 0xfauy; Blue = 0xfauy; Alpha = 0xffuy }
    [<CompiledName("SpringGreen")>]
    let springGreen = { Red = 0x00uy; Green = 0xffuy; Blue = 0x7fuy; Alpha = 0xffuy }
    [<CompiledName("SteelBlue")>]
    let steelBlue = { Red = 0x46uy; Green = 0x82uy; Blue = 0xb4uy; Alpha = 0xffuy }
    [<CompiledName("Tan")>]
    let tan = { Red = 0xd2uy; Green = 0xb4uy; Blue = 0x8cuy; Alpha = 0xffuy }
    [<CompiledName("Teal")>]
    let teal = { Red = 0x00uy; Green = 0x80uy; Blue = 0x80uy; Alpha = 0xffuy }
    [<CompiledName("Thistle")>]
    let thistle = { Red = 0xd8uy; Green = 0xbfuy; Blue = 0xd8uy; Alpha = 0xffuy }
    [<CompiledName("Tomato")>]
    let tomato = { Red = 0xffuy; Green = 0x63uy; Blue = 0x47uy; Alpha = 0xffuy }
    [<CompiledName("Transparent")>]
    let transparent = { Red = 0xffuy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0x00uy }
    [<CompiledName("Turquoise")>]
    let turquoise = { Red = 0x40uy; Green = 0xe0uy; Blue = 0xd0uy; Alpha = 0xffuy }
    [<CompiledName("Violet")>]
    let violet = { Red = 0xeeuy; Green = 0x82uy; Blue = 0xeeuy; Alpha = 0xffuy }
    [<CompiledName("Wheat")>]
    let wheat = { Red = 0xf5uy; Green = 0xdeuy; Blue = 0xb3uy; Alpha = 0xffuy }
    [<CompiledName("White")>]
    let white = { Red = 0xffuy; Green = 0xffuy; Blue = 0xffuy; Alpha = 0xffuy }
    [<CompiledName("WhiteSmoke")>]
    let whiteSmoke = { Red = 0xf5uy; Green = 0xf5uy; Blue = 0xf5uy; Alpha = 0xffuy }
    [<CompiledName("Yellow")>]
    let yellow = { Red = 0xffuy; Green = 0xffuy; Blue = 0x00uy; Alpha = 0xffuy }
    [<CompiledName("YellowGreen")>]
    let yellowGreen = { Red = 0x9auy; Green = 0xcduy; Blue = 0x32uy; Alpha = 0xffuy }