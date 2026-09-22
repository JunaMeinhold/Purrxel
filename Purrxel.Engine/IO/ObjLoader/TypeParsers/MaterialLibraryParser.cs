namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using VoxelEngine.IO.ObjLoader.Loaders;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class MaterialLibraryParser : TypeParserBase, IMaterialLibraryParser
    {
        private readonly IMaterialLibraryLoaderFacade _libraryLoaderFacade;

        public MaterialLibraryParser(IMaterialLibraryLoaderFacade libraryLoaderFacade)
        {
            _libraryLoaderFacade = libraryLoaderFacade;
        }

        protected override string Keyword => "mtllib";

        public override void Parse(string line)
        {
            _libraryLoaderFacade.Load(line);
        }
    }
}