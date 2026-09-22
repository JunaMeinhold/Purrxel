namespace VoxelEngine.Scenes
{
    public enum SystemFlags
    {
        None = 0,
        Awake = 1 << 0,
        Destroy = 1 << 1,
        EarlyUpdate = 1 << 2,
        Update = 1 << 3,
        LateUpdate = 1 << 4,
        FixedUpdate = 1 << 5,
        PhysicsUpdate = 1 << 6,
    }
}