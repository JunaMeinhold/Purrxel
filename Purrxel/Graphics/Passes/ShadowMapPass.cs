using App.Renderers;

namespace App.Graphics.Passes
{
    using App.Graphics.Effects;
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using Hexa.NET.DXGI;
    using HexaEngine.Graphics.Effects.Blur;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Lights;
    using VoxelEngine.Scenes;

    public class ShadowMapPass : RenderPass
    {
        private ConstantBuffer<CSMShadowParams> csmBuffer = null!;
        private GaussianBlur blurFilter = null!;
        private CopyEffect copyEffect = null!;
        private ReprojectEffect reprojectEffect = null!;
        private ClearSliceEffect clearSliceEffect = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            blurFilter = new(Format.R32G32Float, Config.Default.ShadowMapSize, Config.Default.ShadowMapSize);
            csmBuffer = creator.CreateConstantBuffer<CSMShadowParams>("CSMBuffer", CpuAccessFlags.Write).Value!;
            copyEffect = new(CopyFilter.None);
            reprojectEffect = new();
            clearSliceEffect = new();
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", csmBuffer);
        }

        protected override void DisposeCore()
        {
            blurFilter.Dispose();
            copyEffect.Dispose();
            reprojectEffect.Dispose();
            clearSliceEffect.Dispose();
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", null);
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            return;
            var directionalLight = scene.LightSystem.ActiveDirectionalLight;
            if (directionalLight == null)
            {
                return;
            }

            DoDirectional(context, directionalLight, scene.LightSystem, camera, scene);

            D3D11GlobalResourceList.SetSRV("CSMDepthBuffer", directionalLight.ShadowMap);
        }

        private void DoDirectional(GraphicsContext context, DirectionalLight directionalLight, LightSystem lights, Camera camera, Scene scene)
        {
            var old = csmBuffer[0];
            if (!directionalLight.UpdateShadowMap(context, lights.ShadowDataBuffer, csmBuffer, camera, out uint cascadeMask, out var reproject))
            {
                return; // false return means nothing to update.
            }

            var map = directionalLight.ShadowMap!;
            var csmDepthBuffer = directionalLight.DepthStencil!;

            clearSliceEffect.Clear(context, map.UAV!, (uint)map.Width, (uint)map.Height, (uint)map.ArraySize, cascadeMask);
            context.ClearDepthStencilView(csmDepthBuffer.DSV, Hexa.NET.D3D11.ClearFlag.Depth, 1, 0);

            context.SetRenderTarget(map.RTV, csmDepthBuffer.DSV);
            context.SetViewport(map.Viewport);

            var now = csmBuffer[0];

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);
            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);

            FilterArray(context, map, cascadeMask, old, now, reproject);
        }

        private void FilterArray(GraphicsContext context, Texture2D source, uint cascadeMask, CSMShadowParams old, CSMShadowParams now, bool reproject)
        {
            if (source.Width != blurFilter.Width || source.Height != blurFilter.Height || source.Format != blurFilter.Format)
            {
                blurFilter.Resize(source.Format, source.Width, source.Height);
            }

            for (int i = 0; i < source.ArraySize; i++)
            {
                if ((cascadeMask & (1 << i)) != 0)
                {
                    blurFilter.Blur(context, source.SRVArraySlices![i], source.RTVArraySlices![i], source.Width, source.Height);
                }
                else if (reproject)
                {
                    Reproject(context, source, i, old, now);
                    blurFilter.Blur(context, source.SRVArraySlices![i], source.RTVArraySlices![i], source.Width, source.Height);
                }
            }
        }

        private unsafe void Reproject(GraphicsContext context, Texture2D texture, int slice, CSMShadowParams old, CSMShadowParams now)
        {
            var oldViewProj = Matrix4x4.Transpose(old[slice]);
            var newViewProj = now[slice];

            Matrix4x4.Invert(oldViewProj, out var oldViewProjInv);
            oldViewProjInv = Matrix4x4.Transpose(oldViewProjInv);

            reprojectEffect.Reproject(context, texture.UAVArraySlices![slice], (uint)texture.Width, (uint)texture.Height, oldViewProjInv, newViewProj, ReprojectFlags.VSM);
        }
    }
}