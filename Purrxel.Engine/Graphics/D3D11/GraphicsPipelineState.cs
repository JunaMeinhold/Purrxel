namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using HexaGen.Runtime.COM;
    using HexaGen.Runtime;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public struct GraphicsPipelineStateDescEx
    {
        public GraphicsPipelineDesc Pipeline;
        public GraphicsPipelineStateDesc State;
    }

    public unsafe class GraphicsPipelineState : D3D11PipelineState
    {
        private readonly ComPtr<ID3D11Device5> device = D3D11DeviceManager.Device;
        private readonly GraphicsPipeline pipeline;
        private readonly D3D11ResourceBindingList resourceBindingList;
        private readonly string dbgName;

        private ComPtr<ID3D11VertexShader> vs;
        private ComPtr<ID3D11HullShader> hs;
        private ComPtr<ID3D11DomainShader> ds;
        private ComPtr<ID3D11GeometryShader> gs;
        private ComPtr<ID3D11PixelShader> ps;

        private ComPtr<ID3D11InputLayout> layout;
        private ComPtr<ID3D11RasterizerState2> RasterizerState;
        private ComPtr<ID3D11DepthStencilState> DepthStencilState;
        private ComPtr<ID3D11BlendState1> BlendState;

        private GraphicsPipelineStateDesc desc = GraphicsPipelineStateDesc.Default;
        private bool isValid = false;

        private PrimitiveTopology primitiveTopology;

        public static GraphicsPipelineState Create(GraphicsPipelineDesc desc, GraphicsPipelineStateDesc stateDesc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            string dbgName = $"{file}, {line}";
            GraphicsPipeline pipeline = new(desc, dbgName);
            GraphicsPipelineState pipelineState = new(pipeline, stateDesc, dbgName);
            pipeline.Dispose();
            return pipelineState;
        }

        public static GraphicsPipelineState Create(GraphicsPipelineStateDescEx desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return Create(desc.Pipeline, desc.State, file, line);
        }

        public GraphicsPipelineState(GraphicsPipeline pipeline, GraphicsPipelineStateDesc desc, string dbgName = "")
        {
            pipeline.AddRef();
            this.desc = desc;
            this.pipeline = pipeline;
            this.dbgName = dbgName;

            {
                pipeline.OnCompile += OnPipelineCompile;
                pipeline.OnCreateLayout += CreateLayout;
                vs = pipeline.vs;
                hs = pipeline.hs;
                ds = pipeline.ds;
                gs = pipeline.gs;
                ps = pipeline.ps;
            }

            if (pipeline.signature == null)
            {
                //LoggerFactory.GetLogger(nameof(D3D11)).Error("Failed to create input layout, signature was null.");
            }
            else
            {
                CreateLayout(pipeline, pipeline.inputElements, pipeline.signature);
                isValid = true;
            }

            ComPtr<ID3D11RasterizerState2> rasterizerState;
            var rsDesc = desc.Rasterizer;
            device.CreateRasterizerState2(&rsDesc, &rasterizerState.Handle).ThrowIf();
            RasterizerState = rasterizerState;

            /*  if (!result.IsSuccess)
              {
                  Logger.Error($"Failed to create ID3D11RasterizerState2, {result.GetMessage()}");
                  isValid = false;
              }
            */

            ComPtr<ID3D11DepthStencilState> depthStencilState;
            var dsDesc = desc.DepthStencil;
            device.CreateDepthStencilState(&dsDesc, &depthStencilState.Handle).ThrowIf();
            DepthStencilState = depthStencilState;

            /*if (!result.IsSuccess)
            {
                Logger.Error($"Failed to create ID3D11DepthStencilState, {result.GetMessage()}");
                isValid = false;
            }*/

            ComPtr<ID3D11BlendState1> blendState;
            var bsDesc = desc.Blend;
            device.CreateBlendState1(&bsDesc, &blendState.Handle).ThrowIf();
            BlendState = blendState;

            /*  if (!result.IsSuccess)
              {
                  Logger.Error($"Failed to create ID3D11BlendState1, {result.GetMessage()}");
                  isValid = false;
              }*/

            resourceBindingList = new(pipeline);
            primitiveTopology = desc.Topology;
        }

        private static bool CanSkipLayout(InputElementDescription[]? inputElements)
        {
            ArgumentNullException.ThrowIfNull(inputElements, nameof(inputElements));

            for (int i = 0; i < inputElements.Length; i++)
            {
                var inputElement = inputElements[i];
                if (inputElement.SemanticName is not "SV_VertexID" and not "SV_InstanceID")
                {
                    return false;
                }
            }

            return true;
        }

        private void CreateLayout(GraphicsPipeline pipe, InputElementDescription[]? defaultInputElements, Blob signature)
        {
            isValid = false;
            if (layout.Handle != null)
            {
                layout.Release();
                layout = default;
            }

            var inputElements = desc.InputElements;
            inputElements ??= defaultInputElements;

            if (inputElements == null)
            {
                //LoggerFactory.GetLogger(nameof(D3D11)).Error("Failed to create input layout, InputElements was null or Reflection failed.");
                return;
            }

            if (!CanSkipLayout(inputElements))
            {
                ComPtr<ID3D11InputLayout> il;
                InputElementDesc* descs = AllocT<InputElementDesc>(inputElements.Length);
                Convert(inputElements, descs);
                HResult result = device.CreateInputLayout(descs, (uint)inputElements.Length, (void*)signature.BufferPointer, signature.PointerSize, &il.Handle);
                FreeInput(descs, inputElements.Length);
                Free(descs);

                if (!result.IsSuccess)
                {
                    result.ThrowIf();
                    isValid = false;
                    return;
                }

                layout = il;

                Utils.SetDebugName(layout.Handle, $"{dbgName}.{nameof(layout)}");
            }
            else
            {
                layout = default;
            }

            isValid = true;
        }

        private void OnPipelineCompile(IPipeline pipe)
        {
            GraphicsPipeline pipeline = (GraphicsPipeline)pipe;
            vs = pipeline.vs;
            hs = pipeline.hs;
            ds = pipeline.ds;
            gs = pipeline.gs;
            ps = pipeline.ps;
        }

        public GraphicsPipeline Pipeline => pipeline;

        public GraphicsPipelineStateDesc Description => desc;

        public PrimitiveTopology Topology
        {
            get => desc.Topology;
            set
            {
                desc.Topology = value;
                primitiveTopology = value;
            }
        }

        public Vector4 BlendFactor
        {
            get => desc.BlendFactor;
            set => desc.BlendFactor = value;
        }

        public uint StencilRef
        {
            get => desc.StencilRef;
            set => desc.StencilRef = value;
        }

        public bool IsValid => pipeline.IsValid && isValid;

        public bool IsInitialized => pipeline.IsInitialized;

        public D3D11ResourceBindingList Bindings => resourceBindingList;

        public string DebugName => dbgName;

        public void Begin(ComPtr<ID3D11DeviceContext> context)
        {
            SetState(context.As<ID3D11DeviceContext3>());
        }

        public void End(ComPtr<ID3D11DeviceContext> context)
        {
            UnsetState(context.As<ID3D11DeviceContext3>());
        }

        internal override void SetState(ComPtr<ID3D11DeviceContext3> context)
        {
            context.VSSetShader(vs, (ID3D11ClassInstance**)null, 0);
            context.HSSetShader(hs, (ID3D11ClassInstance**)null, 0);
            context.DSSetShader(ds, (ID3D11ClassInstance**)null, 0);
            context.GSSetShader(gs, (ID3D11ClassInstance**)null, 0);
            context.PSSetShader(ps, (ID3D11ClassInstance**)null, 0);

            context.RSSetState(RasterizerState.As<ID3D11RasterizerState>());

            var factor = desc.BlendFactor;
            float* fac = (float*)&factor;

            context.OMSetBlendState(BlendState.As<ID3D11BlendState>(), fac, uint.MaxValue);
            context.OMSetDepthStencilState(DepthStencilState, desc.StencilRef);
            context.IASetInputLayout(layout);
            context.IASetPrimitiveTopology(primitiveTopology);

            resourceBindingList?.BindGraphics(context);
        }

        internal override void UnsetState(ComPtr<ID3D11DeviceContext3> context)
        {
            context.VSSetShader((ID3D11VertexShader*)null, (ID3D11ClassInstance**)null, 0);
            context.HSSetShader((ID3D11HullShader*)null, (ID3D11ClassInstance**)null, 0);
            context.DSSetShader((ID3D11DomainShader*)null, (ID3D11ClassInstance**)null, 0);
            context.GSSetShader((ID3D11GeometryShader*)null, (ID3D11ClassInstance**)null, 0);
            context.PSSetShader((ID3D11PixelShader*)null, (ID3D11ClassInstance**)null, 0);

            context.RSSetState((ID3D11RasterizerState*)null);
            context.OMSetBlendState((ID3D11BlendState*)null, (float*)null, uint.MaxValue);
            context.OMSetDepthStencilState((ID3D11DepthStencilState*)null, 0);
            context.IASetInputLayout((ID3D11InputLayout*)null);
            context.IASetPrimitiveTopology(0);

            resourceBindingList?.UnbindGraphics(context);
        }

        #region Utility

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Convert(InputElementDescription[] inputElements, InputElementDesc* descs)
        {
            for (int i = 0; i < inputElements.Length; i++)
            {
                descs[i] = Convert(inputElements[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FreeInput(InputElementDesc* descs, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Marshal.FreeHGlobal((nint)descs[i].SemanticName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InputElementDesc Convert(InputElementDescription description)
        {
            return new()
            {
                AlignedByteOffset = (uint)description.AlignedByteOffset,
                Format = description.Format,
                InputSlot = (uint)description.Slot,
                InputSlotClass = description.Classification,
                InstanceDataStepRate = (uint)description.InstanceDataStepRate,
                SemanticIndex = (uint)description.SemanticIndex,
                SemanticName = description.SemanticName.ToUTF8Ptr()
            };
        }

        #endregion Utility

        protected override void DisposeCore()
        {
            pipeline.OnCompile -= OnPipelineCompile;
            pipeline.Dispose();

            resourceBindingList?.Dispose();

            if (layout.Handle != null)
            {
                layout.Release();
                layout = default;
            }

            if (RasterizerState.Handle != null)
            {
                RasterizerState.Release();
                RasterizerState = default;
            }

            if (DepthStencilState.Handle != null)
            {
                DepthStencilState.Release();
                DepthStencilState = default;
            }

            if (BlendState.Handle != null)
            {
                BlendState.Release();
                BlendState = default;
            }
        }
    }
}