namespace Purrxel.Engine.Graphics.D3D11
{
    public interface ISamplerState : IDeviceChild
    {
        public new nint NativePointer { get; }
    }
}