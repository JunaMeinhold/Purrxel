namespace VoxelEngine.UI
{
    using System.Numerics;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Backends.D3D11;
    using Hexa.NET.ImGui.Backends.SDL2;
    using Hexa.NET.ImGui.Utilities;
    using Hexa.NET.ImGuizmo;
    using Hexa.NET.ImNodes;
    using Hexa.NET.ImPlot;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Windows;
    using SDLWindow = Hexa.NET.SDL2.SDLWindow;
    using SDLEvent = Hexa.NET.SDL2.SDLEvent;
    using ID3D11Device = Hexa.NET.D3D11.ID3D11Device;
    using ID3D11DeviceContext = Hexa.NET.D3D11.ID3D11DeviceContext;
    using HexaGen.Runtime.COM;

    public class ImGuiManager
    {
        private ImGuiContextPtr guiContext;
        private ImNodesContextPtr nodesContext;
        private ImPlotContextPtr plotContext;

        private bool disposedValue;

        public unsafe ImGuiManager(CoreWindow window, ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context, ImGuiConfigFlags flags = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad | ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable)
        {
            guiContext = ImGui.CreateContext(null);
            ImGui.SetCurrentContext(guiContext);

            ImGui.SetCurrentContext(guiContext);
            ImGuizmo.SetImGuiContext(guiContext);
            ImPlot.SetImGuiContext(guiContext);
            ImNodes.SetImGuiContext(guiContext);

            nodesContext = ImNodes.CreateContext();
            ImNodes.SetCurrentContext(nodesContext);
            ImNodes.StyleColorsDark(ImNodes.GetStyle());

            plotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(plotContext);
            ImPlot.StyleColorsDark(ImPlot.GetStyle());

            var io = ImGui.GetIO();
            io.ConfigFlags |= flags;
            io.ConfigViewportsNoAutoMerge = false;
            io.ConfigViewportsNoTaskBarIcon = false;
            io.ConfigErrorRecoveryEnableAssert = false;
            io.ConfigErrorRecoveryEnableTooltip = true;
            

            uint[] range = [0xE700, 0xF800, 0];

            ImGuiFontBuilder builder = new();
            builder.AddDefaultFont();
            builder.SetOption(config => { config.GlyphMinAdvanceX = 18; config.GlyphOffset = new(0, 4); });
            builder.AddFontFromFileTTF("C:\\windows\\fonts\\SegoeIcons.ttf", 14, range);

            SDLWindow* windowPtr = window.GetWindow();

            ImGuiImplSDL2.SetCurrentContext(guiContext);
            ImGuiImplSDL2.InitForD3D((Hexa.NET.ImGui.Backends.SDL2.SDLWindow*)windowPtr);

            ImGuiImplD3D11.SetCurrentContext(guiContext);
            ImGuiImplD3D11.Init(new((Hexa.NET.ImGui.Backends.D3D11.ID3D11Device*)device.Handle), new((Hexa.NET.ImGui.Backends.D3D11.ID3D11DeviceContext*)context.Handle));
            ImGuiImplD3D11.NewFrame();

            Application.RegisterHook(MessageHook);
        }

        private static unsafe bool MessageHook(SDLEvent @event)
        {
            return ImGuiImplSDL2.ProcessEvent((Hexa.NET.ImGui.Backends.SDL2.SDLEvent*)&@event);
        }

        public unsafe void NewFrame()
        {
            ImGui.SetCurrentContext(guiContext);
            ImGuizmo.SetImGuiContext(guiContext);
            ImPlot.SetImGuiContext(guiContext);
            ImNodes.SetImGuiContext(guiContext);

            ImNodes.SetCurrentContext(nodesContext);
            ImPlot.SetCurrentContext(plotContext);

            ImGuiImplSDL2.NewFrame();
            ImGuiImplD3D11.NewFrame();
            ImGui.NewFrame();
            ImGuizmo.BeginFrame();

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            DockSpaceId = ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null);
            ImGui.PopStyleColor(1);
        }

        public static uint DockSpaceId { get; private set; }

        public unsafe void EndFrame()
        {
            var io = ImGui.GetIO();
            ImGui.Render();
            ImGui.EndFrame();
            ImGuiImplD3D11.RenderDrawData(ImGui.GetDrawData());

            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }
        }

        public void Dispose()
        {
            if (disposedValue)
            {
                return;
            }

            Application.UnregisterHook(MessageHook);

            ImGuiImplD3D11.Shutdown();
            ImGuiImplSDL2.Shutdown();

            ImGuiImplSDL2.SetCurrentContext(null);
            ImGuiImplD3D11.SetCurrentContext(null);

            ImNodes.DestroyContext(nodesContext);
            ImNodes.SetCurrentContext(null);
            ImPlot.DestroyContext(plotContext);
            ImPlot.SetCurrentContext(null);

            ImGuizmo.SetImGuiContext(null);
            ImPlot.SetImGuiContext(null);
            ImNodes.SetImGuiContext(null);

            ImGui.DestroyContext(guiContext);

            ImGui.SetCurrentContext(null);
            disposedValue = true;
        }
    }
}