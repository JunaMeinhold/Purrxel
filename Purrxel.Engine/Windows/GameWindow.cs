namespace Purrxel.Engine.Windows
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DebugDraw;
    using Purrxel.Engine.Core;
    using Purrxel.Engine.Core.Input;
    using Purrxel.Engine.Core.Input.Events;
    using Purrxel.Engine.Core.Windows;
    using Purrxel.Engine.Core.Windows.Events;
    using Purrxel.Engine.Debugging;
    using Purrxel.Engine.Graphics.D3D11;
    using Purrxel.Engine.Scenes;
    using Purrxel.Engine.UI;
    using System.Diagnostics;

    public class GameWindow : CoreWindow
    {
        private bool firstFrame;
        private SwapChain swapChain;

        private readonly Scene scene;
        private Dispatcher renderDispatcher;
        private ImGuiManager renderer;
        private DebugDrawD3D11Renderer debugDraw;
        private bool resize;

        public GameWindow(Scene scene) : base(1280, 720)
        {
            this.scene = scene;
        }

        public Dispatcher RenderDispatcher => renderDispatcher;

        public SwapChain SwapChain => swapChain;

        protected override void OnShown(ShownEventArgs args)
        {
            base.OnShown(args);
        }

        public override void RendererCreate()
        {
            DXGIDeviceManager.Initialize();

            swapChain = DXGIDeviceManager.CreateSwapChain(this);

            SceneRenderer.Initialize(this);
            renderDispatcher = Dispatcher.CurrentDispatcher;
            renderer = new(this, D3D11DeviceManager.Device.As<ID3D11Device>(), D3D11DeviceManager.Context.As<ID3D11DeviceContext>());
            debugDraw = new(D3D11DeviceManager.Device.As<ID3D11Device>(), D3D11DeviceManager.Context.As<ID3D11DeviceContext>());
            SceneManager.SceneChanged += (_, _) => { firstFrame = true; };
            SceneManager.Load(scene);
        }

        public ISceneRenderer SceneRenderer { get; set; }

        public override void Render()
        {
            if (resize)
            {
                swapChain.Resize(Width, Height);
                SceneRenderer.Resize(this);
                resize = false;
            }
            debugDraw.BeginDraw();
            renderer.NewFrame();

            Dispatcher.ExecuteQueue();
            lock (SceneManager.Lock)
            {
                var scene = SceneManager.Current;
                if (scene != null)
                {
                    if (firstFrame)
                    {
                        Time.Initialize();
                        firstFrame = false;
                    }
                    scene.Tick();
                    SceneRenderer.Render(D3D11DeviceManager.GraphicsContext, scene.Camera, scene);
                }
            }

            ImGuiConsole.Draw();

            swapChain.SetTarget(D3D11DeviceManager.GraphicsContext, false);
            DebugDraw.SetViewport(default, new(Width, Height));
            debugDraw.EndDraw(swapChain.RTV, swapChain.DSV);
            renderer.EndFrame();

            swapChain.Present(Config.Default.VSync ? 1u : 0u);
        }

        public override void RendererDestroy()
        {
            SceneRenderer.Dispose();
            renderer.Dispose();
            SceneManager.Unload();
            debugDraw.Dispose();
            swapChain.Dispose();
            DXGIDeviceManager.Dispose();
        }

        private long fpsFrameCount;
        private long fpsStartTime;

        private void LimitFrameRate()
        {
            if (Config.Default.LimitFPS & !Config.Default.VSync)
            {
                int fps = Config.Default.TargetFPS;
                long freq = Stopwatch.Frequency;
                long frame = Stopwatch.GetTimestamp();
                while ((frame - fpsStartTime) * fps < freq * fpsFrameCount)
                {
                    int sleepTime = (int)((fpsStartTime * fps + freq * fpsFrameCount - frame * fps) * 1000 / (freq * fps));
                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    frame = Stopwatch.GetTimestamp();
                }
                if (++fpsFrameCount > fps)
                {
                    fpsFrameCount = 0;
                    fpsStartTime = frame;
                }
            }
        }

        protected override void OnResized(ResizedEventArgs args)
        {
            base.OnResized(args);
            resize = true;
        }

        protected override void OnClose(CloseEventArgs args)
        {
            Trace.WriteLine("Perfoming Shutdown");
            base.OnClose(args);
        }

        protected override void OnKeyboardInput(KeyboardEventArgs args)
        {
            base.OnKeyboardInput(args);
            if (args.KeyCode == Key.F5)
            {
                renderDispatcher.Invoke(GraphicsPipeline.ReloadShaders);
            }
            if (args.KeyCode == Key.F9)
            {
                Trace.WriteLine(Process.GetCurrentProcess().PrivateMemorySize64);
            }
        }
    }
}