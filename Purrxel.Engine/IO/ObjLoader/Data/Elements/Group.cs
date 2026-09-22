namespace VoxelEngine.IO.ObjLoader.Data.Elements
{
    using System.Collections.Generic;
    using VoxelEngine.IO.ObjLoader.Data;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;

    public class Group : IFaceGroup
    {
        private readonly List<Face> _faces = new();

        public Group(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public Material Material { get; set; }

        public IList<Face> Faces => _faces;

        public void AddFace(Face face)
        {
            _faces.Add(face);
        }
    }
}