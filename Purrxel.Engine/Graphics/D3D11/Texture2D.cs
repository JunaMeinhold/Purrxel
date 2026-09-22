namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.IO;

    public unsafe class Texture2D : DisposableRefBase, IShaderResourceView, IUnorderedAccessView, IRenderTargetView, IResource
    {
        private readonly string dbgName;
        private Texture2DDesc description;
        private GpuAccessFlags gpuAccessFlags;

        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11UnorderedAccessView> uav;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11RenderTargetView> rtv;

        private RenderTargetView[]? rtvSlices;
        private ShaderResourceView[]? srvSlices;
        private UnorderedAccessView[]? uavSlices;

        public Texture2D(string[] paths, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            texture = TextureHelper.LoadFromFiles(device, paths);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(string path, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);
            texture = TextureHelper.LoadTexture2DFile(device, Paths.CurrentTexturePath + path, description);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        public Texture2D(Texture2DDescription desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            gpuAccessFlags = desc.GpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)desc.Width, (uint)desc.Height, (uint)desc.MipLevels, (uint)desc.ArraySize, desc.Format, desc.SampleDesc, 0, 0, (uint)desc.CpuAccessFlags, (uint)desc.MiscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(desc.CpuAccessFlags, desc.GpuAccessFlags);

            device.CreateTexture2D(ref description, null, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, SampleDesc? sampleDesc = default, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)mipLevels, (uint)arraySize, format, sampleDesc ?? new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture2D(ref description, null, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(SubresourceData subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, SampleDesc? sampleDesc = default, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)mipLevels, (uint)arraySize, format, sampleDesc ?? new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture2D(ref description, &subresourceData, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(SubresourceData[] subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, SampleDesc? sampleDesc = default, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)mipLevels, (uint)arraySize, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture2D(ref description, ref subresourceData[0], out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        public ShaderResourceView SRV => srv;

        public RenderTargetView RTV => rtv;

        public UnorderedAccessView UAV => uav;

        nint IShaderResourceView.NativePointer => (nint)srv.Handle;

        nint IRenderTargetView.NativePointer => (nint)rtv.Handle;

        nint IUnorderedAccessView.NativePointer => (nint)uav.Handle;

        nint IDeviceChild.NativePointer => (nint)texture.Handle;

        public RenderTargetView[] RTVArraySlices => rtvSlices;

        public ShaderResourceView[] SRVArraySlices => srvSlices;

        public UnorderedAccessView[] UAVArraySlices => uavSlices;

        public Format Format => description.Format;

        public int Width => (int)description.Width;

        public int Height => (int)description.Height;

        public int ArraySize => (int)description.ArraySize;

        public Hexa.NET.Mathematics.Viewport Viewport => new(description.Width, description.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ComPtr<ID3D11DeviceContext> context, int slot, ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    context.VSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Hull:
                    context.HSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Domain:
                    context.DSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Pixel:
                    context.PSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;
            }
        }

        public void Resize(int width, int height)
        {
            Resize(description.Format, width, height, (int)description.ArraySize, (int)description.MipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(int width, int height, int arraySize, int mipLevels)
        {
            Resize(description.Format, width, height, arraySize, mipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int height, int arraySize, int mipLevels)
        {
            Resize(format, width, height, arraySize, mipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int height, int arraySize, int mipLevels, CpuAccessFlags cpuAccessFlags, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = 0)
        {
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlags, (uint)miscFlag);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);
            CreateViews(device, description);

            DisposeCore();

            device.CreateTexture2D(ref description, null, out texture).ThrowIf();
            //texture.DebugName = dbgName;
            CreateViews(device, description);
        }

        private void CreateViews(ComPtr<ID3D11Device> device, Texture2DDesc description)
        {
            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                UnorderedAccessViewDesc desc = new(description.Format);
                if (description.ArraySize > 1)
                {
                    desc.ViewDimension = UavDimension.Texture2Darray;
                    desc.Union.Texture2DArray = new(0, 0, description.ArraySize);
                }
                else
                {
                    desc.ViewDimension = UavDimension.Texture2D;
                    desc.Union.Texture2D = new(0);
                }

                device.CreateUnorderedAccessView(texture.As<ID3D11Resource>(), ref desc, out uav).ThrowIf();
                //uav.DebugName = nameof(Texture2D) + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out srv).ThrowIf();
                //srv.DebugName = nameof(Texture2D) + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out rtv).ThrowIf();
                //rtv.DebugName = nameof(Texture2D) + ".RTV";
            }
        }

        public void CreateArraySlices()
        {
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();

            uint arraySize = description.ArraySize;
            if (arraySize == 1)
            {
                if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
                {
                    uavSlices = [uav];
                }
                if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
                {
                    srvSlices = [srv];
                }
                if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
                {
                    rtvSlices = [rtv];
                }
                return;
            }

            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                uavSlices = new UnorderedAccessView[arraySize];
                for (uint i = 0; i < arraySize; i++)
                {
                    UnorderedAccessViewDesc desc = new(description.Format, UavDimension.Texture2Darray);
                    desc.Union.Texture2DArray = new()
                    {
                        ArraySize = 1,
                        FirstArraySlice = i,
                    };

                    device.CreateUnorderedAccessView(texture.As<ID3D11Resource>(), ref desc, out var uav).ThrowIf();
                    uavSlices[i] = uav;
                }
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                srvSlices = new ShaderResourceView[arraySize];
                for (uint i = 0; i < arraySize; i++)
                {
                    ShaderResourceViewDesc desc = new(description.Format, SrvDimension.Texture2Darray);
                    desc.Union.Texture2DArray = new()
                    {
                        ArraySize = 1,
                        FirstArraySlice = i,
                        MipLevels = description.MipLevels,
                    };

                    device.CreateShaderResourceView(texture.As<ID3D11Resource>(), ref desc, out var srv).ThrowIf();
                    srvSlices[i] = srv;
                }
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                rtvSlices = new RenderTargetView[arraySize];
                for (uint i = 0; i < arraySize; i++)
                {
                    RenderTargetViewDesc desc = new(description.Format, RtvDimension.Texture2Darray);
                    desc.Union.Texture2DArray = new()
                    {
                        ArraySize = 1,
                        FirstArraySlice = i,
                    };

                    device.CreateRenderTargetView(texture.As<ID3D11Resource>(), ref desc, out var rtv).ThrowIf();
                    rtvSlices[i] = rtv;
                }
            }
        }

        private void DestroySlices()
        {
            if (description.ArraySize > 1)
            {
                if (srvSlices != null)
                {
                    foreach (var srv in srvSlices)
                    {
                        srv.Dispose();
                    }
                }
                if (rtvSlices != null)
                {
                    foreach (var rtv in rtvSlices)
                    {
                        rtv.Dispose();
                    }
                }
                if (uavSlices != null)
                {
                    foreach (var uav in uavSlices)
                    {
                        uav.Dispose();
                    }
                }
            }
            srvSlices = null; rtvSlices = null; uavSlices = null;
        }

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(Texture2D texture) => texture.SRV;

        public static implicit operator ComPtr<ID3D11RenderTargetView>(Texture2D texture) => texture.RTV;

        public static implicit operator ComPtr<ID3D11Texture2D>(Texture2D texture) => texture.texture;

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
            DestroySlices();
        }
    }
}