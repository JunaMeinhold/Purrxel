namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel.Blocks;

    public class Player : GameObject
    {
        private int hp;
        private bool isAlive = true;
        private Vector3 spawnpoint;
        private Gamemode gamemode;

        public Player(Vector3 spawnpoint)
        {
            Name = nameof(Player);

            this.spawnpoint = spawnpoint;
            Respawn();
        }

        public override void Awake()
        {
            World = Scene.Find<World>()!;
            base.Awake();
        }

        public World World { get; private set; }

        public bool FreeCamera { get; set; }

        public Gamemode Gamemode
        {
            get => gamemode;
            set
            {
                gamemode = value;
                FreeCamera = value == Gamemode.Creative;
                IsLookingAtBlock = false;
                GamemodeChanged?.Invoke(this, value);
            }
        }

        public Point3 LookAtBlock { get; set; }

        public bool IsLookingAtBlock { set; get; }

        public int SelectedBlockId = 1;

        public BlockEntry SelectedBlock => BlockRegistry.GetBlockById(SelectedBlockId);

        public bool IsAlive
        {
            get => isAlive;
            private set
            {
                isAlive = value;
                IsAliveChanged?.Invoke(this, value);
            }
        }

        // Will be later extended;
        public Vector3 Spawnpoint { get => spawnpoint; set => spawnpoint = value; }

        public int HP => hp;

        public event EventHandler<Gamemode> GamemodeChanged;

        public event EventHandler<bool> IsAliveChanged;

        public event EventHandler Respawned;

        public void TakeDamage(int damage)
        {
            if (gamemode == Gamemode.Creative)
            {
                return;
            }
            hp -= damage;
            if (hp <= 0)
            {
                hp = 0;
                IsAlive = false;
                Respawn();
            }
        }

        public void Respawn()
        {
            Transform.Position = spawnpoint;
            hp = 100;
            IsAlive = true;
            Respawned?.Invoke(this, null);
        }
    }
}