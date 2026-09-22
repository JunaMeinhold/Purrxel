namespace VoxelEngine.Physics
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using HexaEngine.Queries.Generic;
    using System.Numerics;
    using System.Runtime.Intrinsics;
    using VoxelEngine.Core;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public unsafe class PhysicsSystem : ISceneSystem
    {
        private World world;
        private readonly ComponentTypeQuery<IPhysicsComponent> components = new();
        private UnsafeList<Pointer<DynamicActor>> actors;
        private UnsafeList<Pointer<KinematicActor>> kinematicActors;
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

        public KinematicActor* CreateKinematicActor()
        {
            var actor = AllocT<KinematicActor>();
            ZeroMemoryT(actor);
            kinematicActors.Add(actor);
            return actor;
        }

        public void DestroyKinematicActor(KinematicActor* actor)
        {
            kinematicActors.Remove(actor);
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

        public unsafe float SweepAxis(Vector3 position, float movement, Vector3 axis, BoxShape* box, DynamicActor* actor)
        {
            var movementAbs = Math.Abs(movement);
            if (movementAbs < 0.0001f)
                return Vector3.Dot(position, axis);

            float currentPos = Vector3.Dot(position, axis);
            float targetPos = currentPos + movement;

            const float unitPerStep = 0.1f;
            int steps = (int)MathF.Ceiling(movementAbs / unitPerStep);
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
            Vector128<float> worldBoxPosition = actorPosition.AsVector128() + box->Pose.Position.AsVector128();
            Vector128<float> halfExtents = box->Size.AsVector128() * 0.5f;

            Vector128<float> min = worldBoxPosition - halfExtents;
            Vector128<float> max = worldBoxPosition + halfExtents;
            Vector128<int> minI = Vector128.ConvertToInt32(Vector128.Floor(min));
            Vector128<int> maxI = Vector128.ConvertToInt32(Vector128.Floor(max));
            Vector128<int> minC = minI >> Chunk.CHUNK_SHIFT_Y;
            Vector128<int> maxC = maxI >> Chunk.CHUNK_SHIFT_Y;
            var mask = Vector128.Create(Chunk.CHUNK_MASK);
            Vector128<uint> minL = (minI & mask).AsUInt32();
            Vector128<uint> maxL = (maxI & mask).AsUInt32();
            int minChunkX = minC[0];
            int minChunkY = minC[1];
            int minChunkZ = minC[2];
            int maxChunkX = maxC[0];
            int maxChunkY = maxC[1];
            int maxChunkZ = maxC[2];

            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                for (int cz = minChunkZ; cz <= maxChunkZ; cz++)
                {
                    for (int cx = minChunkX; cx <= maxChunkX; cx++)
                    {
                        if (cx < World.CHUNK_AMOUNT_X_MIN || cx >= World.CHUNK_AMOUNT_X ||
                            cy < World.CHUNK_AMOUNT_Y_MIN || cy >= World.CHUNK_AMOUNT_Y ||
                            cz < World.CHUNK_AMOUNT_Z_MIN || cz >= World.CHUNK_AMOUNT_Z)
                        {
                            continue;
                        }

                        Chunk* chunk = world.Chunks.GetUnsafe(new(cx, cy, cz));
                        if (chunk == null || !chunk->InMemory)
                        {
                            continue;
                        }

                        uint localMinX = cx == minChunkX ? minL[0] : 0;
                        uint localMinY = cy == minChunkY ? minL[1] : 0;
                        uint localMinZ = cz == minChunkZ ? minL[2] : 0;
                        uint localMaxX = cx == maxChunkX ? maxL[0] : 15;
                        uint localMaxY = cy == maxChunkY ? maxL[1] : 15;
                        uint localMaxZ = cz == maxChunkZ ? maxL[2] : 15;

                        for (uint z = localMinZ; z <= localMaxZ; z++)
                        {
                            var zShift = z << Chunk.CHUNK_SHIFT_Z;
                            var heightMapAccess = z * Chunk.CHUNK_SIZE;
                            for (uint x = localMinX; x <= localMaxX; x++)
                            {
                                var xShift = x << Chunk.CHUNK_SHIFT_Y;
                                var heightIdx = heightMapAccess++;

                                var minY = chunk->MinY[heightIdx];
                                var maxY = chunk->MaxY[heightIdx];

                                var startY = Math.Max(localMinY, minY);
                                var endY = Math.Min(localMaxY, maxY);

                                var access = xShift + zShift + startY;
                                for (uint y = startY; y <= endY; y++, access++)
                                {
                                    if (chunk->Data[access].Type != Chunk.EMPTY)
                                    {
                                        return true;
                                    }
                                }
                            }
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

        public unsafe Vector3 MoveKinematic(KinematicActor* actor, Vector3 targetPosition, float step)
        {
            Vector3 currentPosition = actor->pose.Position;
            Vector3 movement = targetPosition - currentPosition;

            if (actor->shapes.Size == 0)
            {
                actor->pose.Position = targetPosition;
                return targetPosition;
            }

            Vector3 newPosition = currentPosition;

            for (int i = 0; i < actor->shapes.Size; i++)
            {
                Shape* shape = actor->shapes[i];

                switch (shape->Type)
                {
                    case ShapeType.Box:
                        BoxShape* box = (BoxShape*)shape;

                        newPosition.Y = SweepAxisKinematic(newPosition, movement.Y, Vector3.UnitY, box, actor, step);
                        newPosition.X = SweepAxisKinematic(newPosition, movement.X, Vector3.UnitX, box, actor, step);
                        newPosition.Z = SweepAxisKinematic(newPosition, movement.Z, Vector3.UnitZ, box, actor, step);
                        const float epsilon = 0.0001f;
                        const float epsilonSq = epsilon * epsilon;
                        var delta = newPosition - currentPosition;
                        delta.Y = 0;
                        var length = delta.LengthSquared();
                        if (length > epsilonSq && float.Abs(movement.Y) < epsilon)
                        {
                            Vector3 groundCheck = newPosition;
                            groundCheck.Y -= 0.0001f;
                            if (!IsBoxColliding(groundCheck, box))
                            {
                                actor->Grounded = false;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            actor->pose.Position = newPosition;
            actor->lastPose.Position = currentPosition;

            return newPosition;
        }

        private unsafe float SweepAxisKinematic(Vector3 position, float movement, Vector3 axis, BoxShape* box, KinematicActor* actor, float stepSize)
        {
            const float epsilon = 0.0001f;
            if (Math.Abs(movement) < epsilon)
                return Vector3.Dot(position, axis);

            float currentPos = Vector3.Dot(position, axis);
            float targetPos = currentPos + movement;
            float delta = targetPos - currentPos;

            int steps = (int)float.Ceiling(float.Abs(delta) / stepSize);
            stepSize = float.CopySign(stepSize, delta);

            for (int step = 0; step < steps; step++)
            {
                float offset = float.MinMagnitude(stepSize * (step + 1), delta);
                Vector3 testPosition = position + axis * offset;

                if (IsBoxColliding(testPosition, box))
                {
                    if (axis.Y != 0)
                    {
                        actor->Grounded = movement < 0;
                    }
                    return currentPos + stepSize * step;
                }
            }

            if (axis.Y != 0 && movement > 0)
            {
                actor->Grounded = false;
            }
            return targetPos;
        }
    }
}