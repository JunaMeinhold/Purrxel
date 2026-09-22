namespace App.Scripts
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;

    public class WorldController : ScriptComponent
    {
        private Vector3 CurrentPlayerChunkSegmentPos;
        private Vector3 CurrentPlayerChunkPos;
        private Vector3 CurrentPlayerLocalChunkPos;
        private Vector3 CurrentPlayerPos;
        private bool invalidate = true;
        private World world;
        private Player player;

        public override void Awake()
        {
            world = (World)GameObject;
            player = Scene.Find<Player>()!;
        }

        public override void Destroy()
        {
        }

        public override void FixedUpdate()
        {
            Vector3 pos = Scene.Camera.Transform.Position;

            float x = pos.X % Chunk.CHUNK_SIZE;
            float y = pos.Y % Chunk.CHUNK_SIZE;
            float z = pos.Z % Chunk.CHUNK_SIZE;

            CurrentPlayerLocalChunkPos = new((int)x, (int)y, (int)z);

            Vector3 chunkPos = pos - new Vector3(x, y, z);
            if (x < 0)
            {
                chunkPos.X -= Chunk.CHUNK_SIZE;
            }
            if (y < 0)
            {
                chunkPos.Y -= Chunk.CHUNK_SIZE;
            }
            if (z < 0)
            {
                chunkPos.Z -= Chunk.CHUNK_SIZE;
            }

            chunkPos /= Chunk.CHUNK_SIZE;

            CurrentPlayerChunkPos = new Vector3((int)chunkPos.X, (int)chunkPos.Y, (int)chunkPos.Z);
            CurrentPlayerPos = CurrentPlayerLocalChunkPos + CurrentPlayerChunkPos * Chunk.CHUNK_SIZE;
            chunkPos = new Vector3((int)chunkPos.X, 0, (int)chunkPos.Z);

            if (chunkPos.X == CurrentPlayerChunkSegmentPos.X & chunkPos.Z == CurrentPlayerChunkSegmentPos.Z & !invalidate)
            {
                return;
            }

            invalidate = false;
            CurrentPlayerChunkSegmentPos = chunkPos;
            world.WorldLoader.Dispatch((Point3)chunkPos);
        }

        public override unsafe void Update()
        {
            var context = D3D11DeviceManager.GraphicsContext;
            world.WorldLoader.Upload(context);
            byte* buffer = stackalloc byte[2048];
            StrBuilder sb = new(buffer, 2048);

            sb.Reset();
            sb.Append("Look at: "u8);
            sb.Append(player.LookAtBlock);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Chunk Segment: "u8);
            sb.Append(CurrentPlayerChunkSegmentPos);
            sb.End();
            ImGui.Text(sb);

            // Reset and reuse sb for other texts
            sb.Reset();
            sb.Append("Chunk: "u8);
            sb.Append(CurrentPlayerChunkPos);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Local Position: "u8);
            sb.Append(CurrentPlayerLocalChunkPos);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Position: "u8);
            sb.Append(CurrentPlayerPos);
            sb.End();
            ImGui.Text(sb);

            ImGui.Separator();

            sb.Reset();
            sb.Append("Worker Threads: loads/updates: "u8);
            sb.Append(world.WorldLoader.LoadQueueCount);
            sb.Append('/');
            sb.Append(world.WorldLoader.UpdateQueueCount);
            sb.Append(", gen "u8);
            sb.Append(world.WorldLoader.GenerationQueueCount);
            sb.Append(", uploads: "u8);
            sb.Append(world.WorldLoader.UploadQueueCount);
            if (world.WorldLoader.Idle)
                sb.Append(", Idle"u8);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("IO Threads: loads/unloads/saves: "u8);
            sb.Append(world.WorldLoader.LoadIOQueueCount);
            sb.Append('/');
            sb.Append(world.WorldLoader.UnloadIOQueueCount);
            sb.Append('/');
            sb.Append(world.WorldLoader.SaveIOQueueCount);
            if (world.WorldLoader.IOIdle)
                sb.Append(", Idle"u8);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Loaded Render Regions: "u8);
            sb.Append(world.WorldLoader.RenderRegionCount);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Loaded Chunk Segments: "u8);
            sb.Append(world.WorldLoader.ChunkSegmentCount);
            sb.End();
            ImGui.Text(sb);

            sb.Reset();
            sb.Append("Loaded Chunks: "u8);
            sb.Append(ChunkAllocator.AllocatedAmount);
            sb.End();
            ImGui.Text(sb);

            var directionalLight = Scene.LightSystem.ActiveDirectionalLight;

            if (directionalLight != null)
            {
                float deltaTime = Time.GameTimeNormalized * MathUtil.PI2 - MathUtil.PIDIV2;

                Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, deltaTime);

                directionalLight.Transform.Orientation = rotation;

                /*
                if (rot.Y > 45 && rot.Y < 135)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1);
                }
                else if (rot.Y > 135 && rot.Y < 225)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(1, 0.2f, (rot.Y - 135) / 90);
                }
                else if (ro > 45 && ro < 135)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 0.2f;
                }
                else if (ro > 135 && ro < 225)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(0.2f, 1, (ro - 135) / 90);
                }
                directionalLight.Color *= 5;
                */
            }
        }
    }

    public static class StrBuilderExtensions
    {
        public static void Append(this ref StrBuilder builder, Vector3 vector)
        {
            builder.Append("<"u8);
            builder.Append(vector.X);
            builder.Append(", "u8);
            builder.Append(vector.Y);
            builder.Append(", "u8);
            builder.Append(vector.Z);
            builder.Append(">"u8);
        }
    }
}