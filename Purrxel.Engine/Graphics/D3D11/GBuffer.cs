namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Resources;

    public unsafe class GBuffer : Resource
    {
        private readonly string dbgName;
        private ComPtr<ID3D11Texture2D>[] textures;
        private ShaderResourceView[] srvs;
        private RenderTargetView[] rtvs;
        private Format[] formats;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Count { get; private set; }

        public Format[] Formats => formats;

        public Hexa.NET.Mathematics.Viewport Viewport { get; private set; }

        public GBuffer(GBufferDescription description, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : this(description.Width, description.Height, description.Formats, file, line)
        {
        }

        public GBuffer(int width, int height, Format[] formats, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            var device = D3D11DeviceManager.Device;
            Viewport = new(width, height);
            Count = formats.Length;
            Width = width;
            Height = height;
            textures = new ComPtr<ID3D11Texture2D>[formats.Length];
            srvs = new ShaderResourceView[formats.Length];
            rtvs = new RenderTargetView[formats.Length];
            this.formats = formats;
            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDesc textureDesc = new()
                {
                    Width = (uint)Width,
                    Height = (uint)Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                    BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                device.CreateTexture2D(ref textureDesc, null, out ComPtr<ID3D11Texture2D> texture).ThrowIf();
                Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}.{i}");
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11ShaderResourceView> srv).ThrowIf();
                Utils.SetDebugName(srv, $"{dbgName}.{nameof(ShaderResourceView)}.{i}");
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11RenderTargetView> rtv).ThrowIf();
                Utils.SetDebugName(rtv, $"{dbgName}.{nameof(RenderTargetView)}.{i}");

                textures[i] = texture;
                srvs[i] = srv;
                rtvs[i] = rtv;
            }
        }

        public ShaderResourceView[] SRVs => srvs;

        public RenderTargetView[] RTVs => rtvs;

        public void Resize(int width, int height, params Format[] formats)
        {
            var device = D3D11DeviceManager.Device;
            if (formats == null || formats.Length == 0)
            {
                formats = this.formats;
            }

            Viewport = new(width, height);
            Width = width;
            Height = height;
            this.formats = formats;
            Count = formats.Length;

            for (int i = 0; i < textures.Length; i++)
            {
                srvs[i].Release();
                rtvs[i].Release();
                textures[i].Release();
            }

            textures = new ComPtr<ID3D11Texture2D>[formats.Length];
            srvs = new ShaderResourceView[formats.Length];
            rtvs = new RenderTargetView[formats.Length];

            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDesc textureDesc = new()
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                    BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                device.CreateTexture2D(ref textureDesc, null, out ComPtr<ID3D11Texture2D> texture).ThrowIf();
                Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}.{i}");
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11ShaderResourceView> srv).ThrowIf();
                Utils.SetDebugName(srv, $"{dbgName}.{nameof(ShaderResourceView)}.{i}");
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11RenderTargetView> rtv).ThrowIf();
                Utils.SetDebugName(rtv, $"{dbgName}.{nameof(RenderTargetView)}.{i}");

                textures[i] = texture;
                srvs[i] = srv;
                rtvs[i] = rtv;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(GraphicsContext context, IDepthStencilView? depthStencilView = null)
        {
            context.SetRenderTargets(rtvs.AsSpan(), depthStencilView);
        }

        public void Clear(GraphicsContext context, Vector4 color)
        {
            for (int i = 0; i < rtvs.Length; i++)
            {
                context.ClearRenderTargetView(rtvs[i], color);
            }
        }

        public static implicit operator ShaderResourceView[](GBuffer array)
        {
            return array.srvs;
        }

        public static implicit operator RenderTargetView[](GBuffer array)
        {
            return array.rtvs;
        }

        protected override void DisposeCore()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                srvs[i].Release();
                rtvs[i].Release();
                textures[i].Release();
            }

            textures = null;
            srvs = null;
            formats = null;
        }
    }
}