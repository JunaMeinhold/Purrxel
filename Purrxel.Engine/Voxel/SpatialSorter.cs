namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Collections.Generic;

    public readonly unsafe struct SpatialSorter : IComparer<Point2>
    {
        public static readonly SpatialSorter Default;

        public int Compare(Point2 x, Point2 y)
        {
            float da = Point2.Distance(Point2.Zero, x);
            float db = Point2.Distance(Point2.Zero, y);

            if (da < db)
            {
                return -1;
            }
            else if (db < da)
            {
                return 1;
            }

            return 0;
        }
    }
}