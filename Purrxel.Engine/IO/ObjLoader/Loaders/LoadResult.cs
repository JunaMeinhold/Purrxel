namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.Collections.Generic;
    using System.Numerics;
    using Hexa.NET.D3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.IO.ObjLoader.Data;
    using VoxelEngine.IO.ObjLoader.Data.Elements;
    using VoxelEngine.IO.ObjLoader.Data.VertexData;

    public class LoadResult
    {
        public IList<Vertex> Vertices { get; set; }
        public IList<Texture> Textures { get; set; }
        public IList<Normal> Normals { get; set; }
        public IList<Group> Groups { get; set; }
        public IList<Material> Materials { get; set; }

        public VertexBuffer<Mathematics.Vertex> GetVertexBuffer(ID3D11DeviceContext context, int group = 0)
        {
            List<Mathematics.Vertex> vertices = new();

            for (int j = 0; j < Groups[group].Faces.Count; j++)
            {
                int vertexIndex1 = Groups[group].Faces[j][0].VertexIndex - 1;
                int textureIndex1 = Groups[group].Faces[j][0].TextureIndex - 1;
                int normalIndex1 = Groups[group].Faces[j][0].NormalIndex - 1;
                Mathematics.Vertex vertex1 = new(Vertices[vertexIndex1], Textures[textureIndex1], normalIndex1 == -1 ? Vector3.Zero : Normals[normalIndex1]);
                int vertexIndex2 = Groups[group].Faces[j][1].VertexIndex - 1;
                int textureIndex2 = Groups[group].Faces[j][1].TextureIndex - 1;
                int normalIndex2 = Groups[group].Faces[j][1].NormalIndex - 1;
                Mathematics.Vertex vertex2 = new(Vertices[vertexIndex2], Textures[textureIndex2], normalIndex2 == -1 ? Vector3.Zero : Normals[normalIndex2]);
                int vertexIndex3 = Groups[group].Faces[j][2].VertexIndex - 1;
                int textureIndex3 = Groups[group].Faces[j][2].TextureIndex - 1;
                int normalIndex3 = Groups[group].Faces[j][2].NormalIndex - 1;
                Mathematics.Vertex vertex3 = new(Vertices[vertexIndex3], Textures[textureIndex3], normalIndex3 == -1 ? Vector3.Zero : Normals[normalIndex3]);

                vertex1.InvertTexture();
                vertex2.InvertTexture();
                vertex3.InvertTexture();
                Mathematics.Face.ComputeTangent(vertex1, vertex2, vertex3, out Vector3 tangent);
                vertex1.Tangent = vertex2.Tangent = vertex3.Tangent = tangent;
                vertices.Add(vertex1);
                vertices.Add(vertex2);
                vertices.Add(vertex3);
            }
            VertexBuffer<Mathematics.Vertex> buffer = new(0, [.. vertices]);
            return buffer;
        }

        public IEnumerable<Tuple<VertexBuffer<Mathematics.Vertex>, Objects.Material>> GetGroups(ID3D11DeviceContext context)
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                yield return new(GetVertexBuffer(context, i), (Objects.Material)Groups[i].Material);
            }
        }

        public IEnumerable<Mathematics.Vertex> GetVertices()
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                for (int j = 0; j < Groups[i].Faces.Count; j++)
                {
                    int vertexIndex1 = Groups[i].Faces[j][0].VertexIndex - 1;
                    int textureIndex1 = Groups[i].Faces[j][0].TextureIndex - 1;
                    int normalIndex1 = Groups[i].Faces[j][0].NormalIndex - 1;
                    Mathematics.Vertex vertex1 = new(Vertices[vertexIndex1], Textures[textureIndex1], normalIndex1 == -1 ? Vector3.Zero : Normals[normalIndex1]);
                    int vertexIndex2 = Groups[i].Faces[j][1].VertexIndex - 1;
                    int textureIndex2 = Groups[i].Faces[j][1].TextureIndex - 1;
                    int normalIndex2 = Groups[i].Faces[j][1].NormalIndex - 1;
                    Mathematics.Vertex vertex2 = new(Vertices[vertexIndex2], Textures[textureIndex2], normalIndex2 == -1 ? Vector3.Zero : Normals[normalIndex2]);
                    int vertexIndex3 = Groups[i].Faces[j][2].VertexIndex - 1;
                    int textureIndex3 = Groups[i].Faces[j][2].TextureIndex - 1;
                    int normalIndex3 = Groups[i].Faces[j][2].NormalIndex - 1;
                    Mathematics.Vertex vertex3 = new(Vertices[vertexIndex3], Textures[textureIndex3], normalIndex3 == -1 ? Vector3.Zero : Normals[normalIndex3]);

                    vertex1.InvertTexture();
                    vertex2.InvertTexture();
                    vertex3.InvertTexture();
                    Mathematics.Face.ComputeTangent(vertex1, vertex2, vertex3, out Vector3 tangent);
                    vertex1.Tangent = vertex2.Tangent = vertex3.Tangent = tangent;
                    yield return vertex1;
                    yield return vertex2;
                    yield return vertex3;
                }
            }
        }
    }
}