namespace VoxelEngine.Resources
{
    using System;
    using System.Diagnostics;

    public abstract class Resource : IDisposable
    {
        public Resource()
        {
        }

        protected abstract void DisposeCore();

        public bool IsDisposed { get; private set; }

        public event EventHandler? Disposed;

        ~Resource()
        {
            Trace.Assert(!IsDisposed, "Not correctly disposed, can lead to memory leaks.");
            DisposeCore();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Disposed?.Invoke(this, EventArgs.Empty);
                DisposeCore();
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}