namespace VoxelEngine.Graphics.D3D11
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;

    public static class D3D11DeviceManager
    {
        internal static readonly FeatureLevel[] FeatureLevels =
        {
            FeatureLevel.Level111,
            FeatureLevel.Level110,
        };

        private static FeatureLevel _featureLevel;
        private static ComPtr<ID3D11Device5> iD3D11Device;
        private static ComPtr<ID3D11DeviceContext4> iD3D11DeviceContext;
        private static ComPtr<ID3D11Debug> debugDevice;
        private static GraphicsContext context;

        public static ComPtr<ID3D11Device5> Device => iD3D11Device;

        public static ComPtr<ID3D11DeviceContext4> Context => iD3D11DeviceContext;

        public static GraphicsContext GraphicsContext => context;

        public static FeatureLevel FeatureLevel => _featureLevel;

#if D3D_DEBUG
        public static ComPtr<ID3D11Debug> DebugDevice => debugDevice;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeDevice(ComPtr<IDXGIAdapter4> adapter)
        {
            CreateDeviceFlag flags = CreateDeviceFlag.BgraSupport;
#if D3D_DEBUG
            flags |= CreateDeviceFlag.Debug;
#endif

#if D3D11On12
            D3D11On12DeviceManager.InitializeDevice(adapter, flags, FeatureLevels, out iD3D11Device, out iD3D11DeviceContext, out _featureLevel);
#else
            ComPtr<ID3D11Device> tempDevice = default;
            ComPtr<ID3D11DeviceContext> tempContext;
            FeatureLevel level = default;
            D3D11.CreateDevice(adapter.As<IDXGIAdapter>(), DriverType.Unknown, 0, (uint)flags, ref FeatureLevels[0], (uint)FeatureLevels.Length, D3D11.D3D11_SDK_VERSION, ref tempDevice, ref level, out tempContext);
            tempDevice.QueryInterface(out iD3D11Device);
            tempContext.QueryInterface(out iD3D11DeviceContext);
            tempContext.Dispose();
            tempDevice.Dispose();

#endif

#if D3D_DEBUG
            Device.QueryInterface(out debugDevice);
#endif

            context = new(iD3D11DeviceContext.As<ID3D11DeviceContext3>());
        }

        public static void ResizeBegin()
        {
            // Delete all references to SwapChainBuffers.
            Context.ClearState();
            Context.Flush();
        }

        public static void ResizeEnd()
        {
        }

        public static void Initialize()
        {
        }

        public static void Dispose()
        {
            iD3D11DeviceContext.Dispose();
            iD3D11DeviceContext = null;

            iD3D11Device.Dispose();
            iD3D11Device = null;

#if D3D_DEBUG
            debugDevice.ReportLiveDeviceObjects(RldoFlags.Detail | RldoFlags.IgnoreInternal);
            debugDevice.Dispose();
            debugDevice = null;
#endif
        }

        public static void DisposeLate()
        {
        }
    }
}