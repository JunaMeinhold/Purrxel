namespace VoxelEngine.Lightning
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using System;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lights;
    using VoxelEngine.Scenes;

    public class DirectionalLight : Light
    {
        public new CameraTransform Transform = new();

        public DirectionalLight()
        {
            OverwriteTransform(Transform);
            for (int i = 0; i < 8; i++)
            {
                ShadowFrustra[i] = new();
            }
            Transform.Near = 0.1f;
        }

        public override LightType Type => LightType.Directional;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[8];

        public int cascadeCount = 4;
        public Texture2D? ShadowMap;
        public DepthStencil? DepthStencil;
        public int Size = Config.Default.ShadowMapSize;
        public float LightBleedingReduction = 0.1f;
        private ShadowData data;
        public CSMConfig CSMConfig = new();

        public override bool HasShadowMap => ShadowMap != null;

        public override void Awake()
        {
            base.Awake();
        }

        public override void CreateShadowMap()
        {
            if (ShadowMap != null) return;
            ShadowMap = new(Format.R32G32Float, Size, Size, cascadeCount - 1, gpuAccessFlags: GpuAccessFlags.All);
            ShadowMap.CreateArraySlices();
            DepthStencil = new(Format.D32Float, Size, Size, cascadeCount - 1);
        }

        public override void DestroyShadowMap()
        {
            if (ShadowMap == null || DepthStencil == null) return;
            ShadowMap.Dispose();
            ShadowMap = null;
            DepthStencil.Dispose();
            DepthStencil = null;
        }

        private Vector3 camOldPos;
        private Vector3 camOldRot;

        private Vector3 oldRot;

        private uint dirtyCascades;
        private ShadowData shadowDataLast;

        public unsafe bool UpdateShadowMap(GraphicsContext context, StructuredBuffer<ShadowData> buffer, ConstantBuffer<CSMShadowParams> csmConstantBuffer, Camera camera, out uint updateMask, out bool reproject)
        {
            if (ShadowMap == null)
            {
                updateMask = 0;
                reproject = false;
                return false;
            }

            var rot = Transform.GlobalOrientation.ToYawPitchRoll();

            var rotDelta = rot - oldRot;

            var camPos = camera.Transform.GlobalPosition;
            var camRot = camera.Transform.GlobalOrientation.ToYawPitchRoll();

            var camPosDelta = camPos - camOldPos;
            var camRotDelta = camRot - camOldRot;

            const float motionEpsilon = 0.00000001f;
            // Determine if we need to update based on camera movement
            bool positionChanged = camPosDelta.LengthSquared() > motionEpsilon;
            bool rotationChanged = camRotDelta.LengthSquared() > motionEpsilon || rotDelta.LengthSquared() > 0;

            // Check if we need to update the cascade shadow maps
            if (!positionChanged && !rotationChanged)
            {
                reproject = false;
                if (dirtyCascades == 0)
                {
                    updateMask = 0;
                    return false; // No significant changes, skip update
                }
            }
            else
            {
                dirtyCascades = (1u << (cascadeCount - 1)) - 1;  // set cascadeCount - 1 bits only
                reproject = true; // signal caller to reproject/reuse depth values.
            }

            oldRot = rot;

            camOldPos = camPos;
            camOldRot = camRot;

            var frame = Time.Frame;
            updateMask = 0;

            for (int i = 0; i < cascadeCount - 1; i++)
            {
                var frequency = 1u << i; // equivalent to pow(2, i), this might get changed.
                var flag = 1u << i;
                if (frame % frequency == 0 && (dirtyCascades & flag) != 0)
                {
                    updateMask |= flag;
                    dirtyCascades &= ~flag; // clear dirty flag.
                }
            }

            ShadowData* data = buffer.Items + ShadowMapIndex;

            Matrix4x4* views = ShadowData.GetViews(data);
            float* cascades = ShadowData.GetCascades(data);

            CSMShadowParams shadowParams = default;

            if (reproject) // only update matrices if needed if not use the last, because updating everytime would cause numerical instability and performance penalties.
            {
                CSMConfig.CascadeCount = cascadeCount;
                CSMConfig.ShadowMapSize = Size;

                var matrices = CSMHelper.GetLightSpaceMatrices(camera, Transform, views, cascades, ShadowFrustra, CSMConfig);
                MemcpyT(matrices, &shadowParams.View0, cascadeCount - 1);
                shadowDataLast = *data;
            }
            else
            {
                *data = shadowDataLast;
                MemcpyT(views, &shadowParams.View0, cascadeCount - 1);
            }

            shadowParams.CascadeCount = (uint)(cascadeCount - 1);
            shadowParams.ActiveCascades = updateMask;

            *csmConstantBuffer.Local = shadowParams;
            csmConstantBuffer.Update(context);

            return true;
        }

        public unsafe void Update(GraphicsContext context, ConstantBuffer<CSMShadowParams> csmMatrixBuffer)
        {
            ShadowData shadow = data;
            Matrix4x4* views = ShadowData.GetViews(&shadow);
            CSMShadowParams buffer = new(views, (uint)(cascadeCount - 1));
            csmMatrixBuffer.Update(context, buffer);
        }

        public override unsafe void UpdateShadowBuffer(StructuredBuffer<ShadowData> buffer, Camera camera)
        {
            ShadowData* data = buffer.Items + ShadowMapIndex;
            data->Softness = LightBleedingReduction;

            Matrix4x4* views = ShadowData.GetViews(data);
            float* cascades = ShadowData.GetCascades(data);

            CSMConfig.CascadeCount = cascadeCount;
            CSMConfig.ShadowMapSize = Size;

            CSMHelper.GetLightSpaceMatrices(camera.Transform, Transform, views, cascades, ShadowFrustra, CSMConfig);
        }
    }
}