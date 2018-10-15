module ImageProcessing

open System
open System.Threading.Tasks
open SkiaSharp

/// Creates a random 'light' color.
let rndColor =
    let rnd = Random()
    let next () = byte (rnd.Next(127, 256))
    fun () -> SKColor(next (), next (), next (), 255uy)

/// Calculate distance between two points.
let distance (mmx: int, mmy: int) (ox: int, oy: int) =
    Math.Sqrt(float ((mmx-ox)*(mmx-ox)+(mmy-oy)*(mmy-oy)))

/// Blend two colors with alpha together.
let blend (a: SKColor) (b: SKColor) =
    let aA, rA, gA, bA = int a.Alpha, int a.Red, int a.Green, int a.Blue
    let aB, rB, gB, bB = int b.Alpha, int b.Red, int b.Green, int b.Blue
    let rOut = (rA * aA / 255) + (rB * aB * (255 - aA) / (255 * 255))
    let gOut = (gA * aA / 255) + (gB * aB * (255 - aA) / (255 * 255))
    let bOut = (bA * aA / 255) + (bB * aB * (255 - aA) / (255 * 255))
    let aOut = aA + (aB * (255 - aA) / 255)
    SKColor(byte rOut, byte gOut, byte bOut, byte aOut)

let determineCircles
        getClosestCircle
        (overlay: SKBitmap): (((int * int) * byte) option) array =
    let inline getPixel x y =
        let grayColor = overlay.GetPixel(x, y)
        let grayscale =
           byte
            ((int grayColor.Red +
              int grayColor.Green +
              int grayColor.Blue) / 3)
        if grayscale > 0uy then
            let circle = getClosestCircle (x, y)
            Some (circle, grayscale)
        else
            None
    Array.Parallel.init
        (overlay.Width * overlay.Height)
        (fun i ->
            let x = i / overlay.Height
            let y = i % overlay.Height
            getPixel x y)

let recolorPixels
        getColorFromCircle
        (alpha: byte)
        (circles: (((int * int) * byte) option) array)
        (original: SKBitmap)
        (bitmap: SKBitmap) =
    let getNewColor color value =
        match value with
        | Some (circle, grayscale) ->
            let alpha = byte ((int grayscale * int alpha) / 255)
            let (ncolor: SKColor) = getColorFromCircle circle
            let ncolor = ncolor.WithAlpha(alpha)
            blend ncolor color
        | None ->
            color
    Parallel.For(0, original.Width * original.Height, (fun i ->
            let x = i / original.Height
            let y = i % original.Height
            let color = original.GetPixel(x, y)
            let ncolor = getNewColor color circles.[i]
            bitmap.SetPixel(x, y, ncolor)))
    |> ignore

/// Creates a grayscale color map.
let getColorMap (width, height) radius ccircles: SKBitmap =
    use whitePaint =
        new SKPaint(
            IsAntialias = true,
            Color = SKColors.White,
            FilterQuality = SKFilterQuality.High
        )
    let info = SKImageInfo(width, height)
    use surface = SKSurface.Create(info)
    use canvas = surface.Canvas
    canvas.Clear(SKColors.Black)
    for (x, y) in ccircles do
        canvas.DrawCircle(
            SKPoint(float32 x, float32 y),
            float32 radius,
            whitePaint
        )
    use image = surface.Snapshot()
    SKBitmap.FromImage(image)

let getCircleAlphaArray (width, height) radius circles =
    use overlay =
        getColorMap
            (width, height)
            radius
            circles

    let getClosestCircle point =
        circles
        |> Array.minBy (distance point)

    determineCircles
        getClosestCircle
        overlay
