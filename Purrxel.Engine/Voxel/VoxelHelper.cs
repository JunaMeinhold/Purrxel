namespace VoxelEngine.Voxel
{
    using System;
    using System.Numerics;

    public struct VoxelHelper
    {
        private Matrix4x4 inverseTransformationMatrix;

        public VoxelHelper(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            TransformationMatrix = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
            _ = Matrix4x4.Invert(TransformationMatrix, out inverseTransformationMatrix);
        }

        public VoxelHelper(Matrix4x4 transformationMatrix)
        {
            TransformationMatrix = transformationMatrix;
            _ = Matrix4x4.Invert(TransformationMatrix, out inverseTransformationMatrix);
        }

        public Matrix4x4 TransformationMatrix { get; private set; }

        public Matrix4x4 InverseTransformationMatrix { get => inverseTransformationMatrix; private set => inverseTransformationMatrix = value; }

        public Vector3 WorldToVoxel(Vector3 worldPosition)
        {
            return Vector3.Transform(worldPosition, inverseTransformationMatrix);
        }

        public Vector3 WorldToVoxelCoordinate(Vector3 worldPosition)
        {
            Vector3 voxelPosition = WorldToVoxel(worldPosition);
            return new Vector3(MathF.Round(voxelPosition.X), MathF.Round(voxelPosition.Y), MathF.Round(voxelPosition.Z));
        }

        public Vector3 VoxelToWorld(Vector3 voxelPosition)
        {
            return Vector3.Transform(voxelPosition, TransformationMatrix);
        }
    }
}