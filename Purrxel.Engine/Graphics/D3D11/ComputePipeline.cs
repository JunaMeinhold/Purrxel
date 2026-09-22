namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Debugging;

    public unsafe class ComputePipeline : DisposableRefBase, IPipeline, IDisposable
    {
        private readonly ComputePipelineDesc desc;
        private readonly string dbgName;
        private ShaderMacro[]? macros;

        internal ComPtr<ID3D11ComputeShader> cs;

        internal Shader* computeShaderBlob;

        private bool valid;
        private volatile bool initialized;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComputePipeline(ComputePipelineDesc desc, string dbgName = "")
        {
            this.desc = desc;
            this.dbgName = dbgName;
            macros = desc.Macros;
            Compile();
            initialized = true;
        }

        public ComputePipelineDesc Description => desc;

        public bool IsInitialized => initialized;

        public bool IsValid => valid;

        public ShaderMacro[]? Macros { get => macros; set => macros = value; }

        protected virtual ShaderMacro[] GetShaderMacros()
        {
            return macros ?? [];
        }

        public static event EventHandler? Reload;

        public event Action<IPipeline>? OnCompile;

        public static void ReloadShaders()
        {
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ...");
            Reload?.Invoke(null, EventArgs.Empty);
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ... done!");
        }

        protected virtual void OnReload(object? sender, EventArgs args)
        {
            initialized = false;

            if (cs.Handle != null)
            {
                cs.Release();
                cs = default;
            }

            if (computeShaderBlob != null)
            {
                Free(computeShaderBlob);
                computeShaderBlob = null;
            }

            Compile(true);

            initialized = true;
        }

        private unsafe void Compile(bool bypassCache = false)
        {
            var device = D3D11DeviceManager.Device;
            ShaderMacro[] macros = GetShaderMacros();

            if (desc.Path is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.ShaderEntry, desc.Path, "cs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ComPtr<ID3D11ComputeShader> computeShader;
                device.CreateComputeShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &computeShader.Handle);
                cs = computeShader;
                Utils.SetDebugName(cs.Handle, dbgName);

                computeShaderBlob = shader;
            }

            valid = true;

            OnCompile?.Invoke(this);
        }

        protected override void DisposeCore()
        {
            Reload -= OnReload;

            if (cs.Handle != null)
            {
                cs.Dispose();
                cs = null;
            }

            if (computeShaderBlob != null)
            {
                Free(computeShaderBlob);
                computeShaderBlob = null;
            }
        }
    }
}