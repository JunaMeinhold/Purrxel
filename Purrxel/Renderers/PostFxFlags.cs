namespace App.Renderers
{
    [Flags]
    public enum PostFxFlags
    {
        None = 0,
        NoInput = 1 << 0,
        NoOutput = 1 << 1,
        PreDraw = 1 << 2,
    }
}