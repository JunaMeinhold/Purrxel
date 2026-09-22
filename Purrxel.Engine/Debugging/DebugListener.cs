namespace VoxelEngine.Debugging
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class DebugListener : TraceListener
    {
        private SemaphoreSlim semaphore = new(1);
        private readonly BufferedStream stream;

        public DebugListener(string file)
        {
            var fileInfo = new FileInfo(file);
            fileInfo.Directory?.Create();
            stream = new(File.Create(file));
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            stream.Flush();
            stream.Close();
        }

        private void Application_ApplicationClosing(object sender, EventArgs e)
        {
            stream.Flush();
            stream.Close();
        }

        public override void Write(string? message)
        {
            if (message == null)
            {
                return;
            }

            semaphore.Wait();
            stream.Write(Encoding.UTF8.GetBytes(message));
            semaphore.Release();
        }

        public override void WriteLine(string? message)
        {
            if (message == null)
            {
                return;
            }

            semaphore.Wait();
            stream.Write(Encoding.UTF8.GetBytes(message + "\n"));
            semaphore.Release();
        }

        public async Task WriteAsync(string? message)
        {
            if (message == null)
            {
                return;
            }

            await semaphore.WaitAsync();
            stream.Write(Encoding.UTF8.GetBytes(message));
            semaphore.Release();
        }

        public async Task WriteLineAsync(string? message)
        {
            if (message == null)
            {
                return;
            }

            await semaphore.WaitAsync();
            stream.Write(Encoding.UTF8.GetBytes(message + "\n"));
            semaphore.Release();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteLine(e.ExceptionObject);
            if (e.IsTerminating)
            {
                stream.Flush();
                stream.Close();
            }
        }
    }
}