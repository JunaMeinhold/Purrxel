namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using System;
    using VoxelEngine.IO.ObjLoader.Common;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.Data.VertexData;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class VertexParser : TypeParserBase, IVertexParser
    {
        private readonly IVertexDataStore _vertexDataStore;

        public VertexParser(IVertexDataStore vertexDataStore)
        {
            _vertexDataStore = vertexDataStore;
        }

        protected override string Keyword => "v";

        public override void Parse(string line)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var x = parts[0].ParseInvariantFloat();
            var y = parts[1].ParseInvariantFloat();
            var z = parts[2].ParseInvariantFloat();

            var vertex = new Vertex(x, y, z);
            _vertexDataStore.AddVertex(vertex);
        }
    }
}