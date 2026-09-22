namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class GroupParser : TypeParserBase, IGroupParser
    {
        private readonly IGroupDataStore _groupDataStore;

        public GroupParser(IGroupDataStore groupDataStore)
        {
            _groupDataStore = groupDataStore;
        }

        protected override string Keyword => "g";

        public override void Parse(string line)
        {
            _groupDataStore.PushGroup(line);
        }
    }
}