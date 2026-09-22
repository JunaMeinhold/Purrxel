namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Diagnostics;
    using VoxelEngine.Core.Windows;

    public static unsafe class DXGIDeviceManager
    {
        private static ComPtr<IDXGIFactory7> factory;
        private static ComPtr<IDXGIAdapter4> adapter;

        public static void Initialize()
        {
            // Create the DXGIFactory1.
            DXGI.CreateDXGIFactory2(0, out factory);

            // Get the HardwareAdapter.
            adapter = GetHardwareAdapter();

            // Initialize d3d11 Device and DeviceContext.
            D3D11DeviceManager.InitializeDevice(adapter);
        }

        public static SwapChain CreateSwapChain(CoreWindow window)
        {
            var (Hwnd, _, _) = window.Win32 ?? throw new NotSupportedException();

            SwapChainDesc1 swapChainDescription = new()
            {
                Width = (uint)window.Width,
                Height = (uint)window.Height,
                Format = Format.B8G8R8A8Unorm,
                BufferCount = 3,
                BufferUsage = (uint)DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                SampleDesc = new SampleDesc(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = (uint)(SwapChainFlag.AllowModeSwitch | SwapChainFlag.AllowTearing),
                Stereo = false,
            };

            SwapChainFullscreenDesc fullscreenDescription = new()
            {
                Windowed = true,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            factory.CreateSwapChainForHwnd(D3D11DeviceManager.Device.As<IUnknown>(), Hwnd, ref swapChainDescription, ref fullscreenDescription, (IDXGIOutput*)null, out ComPtr<IDXGISwapChain1> swapChain);

            return new(D3D11DeviceManager.Device.As<ID3D11Device>(), swapChain, swapChainDescription);
        }

        public static void Dispose()
        {
            adapter.Dispose();
            factory.Dispose();
            D3D11DeviceManager.Dispose();
        }

        private static ComPtr<IDXGIAdapter4> GetHardwareAdapter()
        {
            ComPtr<IDXGIAdapter4> selected = null;

            for (uint adapterIndex = 0;
                factory.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out ComPtr<IDXGIAdapter4> adapter).Value !=
                (int)ResultCode.DXGI_ERROR_NOT_FOUND;
                adapterIndex++)
            {
                AdapterDesc1 desc;
                adapter.GetDesc1(&desc);

                Trace.WriteLine($"Found Adapter {new(&desc.Description_0)}");

                if (((AdapterFlag)desc.Flags & AdapterFlag.Software) != 0)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();
                    continue;
                }

                Trace.WriteLine($"Using {new(&desc.Description_0)}");

                selected = adapter;
            }

            if (selected.Handle == null)
                throw new NotSupportedException();
            return selected;
        }
    }
}