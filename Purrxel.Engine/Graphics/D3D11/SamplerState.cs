namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;

    public unsafe class SamplerState : DisposableRefBase, ISamplerState
    {
        private ComPtr<ID3D11SamplerState> sampler;

        public SamplerState(SamplerDesc desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var device = D3D11DeviceManager.Device;
            device.CreateSamplerState(&desc, out sampler);
            Utils.SetDebugName(sampler, $"{file}, {line}");
        }

        public SamplerState(SamplerDesc desc, string dbgName)
        {
            var device = D3D11DeviceManager.Device;
            device.CreateSamplerState(&desc, out sampler);
            Utils.SetDebugName(sampler, dbgName);
        }

        public ComPtr<ID3D11SamplerState> Sampler => sampler;

        public nint NativePointer => (nint)sampler.Handle;

        public static implicit operator ComPtr<ID3D11SamplerState>(SamplerState sampler) => sampler.sampler;

        protected override void DisposeCore()
        {
            if (sampler.Handle != null)
            {
                sampler.Release();
                sampler = default;
            }
        }
    }
}