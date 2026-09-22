namespace VoxelEngine.Scenes
{
    using Hexa.NET.Mathematics;
    using HexaEngine.Queries;
    using HexaEngine.Queries.Generic;
    using System.Collections.Concurrent;
    using System.Numerics;

    public static class TransformExtensions
    {
        public static Vector3 GetAbsGlobalPosition(this Transform transform)
        {
            return SceneManager.Current.TransformSystem.ToAbsoluteWorldPosition(transform.GlobalPosition);
        }
    }

    public class TransformSystem : ISceneSystem
    {
        private readonly ObjectTypeQuery<GameObject> objects = new(QueryFlags.ObjectAdded | QueryFlags.ObjectRemoved);
        private readonly ConcurrentQueue<Transform> updateQueue = new();
        private Point3 worldOrigin;

        public string Name => "TransformUpdate";

        public SystemFlags Flags => SystemFlags.LateUpdate | SystemFlags.Awake | SystemFlags.Destroy;

        public void Awake(Scene scene)
        {
            scene.QueryManager.AddQuery(objects);
            objects.OnAdded += OnAdded;
            objects.OnRemoved += OnRemoved;
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].TransformChanged += TransformChanged;
                objects[i].Transform.Recalculate();
            }
        }

        private void OnRemoved(GameObject gameObject)
        {
            gameObject.TransformChanged -= TransformChanged;
        }

        private void OnAdded(GameObject gameObject)
        {
            gameObject.TransformChanged += TransformChanged;
            gameObject.Transform.Recalculate();
        }

        public void Destroy()
        {
            objects.OnAdded -= OnAdded;
            objects.OnRemoved -= OnRemoved;
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].TransformChanged -= TransformChanged;
            }
            objects.Dispose();
            updateQueue.Clear();
        }

        private void TransformChanged(GameObject sender, Transform e)
        {
            updateQueue.Enqueue(e);
        }

        public void Update(float dt)
        {
            if (updateQueue.IsEmpty)
            {
                return;
            }

            while (updateQueue.TryDequeue(out Transform? transform))
            {
                transform.Recalculate();
            }
        }

        public void ShiftWorldOrigin(Point3 newOrigin)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                // temporarily suppress update events.
                objects[i].TransformChanged -= TransformChanged;
                objects[i].Transform.Position += worldOrigin;
                objects[i].Transform.Position -= newOrigin;
                objects[i].Transform.Recalculate();
                objects[i].TransformChanged += TransformChanged;
            }
            worldOrigin = newOrigin;
        }

        public Vector3 ToAbsoluteWorldPosition(Vector3 vector)
        {
            return vector + worldOrigin;
        }
    }
}