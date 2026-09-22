namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Debugging;

    public unsafe class GraphicsPipeline : DisposableRefBase, IPipeline, IDisposable
    {
        private readonly ComPtr<ID3D11Device5> device = D3D11DeviceManager.Device;
        private readonly GraphicsPipelineDesc desc;
        private readonly string? dbgName;
        internal ComPtr<ID3D11VertexShader> vs;
        internal ComPtr<ID3D11HullShader> hs;
        internal ComPtr<ID3D11DomainShader> ds;
        internal ComPtr<ID3D11GeometryShader> gs;
        internal ComPtr<ID3D11PixelShader> ps;

        internal Shader* vertexShaderBlob;
        internal Shader* hullShaderBlob;
        internal Shader* domainShaderBlob;
        internal Shader* geometryShaderBlob;
        internal Shader* pixelShaderBlob;

        internal Blob? signature;
        internal InputElementDescription[]? inputElements;
        private bool valid;
        private volatile bool initialized;

        public GraphicsPipeline(GraphicsPipelineDesc desc, string? dbgName = null)
        {
            this.desc = desc;
            this.dbgName = dbgName;
            Compile();
            Reload += OnReload;
        }

        public GraphicsPipelineDesc Description => desc;

        public ShaderMacro[]? Macros => desc.Macros;

        public bool IsValid => valid;

        public bool IsInitialized => initialized;

        public static event EventHandler? Reload;

        public event Action<IPipeline>? OnCompile;

        public event Action<GraphicsPipeline, InputElementDescription[]?, Blob>? OnCreateLayout;

        #region Pipeline compilation

        protected virtual ShaderMacro[] GetShaderMacros()
        {
            return desc.Macros ?? [];
        }

        public static void ReloadShaders()
        {
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ...");
            Reload?.Invoke(null, EventArgs.Empty);
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ... done!");
        }

        protected virtual void OnReload(object? sender, EventArgs args)
        {
            initialized = false;

            if (vs.Handle != null)
            {
                vs.Release();
                vs = default;
            }

            if (hs.Handle != null)
            {
                hs.Release();
                hs = default;
            }

            if (ds.Handle != null)
            {
                ds.Release();
                ds = default;
            }

            if (gs.Handle != null)
            {
                gs.Release();
                gs = default;
            }

            if (ps.Handle != null)
            {
                ps.Release();
                ps = default;
            }

            if (signature != null)
            {
                signature.Dispose();
                signature = null;
            }

            if (vertexShaderBlob != null)
            {
                Free(vertexShaderBlob);
                vertexShaderBlob = null;
            }

            if (hullShaderBlob != null)
            {
                Free(hullShaderBlob);
                hullShaderBlob = null;
            }

            if (domainShaderBlob != null)
            {
                Free(domainShaderBlob);
                domainShaderBlob = null;
            }

            if (geometryShaderBlob != null)
            {
                Free(geometryShaderBlob);
                geometryShaderBlob = null;
            }

            if (pixelShaderBlob != null)
            {
                Free(pixelShaderBlob);
                pixelShaderBlob = null;
            }

            Compile(true);
            initialized = true;
        }

        private static bool CanSkipLayout(InputElementDescription[]? inputElements)
        {
            ArgumentNullException.ThrowIfNull(inputElements, nameof(inputElements));

            for (int i = 0; i < inputElements.Length; i++)
            {
                InputElementDescription inputElement = inputElements[i];
                if (inputElement.SemanticName is not "SV_VertexID" and not "SV_InstanceID")
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Compile(bool bypassCache = false)
        {
            ShaderMacro[] macros = GetShaderMacros();
            if (desc.VertexShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFileWithInputSignature(desc.VertexShaderEntrypoint, desc.VertexShader, "vs_5_0", macros, &shader, out inputElements, out signature, bypassCache);
                if (shader == null || signature == null || inputElements == null)
                {
                    valid = false;
                    return;
                }

                OnCreateLayout?.Invoke(this, inputElements, signature);

                ComPtr<ID3D11VertexShader> vertexShader;
                device.CreateVertexShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &vertexShader.Handle);
                vs = vertexShader;
                Utils.SetDebugName(vs.Handle, $"{dbgName}.{nameof(vs)}");

                vertexShaderBlob = shader;
            }
            if (desc.HullShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.HullShaderEntrypoint, desc.HullShader, "hs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ComPtr<ID3D11HullShader> hullShader;
                device.CreateHullShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &hullShader.Handle);
                hs = hullShader;
                Utils.SetDebugName(hs.Handle, $"{dbgName}.{nameof(hs)}");

                hullShaderBlob = shader;
            }
            if (desc.DomainShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.DomainShaderEntrypoint, desc.DomainShader, "ds_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ComPtr<ID3D11DomainShader> domainShader;
                device.CreateDomainShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &domainShader.Handle);
                ds = domainShader;
                Utils.SetDebugName(ds.Handle, $"{dbgName}.{nameof(hs)}");

                domainShaderBlob = shader;
            }
            if (desc.GeometryShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.GeometryShaderEntrypoint, desc.GeometryShader, "gs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ComPtr<ID3D11GeometryShader> geometryShader;
                device.CreateGeometryShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &geometryShader.Handle);
                gs = geometryShader;
                Utils.SetDebugName(gs.Handle, $"{dbgName}.{nameof(gs)}");

                geometryShaderBlob = shader;
            }
            if (desc.PixelShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.PixelShaderEntrypoint, desc.PixelShader, "ps_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ComPtr<ID3D11PixelShader> pixelShader;
                device.CreatePixelShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &pixelShader.Handle);
                ps = pixelShader;
                Utils.SetDebugName(ps.Handle, $"{dbgName}.{nameof(ps)}");

                pixelShaderBlob = shader;
            }

            valid = true;

            OnCompile?.Invoke(this);
        }

        #endregion Pipeline compilation

        #region Dispose

        protected override void DisposeCore()
        {
            Reload -= OnReload;

            if (vs.Handle != null)
            {
                vs.Release();
                vs = default;
            }

            if (hs.Handle != null)
            {
                hs.Release();
                hs = default;
            }

            if (ds.Handle != null)
            {
                ds.Release();
                ds = default;
            }

            if (gs.Handle != null)
            {
                gs.Release();
                gs = default;
            }

            if (ps.Handle != null)
            {
                ps.Release();
                ps = default;
            }

            if (signature != null)
            {
                signature.Dispose();
                signature = null;
            }

            if (vertexShaderBlob != null)
            {
                Free(vertexShaderBlob);
                vertexShaderBlob = null;
            }

            if (hullShaderBlob != null)
            {
                Free(hullShaderBlob);
                hullShaderBlob = null;
            }

            if (domainShaderBlob != null)
            {
                Free(domainShaderBlob);
                domainShaderBlob = null;
            }

            if (geometryShaderBlob != null)
            {
                Free(geometryShaderBlob);
                geometryShaderBlob = null;
            }

            if (pixelShaderBlob != null)
            {
                Free(pixelShaderBlob);
                pixelShaderBlob = null;
            }
        }

        #endregion Dispose
    }
}