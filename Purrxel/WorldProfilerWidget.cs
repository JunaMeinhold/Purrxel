namespace App
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImPlot;
    using System.Collections.Generic;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Unsafes;
    using VoxelEngine.Voxel;

    public unsafe class WorldProfilerWidget
    {
        public void Draw()
        {
            if (!ImGui.Begin("Profiler"))
            {
                ImGui.End();
                return;
            }

            DrawContent();

            ImGui.End();
        }

        private UnsafeRingBuffer<float> frames = new(512) { AverageValues = false };
        private Dictionary<string, UnsafeRingBuffer2<float>> stages = [];

        public void DrawContent()
        {
            const int shade_mode = 2;
            const float fill_ref = 0;
            double fill = shade_mode == 0 ? -double.PositiveInfinity : shade_mode == 1 ? double.PositiveInfinity : fill_ref;

            if (!ImGui.BeginTabBar("tab"u8))
            {
                return;
            }

            if (ImGui.BeginTabItem("Framerate"u8))
            {
                if (Time.Delta > 0)
                {
                    frames.Add(Time.Delta * 1000);
                }
                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("Frames"))
                {
                    ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
                    ImPlot.PlotShaded("Frames", ref frames.Values[0], frames.Length, fill, 1, 0, ImPlotShadedFlags.None, frames.Head);
                    ImPlot.PopStyleVar();

                    ImPlot.PlotLine("Frames", ref frames.Values[0], frames.Length, 1, 0, ImPlotLineFlags.None, frames.Head);
                    ImPlot.EndPlot();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Loader Profiler"u8))
            {
                var profiler = WorldLoader.Profiler;
                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("WorldLoader"))
                {
                    foreach (var stage in profiler.Names)
                    {
                        if (!stages.TryGetValue(stage, out var buffer))
                        {
                            buffer = new(1024);
                            stages[stage] = buffer;
                        }
                        float value = Math.Max(profiler[stage], 0);

                        buffer.Add(value * 1000);

                        ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
                        ImPlot.PlotShaded(stage, ref buffer.Raw[0], buffer.Length, fill, 1, 0, ImPlotShadedFlags.None, buffer.Head);
                        ImPlot.PopStyleVar();

                        ImPlot.PlotLine(stage, ref buffer.Raw[0], buffer.Length, 1, 0, ImPlotLineFlags.None, buffer.Head);
                    }
                    ImPlot.EndPlot();
                }

                profiler.Clear();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}