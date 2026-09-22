namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class UseMaterialParser : TypeParserBase, IUseMaterialParser
    {
        private readonly IElementGroup _elementGroup;

        public UseMaterialParser(IElementGroup elementGroup)
        {
            _elementGroup = elementGroup;
        }

        protected override string Keyword => "usemtl";

        public override void Parse(string line)
        {
            _elementGroup.SetMaterial(line);
        }
    }
}