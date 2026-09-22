namespace App.Scripts
{
    using App.Objects;
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Physics;
    using VoxelEngine.Scenes;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;

    public class PlayerController : ScriptComponent
    {
        public float Speed = 20F;
        public float AngluarSpeed = 20F;
        private bool leftDown;
        private bool rightDown;
        private bool midDown;
        private Camera camera;
        private CPlayer player;
        private World world;
        private Vector3 teleportLocation;
        private DynamicActorComponent actor;
        private bool freeCameraMode = true;
        private float verticalVelocity = 0f;
        private const float Gravity = -9.81f * 4;
        private const float JumpForce = 20.0f;

        public override void Awake()
        {
            player = (CPlayer)GameObject;
            player.Respawned += Player_Respawned;
            camera = Scene.Camera;

            Keyboard.KeyUp += Keyboard_OnKeyUp;
            world = Scene.Find<World>()!;

            var origin = player.Transform.GlobalPosition;
            origin.Y = 256;
            var result = PhysicsSystem.CastRay(origin, -Vector3.UnitY, float.MaxValue, world);
            actor = GameObject.GetComponent<DynamicActorComponent>()!;
        }

        public bool FreeCameraMode { get => freeCameraMode; set => freeCameraMode = value; }

        private void Keyboard_OnKeyUp(object? sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
        {
            if (e.KeyCode == Key.Escape)
            {
                Application.MainWindow.LockCursor = !Application.MainWindow.LockCursor;
            }

            if (e.KeyCode == Key.F1)
            {
                player.Gamemode = Gamemode.Survival;
            }

            if (e.KeyCode == Key.F2)
            {
                player.Gamemode = Gamemode.Creative;
            }
        }

        private void Player_Respawned(object? sender, EventArgs e)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void Update()
        {
            if (ImGui.Begin("Player"))
            {
                ImGui.InputFloat3("Location", ref teleportLocation);
                ImGui.SameLine();
                if (ImGui.Button("Teleport"))
                {
                    GameObject.Transform.GlobalPosition = teleportLocation;
                    world.WorldLoader.Reset();
                }
                ImGui.Text(player.SelectedBlock.Name);
                ImGui.InputFloat("Speed", ref Speed);
                ImGui.Checkbox("Free Camera", ref freeCameraMode);

                ImGui.InputFloat("Y Velocity", ref verticalVelocity);
            }
            ImGui.End();

            CameraTransform transform = camera.Transform;
            if (!freeCameraMode)
            {
                transform.Position = GameObject.Transform.Position + new Vector3(0, 1f, 0);
            }
            if (!Application.MainWindow.LockCursor)
            {
                return;
            }

            if (freeCameraMode)
            {
                HandleFreeCamera();
                var position = transform.Position - new Vector3(0, 1f, 0);
                GameObject.Transform.Position = position;
                actor.SetPosition(position);
            }
            else
            {
                HandleMovement();
            }

            var result = PhysicsSystem.CastRay(transform.Position, transform.Forward, 20, player.World);

            if (result.Hit)
            {
                Vector3 hitLocation = result.Position; //transform.Position + transform.Forward * result.T + transform.Forward * 0.01f;
                player.IsLookingAtBlock = true;
                player.LookAtBlock = new((int)Math.Floor(hitLocation.X), (int)Math.Floor(hitLocation.Y), (int)Math.Floor(hitLocation.Z));
            }
            else
            {
                player.IsLookingAtBlock = false;
            }

            if (!Mouse.IsDown(MouseButton.Left) & leftDown)
            {
                leftDown = false;
            }

            if (!Mouse.IsDown(MouseButton.Right) & rightDown)
            {
                rightDown = false;
            }
            if (!Mouse.IsDown(MouseButton.Middle) & midDown)
            {
                midDown = false;
            }

            if (Mouse.IsDown(MouseButton.Left) & !leftDown)
            {
                leftDown = true;
                if (player.IsLookingAtBlock)
                {
                    Scene.Dispatcher.Invoke(player, player =>
                    {
                        player.World.SetBlock(player.LookAtBlock, Block.Air);
                    });
                }
            }

            if (Mouse.IsDown(MouseButton.Right) & !rightDown)
            {
                rightDown = true;

                if (player.IsLookingAtBlock)
                {
                    Point3 hitLocation = (Point3)(result.Position + result.Normal);
                    player.World.SetBlock(hitLocation, player.SelectedBlock);
                }
            }

            if (Mouse.IsDown(MouseButton.Middle) & !midDown)
            {
                midDown = true;
                if (player.IsLookingAtBlock)
                {
                    Scene.Dispatcher.Invoke(player, player =>
                    {
                        player.SelectedBlockId = player.World.GetBlock(player.LookAtBlock).Type;
                    });
                }
            }

            if (Mouse.DeltaWheel.Y == 1)
            {
                int id = player.SelectedBlockId + 1;
                if (id > BlockRegistry.Blocks.Count)
                {
                    id = 1;
                }

                player.SelectedBlockId = id;
            }
            else if (Mouse.DeltaWheel.Y == -1)
            {
                int id = player.SelectedBlockId - 1;
                if (id == 0)
                {
                    id = BlockRegistry.Blocks.Count;
                }

                player.SelectedBlockId = id;
            }

            Vector2 delta = Mouse.Delta * 0.004f;

            if (delta.X != 0)
            {
                transform.Rotation += new Vector3(delta.X * AngluarSpeed, 0, 0);
            }

            if (delta.Y != 0)
            {
                transform.Rotation += new Vector3(0, delta.Y * AngluarSpeed, 0);
                if (transform.Rotation.Y < 269 & transform.Rotation.Y > 180)
                {
                    transform.Rotation = new Vector3(transform.Rotation.X, 269, transform.Rotation.Z);
                }

                if (transform.Rotation.Y > 89 & transform.Rotation.Y < 269)
                {
                    transform.Rotation = new Vector3(transform.Rotation.X, 89, transform.Rotation.Z);
                }
            }
        }

        private void HandleFreeCamera()
        {
            CameraTransform transform = camera.Transform;
            Vector3 direction = default;
            if (Keyboard.IsDown(Key.W))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    direction += transform.Forward * Speed * 2;
                }
                else
                {
                    direction += transform.Forward * Speed;
                }
            }

            if (Keyboard.IsDown(Key.S))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    direction += transform.Backward * Speed * 2;
                }
                else
                {
                    direction += transform.Backward * Speed;
                }
            }

            if (Keyboard.IsDown(Key.A))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    direction += transform.Left * Speed * 2;
                }
                else
                {
                    direction += transform.Left * Speed;
                }
            }

            if (Keyboard.IsDown(Key.D))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    direction += transform.Right * Speed * 2;
                }
                else
                {
                    direction += transform.Right * Speed;
                }
            }

            if (Keyboard.IsDown(Key.Space))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    direction += Vector3.UnitY * Speed * 2;
                }
                else
                {
                    direction += Vector3.UnitY * Speed;
                }
            }

            if (Keyboard.IsDown(Key.LShift))
            {
                direction += -Vector3.UnitY * Speed;
            }

            transform.Position += direction * Time.Delta;
        }

        private void HandleMovement()
        {
            CameraTransform transform = camera.Transform;
            Vector3 rotation = transform.Rotation;
            rotation.Y = 0;
            Quaternion quaternion = rotation.ToRad().ToQuaternion();
            Vector3 forward = Vector3.Transform(Vector3.UnitZ, quaternion);
            Vector3 right = Vector3.Transform(Vector3.UnitX, quaternion);
            Vector3 direction = Vector3.Zero;

            if (Keyboard.IsDown(Key.W))
            {
                direction += forward;
            }

            if (Keyboard.IsDown(Key.S))
            {
                direction += -forward;
            }

            if (Keyboard.IsDown(Key.A))
            {
                direction += -right;
            }

            if (Keyboard.IsDown(Key.D))
            {
                direction += right;
            }

            if (Keyboard.IsDown(Key.LCtrl))
            {
                direction *= Speed * 2;
            }
            else
            {
                direction *= Speed;
            }

            direction.Y = 0;

            verticalVelocity += Gravity * Time.Delta;

            if (actor.IsGrounded)
            {
                verticalVelocity = 0f;

                if (Keyboard.IsDown(Key.Space))
                {
                    verticalVelocity = JumpForce;
                }
            }

            direction.Y = verticalVelocity;

            actor.Move(GameObject.Transform.Position + direction * Time.Delta, 0.0001f);
        }

        public override void Destroy()
        {
        }
    }
}