namespace VoxelEngine.Physics
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using HexaEngine.Queries.Generic;
    using System.Numerics;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;
    using VoxelEngine.Core;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using static Hexa.NET.Utilities.IO.FileUtils;

    public interface IPhysicsComponent : IComponent
    {
        public void PreTick(PhysicsSystem system);

        public void PostTick(PhysicsSystem system);
    }

    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public bool Hit;
        public int BlockX, BlockY, BlockZ;
    }

    public enum ShapeType
    {
        Box,
    }

    public interface IShape
    {
        public ShapeType Type { get; }
    }

    public struct Pose
    {
        public Vector3 Position;
        public Vector3 Rotation;
    }

    public struct Shape
    {
        public readonly ShapeType Type;
        public Pose Pose;
    }

    public struct BoxShape : IShape
    {
        private readonly ShapeType type = ShapeType.Box;
        public Pose Pose;
        public Vector3 Size;

        public BoxShape(Vector3 size)
        {
            Size = size;
        }

        public readonly ShapeType Type => type;
    }

    public unsafe struct DynamicActor
    {
        internal Pose pose;
        internal Pose lastPose;

        internal Vector3 LinearVelocity;
        internal Vector3 AngularVelocity;
        internal UnsafeList<Pointer<Shape>> shapes;

        public bool Grounded;

        public void SetPosition(Vector3 position)
        {
            if (pose.Position == position) return;
            Grounded = false;
            pose.Position = position;
        }

        public void Move(Vector3 position)
        {
            if (pose.Position == position) return;
            Grounded = false;
            lastPose = pose;
            pose.Position = position;
        }

        public Vector3 GetPosition() => pose.Position;

        public void AddShape<T>(T shape) where T : unmanaged, IShape
        {
            var s = AllocT(shape);
            shapes.Add((Shape*)s);
        }
    }

    public unsafe class PhysicsSystem : ISceneSystem
    {
        private World world;
        private readonly ComponentTypeQuery<IPhysicsComponent> components = new();
        private UnsafeList<Pointer<DynamicActor>> actors;
        private bool awaked;

        public string Name { get; } = "Physics System";

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy | SystemFlags.PhysicsUpdate;

        public void Awake(Scene scene)
        {
            awaked = true;
            world = scene.Find<World>()!;
            components.OnAdded += OnAdded;
            components.OnRemoved += OnRemoved;
            scene.QueryManager.AddQuery(components);
        }

        private void OnRemoved(GameObject gameObject, IPhysicsComponent component)
        {
            if (awaked)
            {
                component.Destroy();
            }
        }

        private void OnAdded(GameObject gameObject, IPhysicsComponent component)
        {
            if (awaked)
            {
                component.Awake();
            }
        }

        public void Destroy()
        {
            components.OnAdded -= OnAdded;
            components.OnRemoved -= OnRemoved;
            components.Dispose();
            awaked = false;
        }

        public DynamicActor* CreateActor()
        {
            var actor = AllocT<DynamicActor>();
            ZeroMemoryT(actor);
            actors.Add(actor);
            return actor;
        }

        public void DestroyActor(DynamicActor* actor)
        {
            actors.Remove(actor);
            Free(actor);
        }

        public void FixedUpdate()
        {
            float deltaTime = Time.FixedDelta;

            foreach (var component in components)
            {
                component.PreTick(this);
            }

            for (int i = 0; i < actors.Count; i++)
            {
                DynamicActor* actor = actors[i];
                Pose* pose = &actor->pose;

                actor->LinearVelocity.Y += -9.81f * deltaTime;

                Vector3 desiredMovement = actor->LinearVelocity * deltaTime;
                Vector3 newPosition = SweepMove(actor, desiredMovement);

                pose->Position = newPosition;
                actor->lastPose = *pose;
            }

            foreach (var component in components)
            {
                component.PostTick(this);
            }
        }

        public unsafe Vector3 SweepMove(DynamicActor* actor, Vector3 movement)
        {
            if (actor->shapes.Size == 0)
                return actor->pose.Position;

            Vector3 position = actor->pose.Position;
            actor->Grounded = false;

            for (int i = 0; i < actor->shapes.Size; i++)
            {
                Shape* shape = actor->shapes[i];

                switch (shape->Type)
                {
                    case ShapeType.Box:
                        BoxShape* box = (BoxShape*)shape;

                        position.X = SweepAxis(position, movement.X, Vector3.UnitX, box, actor);
                        position.Y = SweepAxis(position, movement.Y, Vector3.UnitY, box, actor);
                        position.Z = SweepAxis(position, movement.Z, Vector3.UnitZ, box, actor);
                        break;

                    default:
                        break;
                }
            }

            return position;
        }

        public unsafe Vector3 MoveWithCollision(DynamicActor* actor, Vector3 targetPosition)
        {
            Vector3 currentPosition = actor->pose.Position;
            Vector3 movement = targetPosition - currentPosition;
            
            Vector3 newPosition = SweepMove(actor, movement);
            actor->pose.Position = newPosition;
            actor->lastPose.Position = currentPosition;
            
            return newPosition;
        }

        public unsafe float SweepAxis(Vector3 position, float movement, Vector3 axis, BoxShape* box, DynamicActor* actor)
        {
            if (Math.Abs(movement) < 0.0001f)
                return Vector3.Dot(position, axis);

            float currentPos = Vector3.Dot(position, axis);
            float targetPos = currentPos + movement;

            const int steps = 10;
            float stepSize = movement / steps;

            for (int step = 0; step < steps; step++)
            {
                float testPos = currentPos + stepSize * (step + 1);
                Vector3 testPosition = position + axis * (testPos - currentPos);

                if (IsBoxColliding(testPosition, box))
                {
                    if (axis == Vector3.UnitY && movement < 0)
                    {
                        actor->Grounded = true;
                    }

                    actor->LinearVelocity *= Vector3.One - axis;
                    return currentPos;
                }

                currentPos = testPos;
            }

            return targetPos;
        }

        private unsafe bool IsBoxColliding(Vector3 actorPosition, BoxShape* box)
        {
            Vector3 worldBoxPosition = actorPosition + box->Pose.Position;
            Vector3 halfExtents = box->Size * 0.5f;

            Vector3 min = worldBoxPosition - halfExtents;
            Vector3 max = worldBoxPosition + halfExtents;

            min = Vector128.Floor(min.AsVector128()).AsVector3();
            max = Vector128.Floor(max.AsVector128()).AsVector3();
            int minX = (int)min.X;
            int minY = (int)min.Y;
            int minZ = (int)min.Z;
            int maxX = (int)max.X;
            int maxY = (int)max.Y;
            int maxZ = (int)max.Z;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (!world.IsNoBlock(x, y, z))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsVoxelSolid(Vector3 position)
        {
            position = Vector128.Floor(position.AsVector128()).AsVector3();
            int voxelX = (int)position.X;
            int voxelY = (int)position.Y;
            int voxelZ = (int)position.Z;

            return !world.IsNoBlock(voxelX, voxelY, voxelZ);
        }

        public unsafe bool HitCeiling(DynamicActor* actor, float maxCheckDistance = 0.5f)
        {
            Pose* pose = &actor->pose;
            RaycastHit hit = CastRay(pose->Position, Vector3.UnitY, maxCheckDistance);

            if (hit.Hit)
            {
                pose->Position.Y = hit.Position.Y - 1;
                actor->LinearVelocity.Y = 0;
                return true;
            }

            return false;
        }

        public RaycastHit CastRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return CastRay(origin, direction, maxDistance, world);
        }

        public static RaycastHit CastRay(Vector3 origin, Vector3 direction, float maxDistance, World world)
        {
            Vector3D rayPos = origin;
            Vector3D rayDir = Vector3D.Normalize(direction);

            long x = (long)Math.Floor(rayPos.X);
            long y = (long)Math.Floor(rayPos.Y);
            long z = (long)Math.Floor(rayPos.Z);

            long stepX = Math.Sign(rayDir.X);
            long stepY = Math.Sign(rayDir.Y);
            long stepZ = Math.Sign(rayDir.Z);

            double tMaxX = (x + (stepX > 0 ? 1 : 0) - rayPos.X) / rayDir.X;
            double tMaxY = (y + (stepY > 0 ? 1 : 0) - rayPos.Y) / rayDir.Y;
            double tMaxZ = (z + (stepZ > 0 ? 1 : 0) - rayPos.Z) / rayDir.Z;

            double tDeltaX = Math.Abs(1 / rayDir.X);
            double tDeltaY = Math.Abs(1 / rayDir.Y);
            double tDeltaZ = Math.Abs(1 / rayDir.Z);

            int lastStepAxis = 0;

            for (double t = 0; t < maxDistance;)
            {
                Chunk* chunk = world.Get((int)(x >> 4), (int)(y >> 4), (int)(z >> 4));
                if (chunk == null || !chunk->InMemory)
                    return new RaycastHit { Hit = false };

                Block block = chunk->GetBlockInternal((int)(x & 15), (int)(y & 15), (int)(z & 15));
                if (block.Type != 0)
                {
                    Vector3 normal = lastStepAxis == 0 ? new Vector3(-stepX, 0, 0) :
                              lastStepAxis == 1 ? new Vector3(0, -stepY, 0) :
                              new Vector3(0, 0, -stepZ);

                    return new RaycastHit
                    {
                        Hit = true,
                        Position = new Vector3(x, y, z),
                        Normal = normal,
                        BlockX = (int)x,
                        BlockY = (int)y,
                        BlockZ = (int)z
                    };
                }

                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    x += stepX;
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    lastStepAxis = 0;
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    lastStepAxis = 1;
                }
                else
                {
                    z += stepZ;
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    lastStepAxis = 2;
                }
            }

            return new RaycastHit { Hit = false };
        }
    }
}