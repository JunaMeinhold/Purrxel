namespace VoxelEngine.Graphics.D3D11
{
    public abstract class DisposableRefBase : IDisposableRef
    {
        private bool disposedValue;
        private long counter = 1;

        public DisposableRefBase()
        {
            //LeakTracer.Allocate(this);
        }

        public bool IsDisposed => disposedValue;

        public event EventHandler? OnDisposed;

        public void AddRef()
        {
            Interlocked.Increment(ref counter);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Decrement(ref counter) != 0)
            {
                return;
            }

            if (!disposedValue)
            {
                DisposeCore();
                OnDisposed?.Invoke(this, EventArgs.Empty);
                disposedValue = true;
            }
        }

        protected abstract void DisposeCore();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}