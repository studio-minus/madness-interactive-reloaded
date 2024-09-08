namespace MIR;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

/// <summary>
/// System for running the benchmark test.
/// </summary>
[Obsolete]
public class BenchmarkSystem : Walgelijk.System
{
    /// <summary>
    /// Is the benchmark gathering data?
    /// </summary>
    public bool IsRecording = false;

    /// <summary>
    /// The result of the benchmark.
    /// </summary>
    public BenchmarkResult BenchmarkResult = new();

    //public static readonly TimeSpan BenchmarkDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan BenchmarkDuration = TimeSpan.FromSeconds(120);
    private float time = 0;
    private float graphYAtMouse = 0;

    /// <summary>
    /// Reset the benchmark recording process.
    /// </summary>
    public void ResetRecording()
    {
        BenchmarkResult.UpdateTimeListMs.Clear();
        time = 0;
        IsRecording = false;
    }

    public override void Update()
    {
        throw new NotImplementedException("This whole thing is obsolete");

        if (IsRecording)
        {
            BenchmarkResult.UpdateTimeListMs.Add(Time.DeltaTimeUnscaled);
            time += Time.DeltaTimeUnscaled;
            if (time > BenchmarkDuration.TotalSeconds)
            {
                IsRecording = false;
                BenchmarkResult.WriteToFile();
                Audio.PlayOnce(Sounds.UiConfirm);
                Logger.Log("Benchmark ended.");

                AiCharacterSystem.AutoSpawn = false;

                foreach (var item in Scene.GetAllComponentsOfType<CharacterComponent>())
                    if (Scene.HasTag(item.Entity, Tags.EnemyAI))
                        item.Kill();
            }
        }

        //if (Scene.FeindAnyComponent<CameraComponent>(out var camera, out var entity) && Scene.TryGetComponentFrom<TransformComponent>(entity, out var transform)
        //    tra


        foreach (var item in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            //item.Faction = Faction.EnemyToAll;
            //item.EnemyCollisionLayer = CollisionLayers.Enemies;
            //item.CollisionLayer = CollisionLayers.None;
        }
    }

    public override void Render()
    {
        var windowSize = Scene.Game.Window.Size;

        Draw.Reset();
        Draw.Order = RenderOrders.UserInterface.WithOrder(10);
        Draw.ScreenSpace = true;
        Draw.FontSize = 48;
        Draw.Font = Fonts.Impact;

        if (IsRecording)
            Draw.Text($"{((int)MathF.Floor(time))} / {(int)BenchmarkDuration.TotalSeconds}", windowSize / 2, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, windowSize.X);
        else
        {
            Draw.Colour = Colors.Black.WithAlpha(0.8f);
            Draw.Quad(Vector2.Zero, windowSize);
            Draw.Colour = Colors.White;
            Draw.Text("BENCHMARK RESULTS", new Vector2(windowSize.X / 2, 48), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, windowSize.X);
#if DEBUG
            if (Time.SecondsSinceLoadUnscaled % 1 > 0.5)
            {
                Draw.FontSize = 24;
                Draw.Colour = Colors.Red;
                Draw.Text("GAME IS IN DEBUG MODE. THESE RESULTS ARE INACCURATE.", new Vector2(windowSize.X / 2, 90), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, windowSize.X);
                Draw.Colour = Colors.White;
            }
#endif

            Draw.Font = Fonts.Inter;

            Draw.FontSize = 24;
            Draw.Text("Update times", new Vector2(windowSize.X / 2, 250), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, windowSize.X);

            Draw.FontSize = 18;
            benchmarkResult($"<color=#ffffff5f>Min. </color>{MathF.Round(1 / BenchmarkResult.MaxUpdateTime, 2)} ups", 282);
            benchmarkResult($"<color=#ffffff5f>Max. </color>{MathF.Round(1 / BenchmarkResult.MinUpdateTime, 2)} ups", 305);
            benchmarkResult($"<color=#ffffff5f>Mean. </color>{MathF.Round(1 / BenchmarkResult.MeanUpdateTime, 2)} ups", 328);

            //time for graph

            var graphRect = new Rect(25, 355, windowSize.X - 25, windowSize.Y - 25);
            float minDt;
            float maxDt;

            minDt = BenchmarkResult.MinUpdateTime;
            maxDt = BenchmarkResult.MaxUpdateTime;

            Draw.Colour = Colors.Black.WithAlpha(0.95f);
            Draw.Quad(graphRect.BottomLeft, graphRect.GetSize());

            int step = Math.Max(8, BenchmarkResult.UpdateTimeListMs.Count / 512);

            Draw.Colour = Colors.Cyan.WithAlpha(0.2f);
            plotGraph(BenchmarkResult.UpdateTimeListMs, step);

            var sixtyFpsLine = Utilities.MapRange(minDt, maxDt, graphRect.MaxY, graphRect.MinY, 1 / 60f);
            Draw.Colour = Colors.Gray.WithAlpha(0.5f);
            Draw.Line(new Vector2(graphRect.MinX, sixtyFpsLine), new Vector2(graphRect.MaxX, sixtyFpsLine), 2);
            Draw.FontSize = 13;
            Draw.Text("Everything under this line exceeds 60 fps", new Vector2(graphRect.MinX + 16, sixtyFpsLine - 5), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);

            if (graphRect.ContainsPoint(Input.WindowMousePosition))
            {
                Draw.Colour = Colors.Red.WithAlpha(0.5f);
                Draw.Line(new Vector2(Input.WindowMousePosition.X, graphRect.MinY), new Vector2(Input.WindowMousePosition.X, graphRect.MaxY), 1);

                var updateTimeIndex = getIndexForGraphX(Input.WindowMousePosition.X, BenchmarkResult.UpdateTimeListMs.Count);
                var updateTime = BenchmarkResult.UpdateTimeListMs.Take(new Range(updateTimeIndex, updateTimeIndex + step)).Max();
                float minY = getGraphYFor(updateTime) - 32;

                graphYAtMouse = Utilities.SmoothApproach(graphYAtMouse, minY, 15, Time.DeltaTime);
                //graphYAtMouse = Utilities.Lerp(graphYAtMouse, minY, Utilities.LerpDt(0.95f, Time.DeltaTime * 4));

                var timeAtMouse = Utilities.MapRange(graphRect.MinX, graphRect.MaxX, 0, (float)BenchmarkDuration.TotalSeconds, Input.WindowMousePosition.X);
                var alignment = Input.WindowMousePosition.X < windowSize.X / 2 ? HorizontalTextAlign.Left : HorizontalTextAlign.Right;

                Draw.Colour = Colors.White;
                Draw.Text(MathF.Round(timeAtMouse, 2) + "s", new Vector2(Input.WindowMousePosition.X, graphRect.MaxY), Vector2.One, alignment, VerticalTextAlign.Bottom);

                Draw.Colour = Colors.Cyan;
                Draw.Text((updateTime * 1000) + "ms update time", new Vector2(Input.WindowMousePosition.X, graphYAtMouse + 16), Vector2.One, alignment, VerticalTextAlign.Middle);
            }

            Draw.Reset();

            float getGraphYFor(float deltaTime) => Utilities.MapRange(minDt, maxDt, graphRect.MaxY, graphRect.MinY, deltaTime);
            float getGraphXFor(int index, int maxIndex) => Utilities.MapRange(0, maxIndex, graphRect.MinX, graphRect.MaxX, index);

            int getIndexForGraphX(float x, int listCount) => (int)Utilities.Clamp(Utilities.MapRange(graphRect.MinX, graphRect.MaxX, 0, listCount - 1, x), 0, listCount);

            void plotGraph(IList<float> collection, int step)
            {
                for (int i = step; i < collection.Count - step; i += step)
                {
                    float currentX = getGraphXFor(i, BenchmarkResult.UpdateTimeListMs.Count - 1);
                    float nextX = getGraphXFor(i + step, BenchmarkResult.UpdateTimeListMs.Count - 1);
                    var current = getGraphYFor(collection.Take(new Range(i - step, i)).Max());
                    var next = getGraphYFor(collection.Take(new Range(i, i + step)).Max());
                    Draw.Line(new Vector2(currentX, current), new Vector2(nextX, next), 2);
                }
            }

            void benchmarkResult(string s, float y) => Draw.Text(s, new Vector2(windowSize.X / 2 - 128, y), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Middle, windowSize.X);
        }
    }
}
