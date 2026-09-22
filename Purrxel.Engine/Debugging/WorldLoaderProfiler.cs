namespace VoxelEngine.Debugging
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public class WorldLoaderProfiler
    {
        private readonly Dictionary<string, long> starts = [];
        private readonly Dictionary<string, long> deltas = [];
        private readonly HashSet<string> names = [];
        private readonly Lock _lock = new();

        public float this[string name]
        {
            get
            {
                if (deltas.TryGetValue(name, out var delta))
                {
                    return delta / (float)Stopwatch.Frequency;
                }
                return -1;
            }
        }

        public IReadOnlySet<string> Names => names;

        public void Clear()
        {
            lock (_lock)
            {
                starts.Clear();
                deltas.Clear();
            }
        }

        public void Begin(string name)
        {
            long now = Stopwatch.GetTimestamp();
            lock (_lock)
            {
                starts[name] = now;
            }
        }

        public void End(string name)
        {
            long now = Stopwatch.GetTimestamp();
            lock (_lock)
            {
                if (starts.TryGetValue(name, out var startTime))
                {
                    names.Add(name);
                    long delta = now - startTime;
                    deltas[name] = delta;
                }
            }
        }
    }
}