namespace VoxelEngine.Scenes
{
    using System.Diagnostics;
    using VoxelEngine.Core;

    public class SceneProfiler
    {
        private readonly Stopwatch simualtionStopwatch;
        private readonly Stopwatch updateStopwatch;
        private readonly Stopwatch renderStopwatch;
        private readonly Stopwatch dispatchStopwatch;

        public double SimulationTicks;
        public double UpdateTicks;
        public double RenderTicks;
        public double DispatchTicks;

        public double FrameTicks => SimulationTicks + UpdateTicks + RenderTicks + DispatchTicks;

        public SceneProfiler()
        {
            simualtionStopwatch = new();
            updateStopwatch = new();
            renderStopwatch = new();
            dispatchStopwatch = new();
            SimulationTicks = 0;
            UpdateTicks = 0;
            RenderTicks = 0;
            DispatchTicks = 0;
        }

        public void Reset()
        {
            simualtionStopwatch.Start();
        }

        public void ProfileSimulation()
        {
            simualtionStopwatch.Stop();
            SimulationTicks = simualtionStopwatch.Elapsed.TotalMilliseconds;
            simualtionStopwatch.Reset();
            updateStopwatch.Start();
        }

        public void ProfileUpdate()
        {
            updateStopwatch.Stop();
            UpdateTicks = updateStopwatch.Elapsed.TotalMilliseconds;
            updateStopwatch.Reset();
            renderStopwatch.Start();
        }

        public void ProfileRender()
        {
            renderStopwatch.Stop();
            RenderTicks = renderStopwatch.Elapsed.TotalMilliseconds;
            renderStopwatch.Reset();
            dispatchStopwatch.Start();
        }

        public void ProfileDispatch()
        {
            dispatchStopwatch.Stop();
            DispatchTicks = dispatchStopwatch.Elapsed.TotalMilliseconds;
            dispatchStopwatch.Reset();
        }

        public override string ToString()
        {
            return $"FPS: {1 / Time.Delta} / {Time.Delta:N4}s\nSimulation: {SimulationTicks:N2}ms\nUpdate: {UpdateTicks:N2}ms\nRender: {RenderTicks:N2}ms\nDispatch: {DispatchTicks:N2}ms\nFrame: {FrameTicks:N2}ms";
        }
    }
}