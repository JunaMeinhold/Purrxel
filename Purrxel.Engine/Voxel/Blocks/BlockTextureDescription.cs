namespace VoxelEngine.Voxel.Blocks
{
    public struct BlockTextureDescription
    {
        public string Top;
        public string Bottom;
        public string Left;
        public string Right;
        public string Front;
        public string Back;
        public string Single;
        public bool IsSingle;

        public BlockTextureDescription(string top, string bottom, string left, string right, string front, string back) : this()
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Front = front;
            Back = back;
        }

        public BlockTextureDescription(string top, string bottom, string side) : this()
        {
            Top = top;
            Bottom = bottom;
            Left = side;
            Right = side;
            Front = side;
            Back = side;
        }

        public BlockTextureDescription(string texture) : this()
        {
            Single = texture;
            IsSingle = true;
        }

        public string[] ToArray()
        {
            return new[] { Right, Left, Top, Bottom, Front, Back };
        }
    }
}