namespace VoxelEngine.Core
{
    using System.Collections.Generic;
    using Hexa.NET.SDL2;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Core.Windows.Events;

    public static unsafe class Application
    {
        private static bool initialized = false;
        private static bool exiting = false;

        private static readonly Dictionary<uint, CoreWindow> windowIdToWindow = new();
        private static readonly List<CoreWindow> windows = new();
        private static readonly List<Func<SDLEvent, bool>> hooks = new();
        private static CoreWindow mainWindow;

#nullable disable
        public static CoreWindow MainWindow => mainWindow;
#nullable enable

        public enum SpecialFolder
        {
            Assets,
            Shaders,
            Sounds,
            Models,
            Textures,
            Scenes,
        }

        public static string GetFolder(SpecialFolder folder)
        {
            return folder switch
            {
                SpecialFolder.Assets => Path.GetFullPath("assets/"),
                SpecialFolder.Shaders => Path.GetFullPath("assets/shaders/"),
                SpecialFolder.Sounds => Path.GetFullPath("assets/sounds/"),
                SpecialFolder.Models => Path.GetFullPath("assets/models/"),
                SpecialFolder.Textures => Path.GetFullPath("assets/textures/"),
                SpecialFolder.Scenes => Path.GetFullPath("assets/scenes/"),
                _ => throw new ArgumentOutOfRangeException(nameof(folder)),
            };
        }

        public static void Boot()
        {
            SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SetHint(SDL.SDL_HINT_AUTO_UPDATE_JOYSTICKS, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_RAWINPUT, "0");
            SDL.Init(SDL.SDL_INIT_EVENTS + SDL.SDL_INIT_GAMECONTROLLER + SDL.SDL_INIT_HAPTIC + SDL.SDL_INIT_JOYSTICK);

            SdlCheckError();

            Keyboard.Init();
            SdlCheckError();
            Mouse.Init();
            SdlCheckError();
            Gamepads.Init();
            SdlCheckError();
            TouchDevices.Init();
            SdlCheckError();
        }

        public static void Run(CoreWindow mainWindow)
        {
            Application.mainWindow = mainWindow;

            mainWindow.Show();
            Init();
            mainWindow.Closing += MainWindowClosing;

            PlatformRun();
        }

        /// <summary>
        /// Registers a window to the application.
        /// </summary>
        /// <param name="window">The window to register.</param>
        internal static void RegisterWindow(CoreWindow window)
        {
            windows.Add(window);
            windowIdToWindow.Add(window.WindowID, window);
            if (initialized)
            {
                window.RendererCreate();
            }
        }

        private static void MainWindowClosing(object? sender, CloseEventArgs e)
        {
            if (!e.Handled)
            {
                exiting = true;
            }
        }

        public static void RegisterHook(Func<SDLEvent, bool> hook)
        {
            hooks.Add(hook);
        }

        public static void UnregisterHook(Func<SDLEvent, bool> hook)
        {
            hooks.Remove(hook);
        }

        private static void Init()
        {
            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].RendererCreate();
            }

            initialized = true;
        }

        private static void PlatformRun()
        {
            SDLEvent evnt;
            Time.Initialize();

            while (!exiting)
            {
                SDL.PumpEvents();
                while (SDL.PollEvent(&evnt) == (int)SDLBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }
                    SDLEventType type = (SDLEventType)evnt.Type;
                    switch (type)
                    {
                        case SDLEventType.Firstevent:
                            break;

                        case SDLEventType.Quit:
                            exiting = true;
                            break;

                        case SDLEventType.AppTerminating:
                            exiting = true;
                            break;

                        case SDLEventType.AppLowmemory:
                            break;

                        case SDLEventType.AppWillenterbackground:
                            break;

                        case SDLEventType.AppDidenterbackground:
                            break;

                        case SDLEventType.AppWillenterforeground:
                            break;

                        case SDLEventType.AppDidenterforeground:
                            break;

                        case SDLEventType.Localechanged:
                            break;

                        case SDLEventType.Displayevent:
                            break;

                        case SDLEventType.Windowevent:
                            {
                                var even = evnt.Window;
                                if (even.WindowID == mainWindow.WindowID)
                                {
                                    mainWindow.ProcessEvent(even);
                                    if ((SDLWindowEventID)evnt.Window.Event == SDLWindowEventID.Close)
                                    {
                                        exiting = true;
                                    }
                                }
                            }

                            break;

                        case SDLEventType.Syswmevent:
                            break;

                        case SDLEventType.Keydown:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputKeyboard(even);
                            }
                            break;

                        case SDLEventType.Keyup:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputKeyboard(even);
                            }
                            break;

                        case SDLEventType.Textediting:
                            break;

                        case SDLEventType.Textinput:
                            {
                                var even = evnt.Text;
                                Keyboard.OnTextInput(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputText(even);
                            }
                            break;

                        case SDLEventType.Keymapchanged:
                            break;

                        case SDLEventType.Mousemotion:
                            {
                                var even = evnt.Motion;
                                Mouse.OnMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousebuttondown:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousebuttonup:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousewheel:
                            {
                                var even = evnt.Wheel;
                                Mouse.OnWheel(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Joyaxismotion:
                            {
                                var even = evnt.Jaxis;
                                Joysticks.OnAxisMotion(even);
                            }
                            break;

                        case SDLEventType.Joyballmotion:
                            {
                                var even = evnt.Jball;
                                Joysticks.OnBallMotion(even);
                            }
                            break;

                        case SDLEventType.Joyhatmotion:
                            {
                                var even = evnt.Jhat;
                                Joysticks.OnHatMotion(even);
                            }
                            break;

                        case SDLEventType.Joybuttondown:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonDown(even);
                            }
                            break;

                        case SDLEventType.Joybuttonup:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonUp(even);
                            }
                            break;

                        case SDLEventType.Joydeviceadded:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.AddJoystick(even);
                            }
                            break;

                        case SDLEventType.Joydeviceremoved:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.RemoveJoystick(even);
                            }
                            break;

                        case SDLEventType.Controlleraxismotion:
                            {
                                var even = evnt.Caxis;
                                Gamepads.AxisMotion(even);
                            }
                            break;

                        case SDLEventType.Controllerbuttondown:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.ButtonDown(even);
                            }
                            break;

                        case SDLEventType.Controllerbuttonup:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.ButtonUp(even);
                            }
                            break;

                        case SDLEventType.Controllerdeviceadded:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.AddController(even);
                            }
                            break;

                        case SDLEventType.Controllerdeviceremoved:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.RemoveController(even);
                            }
                            break;

                        case SDLEventType.Controllerdeviceremapped:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.Remapped(even);
                            }
                            break;

                        case SDLEventType.Controllertouchpaddown:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadDown(even);
                            }
                            break;

                        case SDLEventType.Controllertouchpadmotion:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadMotion(even);
                            }
                            break;

                        case SDLEventType.Controllertouchpadup:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadUp(even);
                            }
                            break;

                        case SDLEventType.Controllersensorupdate:
                            {
                                var even = evnt.Csensor;
                                Gamepads.SensorUpdate(even);
                            }
                            break;

                        case SDLEventType.Fingerdown:
                            break;

                        case SDLEventType.Fingerup:
                            break;

                        case SDLEventType.Fingermotion:
                            break;

                        case SDLEventType.Dollargesture:
                            break;

                        case SDLEventType.Dollarrecord:
                            break;

                        case SDLEventType.Multigesture:
                            break;

                        case SDLEventType.Clipboardupdate:
                            break;

                        case SDLEventType.Dropfile:
                            break;

                        case SDLEventType.Droptext:
                            break;

                        case SDLEventType.Dropbegin:
                            break;

                        case SDLEventType.Dropcomplete:
                            break;

                        case SDLEventType.Audiodeviceadded:
                            break;

                        case SDLEventType.Audiodeviceremoved:
                            break;

                        case SDLEventType.Sensorupdate:
                            break;

                        case SDLEventType.RenderTargetsReset:
                            break;

                        case SDLEventType.RenderDeviceReset:
                            break;

                        case SDLEventType.Userevent:
                            break;

                        case SDLEventType.Lastevent:
                            break;
                    }
                }

                mainWindow.Render();
                Keyboard.Flush();
                Mouse.Flush();
                Time.FrameUpdate();
            }

            mainWindow.RendererDestroy();

            SDL.Quit();
        }
    }
}