namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.IO;

    public unsafe class Texture1D : DisposableRefBase, IShaderResourceView, IUnorderedAccessView, IRenderTargetView, IResource
    {
        private readonly string dbgName;
        private Texture1DDesc description;
        private GpuAccessFlags gpuAccessFlags;

        private ComPtr<ID3D11Texture1D> texture;
        private ComPtr<ID3D11UnorderedAccessView> uav;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11RenderTargetView> rtv;

        private RenderTargetView[]? rtvSlices;
        private ShaderResourceView[]? srvSlices;
        private UnorderedAccessView[]? uavSlices;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture1D(string path, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);
            texture = TextureHelper.LoadTexture1DFile(device, Paths.CurrentTexturePath + path, description);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        public Texture1D(Texture1DDescription desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            gpuAccessFlags = desc.GpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)desc.Width, (uint)desc.MipLevels, (uint)desc.ArraySize, desc.Format, 0, 0, (uint)desc.CpuAccessFlags, (uint)desc.MiscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(desc.CpuAccessFlags, desc.GpuAccessFlags);

            device.CreateTexture1D(ref description, null, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture1D(Format format, int width, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)mipLevels, (uint)arraySize, format, 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture1D(ref description, null, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture1D(SubresourceData subresourceData, Format format, int width, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)mipLevels, (uint)arraySize, format, 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture1D(ref description, &subresourceData, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture1D(SubresourceData[] subresourceData, Format format, int width, int arraySize = 1, int mipLevels = 1, CpuAccessFlags cpuAccessFlags = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)mipLevels, (uint)arraySize, format, 0, 0, (uint)cpuAccessFlags, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);

            device.CreateTexture1D(ref description, ref subresourceData[0], out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        public ComPtr<ID3D11ShaderResourceView> SRV => srv;

        public ComPtr<ID3D11RenderTargetView> RTV => rtv;

        public ComPtr<ID3D11UnorderedAccessView> UAV => uav;

        nint IShaderResourceView.NativePointer => (nint)srv.Handle;

        nint IRenderTargetView.NativePointer => (nint)rtv.Handle;

        nint IUnorderedAccessView.NativePointer => (nint)uav.Handle;

        nint IDeviceChild.NativePointer => (nint)texture.Handle;

        public Format Format => description.Format;

        public int Width => (int)description.Width;

        public int ArraySize => (int)description.ArraySize;

        public Hexa.NET.Mathematics.Viewport Viewport => new(description.Width, 1);

        public void Resize(int width)
        {
            Resize(description.Format, width, (int)description.ArraySize, (int)description.MipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(int width, int arraySize, int mipLevels)
        {
            Resize(description.Format, width, arraySize, mipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int arraySize, int mipLevels)
        {
            Resize(format, width, arraySize, mipLevels, (CpuAccessFlags)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int arraySize, int mipLevels, CpuAccessFlags cpuAccessFlags, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = 0)
        {
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)arraySize, (uint)mipLevels, format, 0, 0, (uint)cpuAccessFlags, (uint)miscFlag);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlags, gpuAccessFlags);
            CreateViews(device, description);

            DisposeCore();

            device.CreateTexture1D(ref description, null, out texture).ThrowIf();
            //texture.DebugName = dbgName;
            CreateViews(device, description);
        }

        private void CreateViews(ComPtr<ID3D11Device> device, Texture1DDesc description)
        {
            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                UnorderedAccessViewDesc desc = new(description.Format, description.ArraySize > 1 ? UavDimension.Texture1Darray : UavDimension.Texture1D);
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
                    UnorderedAccessViewDesc desc = new(description.Format, UavDimension.Texture1Darray);
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
                    ShaderResourceViewDesc desc = new(description.Format, SrvDimension.Texture1Darray);
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
                    RenderTargetViewDesc desc = new(description.Format, RtvDimension.Texture1Darray);
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

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(Texture1D texture) => texture.SRV;

        public static implicit operator ComPtr<ID3D11RenderTargetView>(Texture1D texture) => texture.RTV;

        public static implicit operator ComPtr<ID3D11Texture1D>(Texture1D texture) => texture.texture;

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