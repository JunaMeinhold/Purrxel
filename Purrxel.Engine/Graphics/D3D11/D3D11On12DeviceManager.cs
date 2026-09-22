namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3D11On12;
    using Hexa.NET.D3D12;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;

    public unsafe class D3D11On12DeviceManager
    {
        private static ComPtr<ID3D12Device> iD3D12Device;
        private static ComPtr<ID3D12CommandQueue> iD3D12CommandQueue;

        public static ComPtr<ID3D12Device> ID3D12Device => iD3D12Device;

        public static ComPtr<ID3D12CommandQueue> ID3D12CommandQueue => iD3D12CommandQueue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeDevice(ComPtr<IDXGIAdapter4> adapter, CreateDeviceFlag flags, FeatureLevel[] featureLevels, out ComPtr<ID3D11Device5> device, out ComPtr<ID3D11DeviceContext4> context, out FeatureLevel featureLevel)
        {
            D3D12.CreateDevice(adapter.As<IUnknown>(), FeatureLevel.Level111, out iD3D12Device);
            CommandQueueDesc commandQueueDesc = new(CommandListType.Direct);
            iD3D12Device.CreateCommandQueue(ref commandQueueDesc, out iD3D12CommandQueue);
            var queue = iD3D12CommandQueue.Handle;

            ComPtr<ID3D11Device> tempDevice = default;
            ComPtr<ID3D11DeviceContext> tempContext = default;
            featureLevel = default;
            D3D11On12.CreateDevice(iD3D12Device.As<IUnknown>(), (uint)flags, ref featureLevels[0], (uint)featureLevels.Length, iD3D12CommandQueue, 1, 0, ref tempDevice, ref tempContext, ref featureLevel);
            tempDevice.QueryInterface(out device);
            tempContext.QueryInterface(out context);
            tempContext.Dispose();
            tempDevice.Dispose();
        }

        public static void Dispose()
        {
            ID3D12Device.Dispose();
            ID3D12CommandQueue.Dispose();
        }
    }
}