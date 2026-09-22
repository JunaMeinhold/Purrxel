namespace VoxelEngine.Debugging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class MemoryLeakReporter
    {
        private readonly Dictionary<string, WeakReference> managedObjects = new();

        public object this[string key]
        {
            get
            {
                if (managedObjects.TryGetValue(key, out WeakReference wr))
                {
                    if (wr.IsAlive)
                    {
                        return wr.Target;
                    }

                    managedObjects.Remove(key);
                }
                return null;
            }
            set => managedObjects[key] = new WeakReference(value);
        }

        public void ReportLiveObjects()
        {
            foreach (KeyValuePair<string, WeakReference> wr in managedObjects)
            {
                if (wr.Value.IsAlive)
                {
                    Trace.WriteLine($"{wr.Key} {wr.Value.Target}");
                }
            }
        }

        public void Clear()
        {
            managedObjects.Clear();
        }
    }
}