namespace VoxelEngine.Mathematics
{
    using System.Numerics;

    public struct Face
    {
        public Vertex Vertex1 { get; set; }

        public Vertex Vertex2 { get; set; }

        public Vertex Vertex3 { get; set; }

        public static void ComputeTangent(Vertex vertex1, Vertex vertex2, Vertex vertex3, out Vector3 tangent)
        {
            // Calculate the two vectors for the this face.
            Vector3 vecPd1 = new(vertex2.Position.X - vertex1.Position.X, vertex2.Position.Y - vertex1.Position.Y, vertex2.Position.Z - vertex1.Position.Z);
            Vector3 vecPd2 = new(vertex3.Position.X - vertex1.Position.X, vertex3.Position.Y - vertex1.Position.Y, vertex3.Position.Z - vertex1.Position.Z);

            // Calculate the tu and tv texture space vectors.
            Vector2 vecTd1 = new(vertex2.Texture.X - vertex1.Texture.X, vertex3.Texture.X - vertex1.Texture.X);
            Vector2 vecTd2 = new(vertex2.Texture.Y - vertex1.Texture.Y, vertex3.Texture.Y - vertex1.Texture.Y);

            // Calculate the denominator of the tangent / binormal equation.
            float den = 1.0f / (vecTd1.X * vecTd2.Y - vecTd1.Y * vecTd2.X);

            // Calculate the cross products and multiply by the coefficient to get the tangent and binormal.
            tangent.X = (vecTd2.Y * vecPd1.X - vecTd2.X * vecPd2.X) * den;
            tangent.Y = (vecTd2.Y * vecPd1.Y - vecTd2.X * vecPd2.Y) * den;
            tangent.Z = (vecTd2.Y * vecPd1.Z - vecTd2.X * vecPd2.Z) * den;

            // Normalize the normal and the store it.
            tangent = Vector3.Normalize(tangent);
        }
    }
}