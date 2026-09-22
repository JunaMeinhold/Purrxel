namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;

    public unsafe class ComputePipelineState : D3D11PipelineState
    {
        private readonly ComputePipeline pipeline;
        private readonly D3D11ResourceBindingList resourceBindingList;
        private readonly string dbgName;

        internal ComPtr<ID3D11ComputeShader> cs;

        public static ComputePipelineState Create(ComputePipelineDesc desc, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            string dbgName = $"{file}, {line}";
            ComputePipeline pipeline = new(desc, dbgName);
            ComputePipelineState state = new(pipeline, dbgName);
            pipeline.Dispose();
            return state;
        }

        public ComputePipelineState(ComputePipeline pipeline, string dbgName = "")
        {
            pipeline.AddRef();
            this.pipeline = pipeline;
            this.dbgName = dbgName;

            resourceBindingList = new(pipeline);

            pipeline.OnCompile += OnPipelineCompile;
            cs = pipeline.cs;
        }

        private void OnPipelineCompile(IPipeline pipe)
        {
            ComputePipeline pipeline = (ComputePipeline)pipe;
            cs = pipeline.cs;
        }

        public ComputePipeline Pipeline => pipeline;

        public D3D11ResourceBindingList Bindings => resourceBindingList;

        public bool IsValid => pipeline.IsValid;

        public bool IsInitialized => pipeline.IsInitialized;

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
            context.CSSetShader(cs, (ID3D11ClassInstance**)null, 0);

            resourceBindingList.BindCompute(context);
        }

        internal override void UnsetState(ComPtr<ID3D11DeviceContext3> context)
        {
            context.CSSetShader((ID3D11ComputeShader*)null, (ID3D11ClassInstance**)null, 0);

            resourceBindingList.UnbindCompute(context);
        }

        protected override void DisposeCore()
        {
            pipeline.OnCompile -= OnPipelineCompile;
            pipeline.Dispose();

            resourceBindingList.Dispose();
        }
    }
}