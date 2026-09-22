namespace VoxelEngine.UI
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.D3DCompiler;
    using Hexa.NET.DebugDraw;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using VoxelEngine.Graphics.D3D11;

    public unsafe class DebugDrawD3D11Renderer : IDisposable
    {
        private static DebugDrawContext debugDrawContext;

        private static ComPtr<ID3D11Device> device;
        private static ComPtr<ID3D11DeviceContext> context;
        private static ComPtr<ID3D11VertexShader> vertexShader;
        private static ComPtr<ID3D11PixelShader> pixelShader;
        private static ComPtr<ID3D11DepthStencilState> depthStencilState;
        private static ComPtr<ID3D11BlendState> blendState;
        private static ComPtr<ID3D11RasterizerState> rasterizerState;
        private static ComPtr<ID3D11InputLayout> inputLayout;
        private static ComPtr<ID3D11Buffer> vertexBuffer;
        private static ComPtr<ID3D11Buffer> indexBuffer;
        private static ComPtr<ID3D11Buffer> constantBuffer;
        private static ComPtr<ID3D11SamplerState> fontSampler;
        private static ComPtr<ID3D11Texture2D> fontTexture;
        private static ComPtr<ID3D11ShaderResourceView> fontView;

        private int vertexBufferSize = 5000;
        private int indexBufferSize = 10000;

        public DebugDrawD3D11Renderer(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context)
        {
            DebugDrawD3D11Renderer.device = device;
            DebugDrawD3D11Renderer.context = context;

            debugDrawContext = DebugDraw.CreateContext();

            string vertexShaderCode =
                 @"
struct VS_INPUT
{
	float3 position : POSITION;
    float2 tex : TEXCOORD0;
	float4 color : COLOR0;
};
struct PS_INPUT
{
	float4 position : SV_POSITION;
	float4 color : COLOR0;
    float2 tex : TEXCOORD0;
};
cbuffer MVPBuffer
{
    float4x4 ProjectionMatrix;
};
PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    output.position = mul(float4(input.position, 1), ProjectionMatrix);
    output.tex = input.tex;
	output.color = input.color;
	return output;
}";

            byte* pVertexShaderCode = vertexShaderCode.ToUTF8Ptr();
            int lengthVs = StrLen(pVertexShaderCode);
            ComPtr<ID3D10Blob> errorBlob = default;
            ComPtr<ID3D10Blob> vertexShaderBlob = default;

            D3DCompiler.Compile(pVertexShaderCode, (nuint)lengthVs, "vs", (Hexa.NET.D3DCommon.ShaderMacro*)null, (ID3DInclude*)null, "main", "vs_4_0", 0, 0, vertexShaderBlob.GetAddressOf(), errorBlob.GetAddressOf());
            Free(pVertexShaderCode);

            if (errorBlob.Handle != null)
            {
                var errorString = ToStringFromUTF8((byte*)errorBlob.GetBufferPointer());
                Debug.WriteLine(errorString);
                errorBlob.Release();
                errorBlob = default;
            }

            if (vertexShaderBlob.Handle == null)
            {
                throw new Exception("error compiling vertex shader");
            }

            ComPtr<ID3D11VertexShader> vtxShader = default;
            device.CreateVertexShader(vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize(), (ID3D11ClassLinkage*)null, vtxShader.GetAddressOf());
            vertexShader = vtxShader;

            InputElementDesc* inputElements = stackalloc InputElementDesc[]
            {
                new InputElementDesc( "POSITION".ToUTF8Ptr(), 0, Format.R32G32B32Float,0,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDesc( "TEXCOORD".ToUTF8Ptr(), 0, Format.R32G32Float,   12, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDesc( "COLOR".ToUTF8Ptr(),    0, Format.R8G8B8A8Unorm, 20, 0, InputClassification.PerVertexData, 0 ),
            };

            device.CreateInputLayout(inputElements, 3, vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize(), inputLayout.GetAddressOf());

            for (int i = 0; i < 3; i++)
            {
                Free(inputElements[i].SemanticName);
            }

            vertexShaderBlob.Release();

            BufferDesc constBufferDesc = new()
            {
                ByteWidth = (uint)sizeof(Matrix4x4),
                Usage = Usage.Dynamic,
                BindFlags = (uint)BindFlag.ConstantBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
            };
            device.CreateBuffer(ref constBufferDesc, null, out constantBuffer);

            string pixelShaderCode =
                @"struct PS_INPUT
{
    float4 position : SV_POSITION;
	float4 color : COLOR0;
    float2 tex : TEXCOORD0;
};

float4 main(PS_INPUT pixel) : SV_TARGET
{
	return pixel.color;
}";
            ComPtr<ID3D10Blob> pixelShaderBlob = default;

            byte* pPixelShaderCode = pixelShaderCode.ToUTF8Ptr();
            int lengthPx = StrLen(pPixelShaderCode);
            D3DCompiler.Compile(pPixelShaderCode, (nuint)lengthPx, "ps", (Hexa.NET.D3DCommon.ShaderMacro*)null, (ID3DInclude*)null, "main", "ps_4_0", 0, 0, pixelShaderBlob.GetAddressOf(), errorBlob.GetAddressOf());
            Free(pPixelShaderCode);

            if (errorBlob.Handle != null)
            {
                var errorString = ToStringFromUTF8((byte*)errorBlob.GetBufferPointer());
                Debug.WriteLine(errorString);
                errorBlob.Release();
                errorBlob = default;
            }

            if (pixelShaderBlob.Handle == null)
            {
                throw new Exception("error compiling pixel shader");
            }

            ComPtr<ID3D11PixelShader> pxShader = default;
            device.CreatePixelShader(pixelShaderBlob.GetBufferPointer(), pixelShaderBlob.GetBufferSize(), (ID3D11ClassLinkage*)null, pxShader.GetAddressOf());
            pixelShader = pxShader;

            pixelShaderBlob.Release();

            BlendDesc blendDesc = new()
            {
                AlphaToCoverageEnable = false
            };

            blendDesc.RenderTarget[0] = new RenderTargetBlendDesc
            {
                BlendOpAlpha = BlendOp.Add,
                BlendEnable = true,
                BlendOp = BlendOp.Add,
                DestBlendAlpha = Blend.InvSrcAlpha,
                DestBlend = Blend.InvSrcAlpha,
                SrcBlend = Blend.SrcAlpha,
                SrcBlendAlpha = Blend.SrcAlpha,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };

            device.CreateBlendState(ref blendDesc, out blendState);

            RasterizerDesc rasterDesc = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                ScissorEnable = false,
                DepthClipEnable = true,
                AntialiasedLineEnable = true,
                MultisampleEnable = false
            };

            device.CreateRasterizerState(ref rasterDesc, out rasterizerState);

            DepthStencilopDesc stencilOpDesc = new(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep, ComparisonFunc.Always);
            DepthStencilDesc depthDesc = new()
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunc.LessEqual,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc
            };

            device.CreateDepthStencilState(ref depthDesc, out depthStencilState);

            SamplerDesc samplerDesc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0f,
                ComparisonFunc = ComparisonFunc.Always,
                MinLOD = 0f,
                MaxLOD = 0f
            };

            device.CreateSamplerState(&samplerDesc, out fontSampler);

            CreateFontsTexture(device);
        }

        public static void CreateFontsTexture(ComPtr<ID3D11Device> device)
        {
            int width = 1;
            int height = 1;

            uint* pixels = AllocT<uint>(width * height);
            MemsetT(pixels, 0xffffffff, width * height);

            Texture2DDesc textureDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                ArraySize = 1,
                MipLevels = 1,
                Format = Format.B8G8R8A8Unorm,
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.ShaderResource,
                CPUAccessFlags = 0,
                MiscFlags = 0,
                SampleDesc = new(1, 0)
            };

            SubresourceData subresourceData = new(pixels, (uint)(width * sizeof(uint)));
            device.CreateTexture2D(&textureDesc, &subresourceData, out fontTexture);

            device.CreateShaderResourceView(fontTexture.As<ID3D11Resource>(), null, out fontView);

            debugDrawContext.FontTextureId = (nint)fontView.Handle;
        }

        public void BeginDraw()
        {
            DebugDraw.SetCurrentContext(debugDrawContext);
            DebugDraw.NewFrame();
        }

        public void EndDraw(ComPtr<ID3D11RenderTargetView> rtv, ComPtr<ID3D11DepthStencilView> dsv)
        {
            DebugDraw.Render();
            Render(DebugDraw.GetDrawData(), rtv, dsv);
        }

        private static unsafe void SetupRenderState(DebugDrawViewport drawData, ComPtr<ID3D11DeviceContext> ctx)
        {
            var viewport = drawData;

            uint stride = (uint)sizeof(DebugDrawVert);
            uint offset = 0;

            Viewport d3dViewport = new(viewport.X, viewport.Y, viewport.Width, viewport.Height, 0, 1);

            ctx.VSSetShader(vertexShader, (ID3D11ClassInstance*)null, 0);
            ctx.PSSetShader(pixelShader, (ID3D11ClassInstance*)null, 0);
            ctx.IASetInputLayout(inputLayout);
            ctx.RSSetState(rasterizerState);
            ctx.OMSetDepthStencilState(depthStencilState, 0);
            ctx.OMSetBlendState(blendState, null, 0);
            ctx.RSSetViewports(1, &d3dViewport);
            var vtxBuffer = vertexBuffer.Handle;
            ctx.IASetVertexBuffers(0, 1, &vtxBuffer, &stride, &offset);
            ctx.IASetIndexBuffer(indexBuffer, sizeof(uint) == 2 ? Format.R16Uint : Format.R32Uint, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.Trianglelist);
            var cb = constantBuffer.Handle;
            ctx.VSSetConstantBuffers(0, 1, &cb);
            var smp = fontSampler.Handle;
            ctx.PSSetSamplers(0, 1, &smp);
        }

        private void Render(DebugDrawData data, ComPtr<ID3D11RenderTargetView> rtv, ComPtr<ID3D11DepthStencilView> dsv)
        {
            if (data.TotalVertices > vertexBufferSize || vertexBuffer.Handle == null)
            {
                if (vertexBuffer.Handle != null)
                {
                    vertexBuffer.Release();
                }

                vertexBuffer.Release();
                var newVertexBufferSize = (int)(data.TotalVertices * 1.5f);
                vertexBufferSize = newVertexBufferSize == 0 ? vertexBufferSize : newVertexBufferSize;
                BufferDesc desc = new((uint)(vertexBufferSize * sizeof(DebugDrawVert)), Usage.Dynamic, (uint)BindFlag.VertexBuffer, (uint)CpuAccessFlag.Write);
                device.CreateBuffer(ref desc, null, out vertexBuffer);
            }

            if (data.TotalIndices > indexBufferSize || indexBuffer.Handle == null)
            {
                if (indexBuffer.Handle != null)
                {
                    indexBuffer.Release();
                }

                var newIndexBufferSize = (int)(data.TotalIndices * 1.5f);
                indexBufferSize = newIndexBufferSize == 0 ? indexBufferSize : newIndexBufferSize;
                BufferDesc desc = new((uint)(indexBufferSize * sizeof(uint)), Usage.Dynamic, (uint)BindFlag.IndexBuffer, (uint)CpuAccessFlag.Write);
                device.CreateBuffer(ref desc, null, out indexBuffer);
            }

            MappedSubresource vertexResource;
            MappedSubresource indexResource;
            context.Map(vertexBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &vertexResource);
            context.Map(indexBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &indexResource);
            var vertexResourcePointer = (DebugDrawVert*)vertexResource.PData;
            var indexResourcePointer = (uint*)indexResource.PData;

            for (int i = 0; i < data.CmdLists.Count; i++)
            {
                var cmdList = data.CmdLists[i];
                MemcpyT(cmdList.Vertices, vertexResourcePointer, cmdList.VertexCount);
                MemcpyT(cmdList.Indices, indexResourcePointer, cmdList.IndexCount);
                vertexResourcePointer += cmdList.VertexCount;
                indexResourcePointer += cmdList.IndexCount;
            }

            context.Unmap(vertexBuffer.As<ID3D11Resource>(), 0);
            context.Unmap(indexBuffer.As<ID3D11Resource>(), 0);

            // Setup desired state
            SetupRenderState(data.Viewport, context);

            int voffset = 0;
            uint ioffset = 0;
            context.OMSetRenderTargets(1, &rtv.Handle, (ID3D11DepthStencilView*)null);
            for (int i = 0; i < data.CmdLists.Count; i++)
            {
                var cmdList = data.CmdLists[i];
                for (int j = 0; j < cmdList.Commands.Count; j++)
                {
                    DebugDrawCommand cmd = cmdList.Commands[j];

                    MappedSubresource mappedResource;
                    context.Map(constantBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mappedResource);
                    Matrix4x4 mvp = Matrix4x4.Transpose(cmd.Transform * data.Camera);
                    Buffer.MemoryCopy(&mvp, mappedResource.PData, mappedResource.RowPitch, sizeof(Matrix4x4));
                    context.Unmap(constantBuffer.As<ID3D11Resource>(), 0);

                    var texId = cmd.TextureId;
                    ID3D11ShaderResourceView* tex = fontView;
                    if (texId != 0)
                    {
                        tex = (ID3D11ShaderResourceView*)texId;
                    }
                    context.PSSetShaderResources(0, 1, &tex);

                    context.IASetPrimitiveTopology(Convert(cmd.Topology));
                    context.DrawIndexedInstanced((uint)(int)cmd.IndexCount, 1, (uint)(int)ioffset, voffset, 0);
                    voffset += (int)cmd.VertexCount;
                    ioffset += cmd.IndexCount;
                }
            }

            context.VSSetShader(null, (ID3D11ClassInstance*)null, 0);
            context.PSSetShader(null, (ID3D11ClassInstance*)null, 0);
            context.IASetInputLayout(null);
            context.RSSetState((ID3D11RasterizerState*)null);
            context.OMSetDepthStencilState((ID3D11DepthStencilState*)null, 0);
            context.OMSetBlendState((ID3D11BlendState*)null, (float*)null, 0);
            context.RSSetViewport(default(Viewport));
            void* nullPtr = null;
            uint stride = 0, offset = 0;
            context.IASetVertexBuffers(0, 1, (ID3D11Buffer**)&nullPtr, &stride, &offset);
            context.IASetIndexBuffer((ID3D11Buffer*)null, default, 0);
            context.IASetPrimitiveTopology(PrimitiveTopology.Undefined);
            context.VSSetConstantBuffers(0, 1, (ID3D11Buffer**)&nullPtr);
            context.PSSetShaderResources(0, 1, (ID3D11ShaderResourceView**)&nullPtr);
            context.PSSetSamplers(0, 1, (ID3D11SamplerState**)&nullPtr);
        }

        private PrimitiveTopology Convert(DebugDrawPrimitiveTopology topology)
        {
            throw new NotImplementedException();
        }

        public void InvalidateFontTexture()
        {
            if (fontView.Handle != null)
            {
                fontView.Release();
                fontView = default;
            }

            if (fontTexture.Handle != null)
            {
                fontTexture.Release();
                fontTexture = default;
            }
        }

        protected virtual void DisposeCore()
        {
            InvalidateFontTexture();

            if (indexBuffer.Handle != null)
            {
                indexBuffer.Release();
                indexBuffer = null;
            }
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }
            if (blendState.Handle != null)
            {
                blendState.Release();
                blendState = null;
            }
            if (depthStencilState.Handle != null)
            {
                depthStencilState.Release();
                depthStencilState = null;
            }
            if (rasterizerState.Handle != null)
            {
                rasterizerState.Release();
                rasterizerState = null;
            }
            if (pixelShader.Handle != null)
            {
                pixelShader.Release();
                pixelShader = null;
            }
            if (constantBuffer.Handle != null)
            {
                constantBuffer.Release();
                constantBuffer = null;
            }
            if (inputLayout.Handle != null)
            {
                inputLayout.Release();
                inputLayout = null;
            }
            if (vertexShader.Handle != null)
            {
                vertexShader.Release();
                vertexShader = null;
            }

            debugDrawContext.Dispose();
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}