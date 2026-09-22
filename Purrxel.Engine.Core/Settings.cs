namespace VoxelEngine.Core
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class Config
    {
        public bool VSync = true;

        public bool LimitFPS = false;

        public int TargetFPS = 120;

        public int ShadowMapSize = 1024 * 2;

        public bool ShaderCache = false;

        public int ChunkRenderDistance { get; set; } = 16;

        public int RenderRegionSize { get; set; } = 4;

        public int ChunkSimulationDistance { get; set; } = 8;

        [JsonIgnore]
        public int BufferCount = 2;

        internal void Save()
        {
            File.WriteAllText("config.json", JsonSerializer.Serialize(this, ConfigSourceGenerationContext.Default.Config));
        }

        static Config()
        {
            if (File.Exists("config.json"))
            {
                Default = JsonSerializer.Deserialize(File.ReadAllText("config.json"), ConfigSourceGenerationContext.Default.Config)!;
            }
            else
            {
                Default = new();
                Default.Save();
            }
        }

        public static Config Default { get; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Config))]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext
    {
    }
}