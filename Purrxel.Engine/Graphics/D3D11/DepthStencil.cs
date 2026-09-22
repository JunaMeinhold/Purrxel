namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Resources;
    using Format = Hexa.NET.DXGI.Format;

    public unsafe class DepthStencil : Resource, IDepthStencilView, IShaderResourceView
    {
        private readonly string dbgName;
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11DepthStencilView> depthStencilView;
        private ComPtr<ID3D11ShaderResourceView> shaderResourceView;

        private Format format;
        private int width;
        private int height;
        private int arraySize;

        public Hexa.NET.Mathematics.Viewport Viewport => new(width, height);

        public DepthStencil(DepthStencilBufferDescription desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : this(desc.Format, desc.Width, desc.Height, desc.ArraySize, file, line)
        {
        }

        public DepthStencil(int width, int height, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : this(Format.D32Float, width, height, 1, file, line)
        {
        }

        public DepthStencil(int width, int height, int arraySize, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : this(Format.D32Float, width, height, arraySize, file, line)
        {
        }

        public DepthStencil(Format format, int width, int height, int arraySize, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            var device = D3D11DeviceManager.Device;
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;
            Texture2DDesc depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDesc = new(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.DepthStencil | BindFlag.ShaderResource),
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref depthBufferDesc, null, out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");

            CreateViews(device, format, arraySize);
        }

        private void CreateViews(ComPtr<ID3D11Device5> device, Format format, int arraySize)
        {
            DepthStencilViewDesc dsvdesc = new(format);
            ShaderResourceViewDesc srvdesc = new(GetDepthSRVFormat(format));
            if (arraySize > 1)
            {
                dsvdesc.ViewDimension = DsvDimension.Texture2Darray;
                dsvdesc.Union.Texture2DArray.ArraySize = (uint)arraySize;
                dsvdesc.Union.Texture2DArray = new()
                {
                    ArraySize = (uint)arraySize,
                    FirstArraySlice = 0,
                };
                srvdesc.ViewDimension = SrvDimension.Texture2Darray;
                srvdesc.Union.Texture2DArray = new()
                {
                    ArraySize = (uint)arraySize,
                    MipLevels = 1,
                    FirstArraySlice = 0,
                    MostDetailedMip = 0
                };
            }
            else
            {
                dsvdesc.ViewDimension = DsvDimension.Texture2D;
                dsvdesc.Union.Texture2D = new()
                {
                    MipSlice = 0,
                };
                srvdesc.ViewDimension = SrvDimension.Texture2D;
                srvdesc.Union.Texture2D = new()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                };
            }

            device.CreateDepthStencilView(texture.As<ID3D11Resource>(), ref dsvdesc, out depthStencilView).ThrowIf();
            Utils.SetDebugName(depthStencilView, $"{dbgName}.{nameof(DSV)}");

            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), ref srvdesc, out shaderResourceView).ThrowIf();
            Utils.SetDebugName(shaderResourceView, $"{dbgName}.{nameof(SRV)}");
        }

        public DepthStencilView DSV => depthStencilView;

        public ShaderResourceView SRV => shaderResourceView;

        nint IDepthStencilView.NativePointer => (nint)depthStencilView.Handle;

        nint IShaderResourceView.NativePointer => (nint)shaderResourceView.Handle;

        public nint NativePointer => (nint)texture.Handle;

        private static Format GetDepthResourceFormat(Format depthFormat)
        {
            Format resformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16Unorm:
                    resformat = Format.R32Typeless;
                    break;

                case Format.D24UnormS8Uint:
                    resformat = Format.R24G8Typeless;
                    break;

                case Format.D32Float:
                    resformat = Format.R32Typeless;
                    break;

                case Format.D32FloatS8X24Uint:
                    resformat = Format.R32G8X24Typeless;
                    break;
            }

            return resformat;
        }

        private static Format GetDepthSRVFormat(Format depthFormat)
        {
            Format srvformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16Unorm:
                    srvformat = Format.R16Float;
                    break;

                case Format.D24UnormS8Uint:
                    srvformat = Format.R24UnormX8Typeless;
                    break;

                case Format.D32Float:
                    srvformat = Format.R32Float;
                    break;

                case Format.D32FloatS8X24Uint:
                    srvformat = Format.R32FloatX8X24Typeless;
                    break;
            }
            return srvformat;
        }

        public void Resize(int width, int height)
        {
            Resize(width, height, arraySize, format);
        }

        public void Resize(int width, int height, int arraySize)
        {
            Resize(width, height, arraySize, format);
        }

        public void Resize(int width, int height, int arraySize, Format format)
        {
            var device = D3D11DeviceManager.Device;
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;

            texture.Dispose();
            depthStencilView.Dispose();
            shaderResourceView.Dispose();

            Texture2DDesc depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.DepthStencil | BindFlag.ShaderResource),
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref depthBufferDesc, null, out texture).ThrowIf();
            //texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            CreateViews(device, format, arraySize);
        }

        public void Clear(GraphicsContext context, ClearFlag clearFlags, float depth, byte stencil)
        {
            context.ClearDepthStencilView(this, clearFlags, depth, stencil);
        }

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(DepthStencil texture) => texture.shaderResourceView;

        public static implicit operator ComPtr<ID3D11DepthStencilView>(DepthStencil texture) => texture.depthStencilView;

        public static implicit operator ComPtr<ID3D11Texture2D>(DepthStencil texture) => texture.texture;

        protected override void DisposeCore()
        {
            DSV.Dispose();
            SRV.Dispose();
            texture.Dispose();
        }
    }
}