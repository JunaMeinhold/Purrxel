namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.IO;

    public unsafe class Texture3D : DisposableRefBase, IShaderResourceView, IUnorderedAccessView, IRenderTargetView
    {
        private readonly string dbgName;
        private Texture3DDesc description;
        private GpuAccessFlags gpuAccessFlags;

        private ComPtr<ID3D11Texture3D> texture;
        private ComPtr<ID3D11UnorderedAccessView> uav;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11RenderTargetView> rtv;
        private ComPtr<ID3D11SamplerState> sampler;

        public Texture3D()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(string path, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);
            texture = TextureHelper.LoadTexture3DFile(device, Paths.CurrentTexturePath + path, description);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(Format format, int width, int height, int depth = 1, int mipLevels = 0, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)depth, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture3D(ref description, null, out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");

            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(Texture3DDescription desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            gpuAccessFlags = desc.GpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)desc.Width, (uint)desc.Height, (uint)desc.Depth, (uint)desc.MipLevels, desc.Format, 0, 0, (uint)desc.CpuAccessFlags, (uint)desc.MiscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(desc.CpuAccessFlags, desc.GpuAccessFlags);

            device.CreateTexture3D(ref description, null, out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");

            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(SubresourceData subresourceData, Format format, int width, int height, int depth = 1, int mipLevels = 0, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)depth, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture3D(ref description, &subresourceData, out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");

            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(SubresourceData[] subresourceData, Format format, int width, int height, int depth = 1, int mipLevels = 0, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)depth, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture3D(ref description, ref subresourceData[0], out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");

            CreateViews(device, description);
        }

        public ComPtr<ID3D11ShaderResourceView> SRV => srv;

        public ComPtr<ID3D11RenderTargetView> RTV => rtv;

        public ComPtr<ID3D11UnorderedAccessView> UAV => uav;

        public ComPtr<ID3D11SamplerState> Sampler
        {
            get => sampler;
            set
            {
                if (sampler.Handle != null)
                {
                    sampler.Release();
                }
                sampler = value;
                if (value.Handle != null)
                {
                    value.AddRef();
                }
            }
        }

        nint IShaderResourceView.NativePointer => (nint)srv.Handle;

        nint IRenderTargetView.NativePointer => (nint)rtv.Handle;

        nint IUnorderedAccessView.NativePointer => (nint)uav.Handle;

        public nint NativePointer => (nint)texture.Handle;

        public void Resize(Format format, int width, int height, int depth, int mipLevels, CpuAccessFlags cpuAccessFlags, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = 0)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)depth, (uint)mipLevels, format, Usage.Default, 0, (uint)cpuAccessFlags, (uint)miscFlag);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            DisposeCore();

            device.CreateTexture3D(ref description, null, out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture3D)}");

            CreateViews(device, description);
        }

        private void CreateViews(ComPtr<ID3D11Device> device, Texture3DDesc description)
        {
            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                device.CreateUnorderedAccessView(texture.As<ID3D11Resource>(), null, out uav).ThrowIf();
                //uav.DebugName = nameof(Texture3D) + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out srv).ThrowIf();
                //srv.DebugName = nameof(Texture3D) + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out rtv).ThrowIf();
                //rtv.DebugName = nameof(Texture3D) + ".RTV";
            }
        }

        protected override void DisposeCore()
        {
            if (texture.Handle != null)
            {
                texture.Dispose();
                texture = default;
            }
            if (srv.Handle != null)
            {
                srv.Dispose();
                srv = default;
            }
            if (rtv.Handle != null)
            {
                rtv.Dispose();
                rtv = default;
            }
            if (uav.Handle != null)
            {
                uav.Dispose();
                uav = default;
            }
            if (sampler.Handle != null)
            {
                sampler.Release();
                sampler = null;
            }
        }
    }
}