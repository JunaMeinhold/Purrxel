namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using VoxelEngine.IO.ObjLoader.Common;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.Data.VertexData;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class TextureParser : TypeParserBase, ITextureParser
    {
        private readonly ITextureDataStore _textureDataStore;

        public TextureParser(ITextureDataStore textureDataStore)
        {
            _textureDataStore = textureDataStore;
        }

        protected override string Keyword => "vt";

        public override void Parse(string line)
        {
            string[] parts = line.Split(' ');

            float x = parts[0].ParseInvariantFloat();
            float y = parts[1].ParseInvariantFloat();

            var texture = new Texture(x, y);
            _textureDataStore.AddTexture(texture);
        }
    }
}