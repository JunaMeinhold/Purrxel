namespace VoxelEngine.Lights
{
    using HexaEngine.Queries.Generic;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;

    public struct LightParams
    {
        public uint LightCount;
        public Vector3 Ambient;

        public LightParams(uint lightCount, Vector3 ambient)
        {
            LightCount = lightCount;
            Ambient = ambient;
        }
    }

    public class LightSystem : ISceneSystem
    {
        private readonly ObjectTypeQuery<Light> lights = new();

        private readonly EventHandlers<LightSystem, Light> activeLightsChanged = new();

        private StructuredBuffer<LightData> lightBuffer;
        private StructuredBuffer<ShadowData> shadowDataBuffer;
        private ConstantBuffer<LightParams> lightParamsBuffer;

        private readonly List<Light> activeLights = [];
        private DirectionalLight? activeDirectionalLight;
        private bool dirty = true;
        private Vector3 ambient = new(0.2f);
        private readonly Lock _lock = new();

        public string Name { get; } = "Light System";

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy;

        public DirectionalLight? ActiveDirectionalLight => activeDirectionalLight;

        public IReadOnlyList<Light> ActiveLights => activeLights;

        public event EventHandler<LightSystem, Light> ActiveLightsChanged { add => activeLightsChanged.AddHandler(value); remove => activeLightsChanged.RemoveHandler(value); }

        public Vector3 Ambient { get => ambient; set => ambient = value; }

        public StructuredBuffer<ShadowData> ShadowDataBuffer => shadowDataBuffer;

        public void Awake(Scene scene)
        {
            lights.OnAdded += LightsOnAdded;
            lights.OnRemoved += LightsOnRemoved;

            scene.QueryManager.AddQuery(lights);

            lightBuffer = new(CpuAccessFlags.Write);
            lightBuffer.Resize += BufferResize;
            shadowDataBuffer = new(CpuAccessFlags.Write);
            shadowDataBuffer.Resize += BufferResize;
            lightParamsBuffer = new(CpuAccessFlags.Write);
            D3D11GlobalResourceList.SetSRV("LightBuffer", lightBuffer.SRV);
            D3D11GlobalResourceList.SetSRV("ShadowDataBuffer", shadowDataBuffer.SRV);
            D3D11GlobalResourceList.SetCBV("LightParams", lightParamsBuffer);
        }

        public void Update(GraphicsContext context)
        {
            lock (_lock)
            {
                lightBuffer.ResetCounter();
                shadowDataBuffer.ResetCounter();

                var camera = SceneManager.Current.Camera;

                uint csmCount = 0;
                for (int i = 0; i < activeLights.Count; i++)
                {
                    var light = activeLights[i];

                    if (light.CastShadows)
                    {
                        if (!light.HasShadowMap)
                        {
                            light.CreateShadowMap();
                        }

                        shadowDataBuffer.Add(default);
                    }
                    else
                    {
                        if (light.HasShadowMap)
                        {
                            light.DestroyShadowMap();
                        }
                    }

                    if (light.CastShadows)
                    {
                        switch (light.Type)
                        {
                            case LightType.Directional:
                                if (csmCount == 1)
                                {
                                    continue;
                                }
                                var dir = (DirectionalLight)light;
                                dir.ShadowMapIndex = (int)shadowDataBuffer.Count;
                                lightBuffer.Add(new(dir));
                                shadowDataBuffer.Add(new(dir, dir.Size));
                                csmCount++;
                                break;
                        }
                    }
                    else
                    {
                        switch (light.Type)
                        {
                            case LightType.Directional:
                                light.ShadowMapIndex = (int)lightBuffer.Count;
                                lightBuffer.Add(new((DirectionalLight)light));
                                break;
                        }
                    }
                    light.UpdateShadowBuffer(shadowDataBuffer, camera);
                }

                lightBuffer.Update(context);
                shadowDataBuffer.Update(context);
                lightParamsBuffer.Update(context, new LightParams((uint)activeLights.Count, ambient));
            }
        }

        private void BufferResize(object? sender, CapacityChangedEventArgs e)
        {
            D3D11GlobalResourceList.SetSRV("LightBuffer", lightBuffer.SRV);
            D3D11GlobalResourceList.SetSRV("ShadowDataBuffer", shadowDataBuffer.SRV);
        }

        public void Destroy()
        {
            D3D11GlobalResourceList.SetSRV("LightBuffer", null);
            D3D11GlobalResourceList.SetSRV("ShadowDataBuffer", null);
            D3D11GlobalResourceList.SetCBV("LightParams", null);
            lightBuffer.Dispose();
            shadowDataBuffer.Dispose();
            activeLightsChanged.Clear();
            activeLights.Clear();
            lights.Dispose();
            lightParamsBuffer.Dispose();
        }

        private void LightsOnAdded(Light light)
        {
            lock (_lock)
            {
                light.EnabledChanged += LightEnabled;
                light.CastsShadowsChanged += LightCastsShadowsChanged;
                light.PropertyChanged += LightPropertyChanged;
                light.TransformUpdated += LightTransformChanged;
                if (light.Enabled)
                {
                    activeLights.Add(light);
                    if (light is DirectionalLight directional)
                    {
                        activeDirectionalLight = directional;
                    }
                    OnActiveLightsChanged(light);
                }
            }
        }

        private void LightsOnRemoved(Light light)
        {
            lock (_lock)
            {
                light.EnabledChanged -= LightEnabled;
                light.CastsShadowsChanged -= LightCastsShadowsChanged;
                light.PropertyChanged -= LightPropertyChanged;
                light.TransformUpdated -= LightTransformChanged;
                if (light.Enabled)
                {
                    activeLights.Remove(light);
                    if (light is DirectionalLight)
                    {
                        activeDirectionalLight = null;
                    }

                    OnActiveLightsChanged(light);
                }

                if (light.HasShadowMap)
                {
                    light.DestroyShadowMap();
                }
            }
        }

        private void LightTransformChanged(GameObject sender, Hexa.NET.Mathematics.Transform args)
        {
            if (!sender.Enabled)
            {
                return;
            }

            dirty = true;
        }

        private void LightCastsShadowsChanged(Light sender, bool enabled)
        {
            if (!sender.Enabled)
            {
                return;
            }

            dirty = true;
        }

        private void LightPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!((Light)sender!).Enabled)
            {
                return;
            }

            dirty = true;
        }

        private void OnActiveLightsChanged(Light light)
        {
            if (!light.Enabled && light.HasShadowMap)
            {
                light.DestroyShadowMap();
            }
            activeLightsChanged.Invoke(this, light);
            dirty = true;
        }

        private void LightEnabled(GameObject sender, bool enabled)
        {
            Light light = (Light)sender;
            lock (_lock)
            {
                if (enabled)
                {
                    activeLights.Add(light);
                    if (light is DirectionalLight directional)
                    {
                        activeDirectionalLight = directional;
                    }
                }
                else
                {
                    activeLights.Remove(light);
                    if (light is DirectionalLight)
                    {
                        activeDirectionalLight = null;
                    }
                }

                OnActiveLightsChanged(light);
            }
        }
    }
}