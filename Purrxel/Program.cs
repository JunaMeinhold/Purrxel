// See https://aka.ms/new-console-template for more information
using Purrxel;
using Purrxel.Engine.Core;
using Purrxel.Engine.Debugging;
using Purrxel.Engine.Windows;
using Purrxel.Renderers;

Logger.Initialize();
Application.Boot();
Application.Run(new GameWindow(MainScene.Create()) { SceneRenderer = new SceneRenderer() });