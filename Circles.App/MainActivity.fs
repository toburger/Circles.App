namespace Circles

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Graphics

open SkiaSharp
open SkiaSharp.Views.Android

open ImageProcessing
open System.Threading

type Resources = Circles.Resource

/// Used to read input from JSON.
type Input = { id: string; x: string; y: string }

[<Activity (Label = "Circles", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity () =
    inherit Activity ()

    let scale = 8
    let radius = 80
    let alpha = 127uy

    let readJson (stream: IO.Stream) =
        let sr = new IO.StreamReader(stream)
        sr.ReadToEnd()
        |> Newtonsoft.Json.JsonConvert.DeserializeObject<Input array>

    override this.OnCreate (bundle) =
        base.OnCreate (bundle)

        this.SetContentView (Resources.Layout.Main)

        let canvasView = this.FindViewById<SKCanvasView>(Resources.Id.canvasView)

        let original =
            SKBitmap.Decode(
                this.Assets.Open("burgstall.jpg")
            )

        let circles =
            this.Assets.Open("circles.json")
            |> readJson
            |> Array.map (fun i ->
                let x = int i.x * scale
                let y = int i.y * scale
                (x, y))

        let mutable circleAlphaArray = [||]

        Async.Start <| async {
            circleAlphaArray <-
                getCircleAlphaArray
                    (original.Width, original.Height)
                    radius
                    circles
            canvasView.Invalidate()
        }

        let newCircleMap () =
            circles
            |> Array.map (fun xy ->
                xy, rndColor ())
            |> dict

        let mutable ccircleMap =
            newCircleMap ()

        let newColors () =
            ccircleMap <- newCircleMap ()

        let getColorFromCircle circle =
            ccircleMap.[circle]

        canvasView.PaintSurface.Add <| fun e ->
            let surface = e.Surface
            let canvas = surface.Canvas

            match circleAlphaArray with
            | [||] ->
                canvas.DrawBitmap(
                    original,
                    0.f,
                    0.f)

            | circleAlphaArray ->
                use bitmap =
                    new SKBitmap(
                        original.Width,
                        original.Height
                    )

                recolorPixels
                    getColorFromCircle
                    alpha
                    circleAlphaArray
                    original
                    bitmap

                canvas.DrawBitmap(
                    bitmap,
                    0.f,
                    0.f
                )

        canvasView.Click.Add <| fun e ->
            newColors ()
            canvasView.Invalidate()
