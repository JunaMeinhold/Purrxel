namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using VoxelEngine.IO.ObjLoader.Common;
    using VoxelEngine.IO.ObjLoader.Data;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;

    public class MaterialLibraryLoader : LoaderBase, IMaterialLibraryLoader
    {
        private readonly IMaterialLibrary _materialLibrary;
        private Material _currentMaterial;

        private readonly Dictionary<string, Action<string>> _parseActionDictionary = new();
        private readonly List<string> _unrecognizedLines = new();

        public MaterialLibraryLoader(IMaterialLibrary materialLibrary)
        {
            _materialLibrary = materialLibrary;

            AddParseAction("newmtl", PushMaterial);
            AddParseAction("Ka", d => CurrentMaterial.AmbientColor = ParseVec3(d));
            AddParseAction("Kd", d => CurrentMaterial.DiffuseColor = ParseVec3(d));
            AddParseAction("Ks", d => CurrentMaterial.SpecularColor = ParseVec3(d));
            AddParseAction("Ns", d => CurrentMaterial.SpecularCoefficient = d.ParseInvariantFloat());

            AddParseAction("d", d => CurrentMaterial.Transparency = d.ParseInvariantFloat());
            AddParseAction("Tr", d => CurrentMaterial.Transparency = d.ParseInvariantFloat());

            AddParseAction("illum", i => CurrentMaterial.IlluminationModel = i.ParseInvariantInt());

            AddParseAction("map_Ka", m => CurrentMaterial.AmbientTextureMap = m);
            AddParseAction("map_Kd", m => CurrentMaterial.DiffuseTextureMap = m);

            AddParseAction("map_Ks", m => CurrentMaterial.SpecularTextureMap = m);
            AddParseAction("map_Ns", m => CurrentMaterial.SpecularHighlightTextureMap = m);

            AddParseAction("map_d", m => CurrentMaterial.AlphaTextureMap = m);

            AddParseAction("map_bump", m => CurrentMaterial.BumpMap = m);
            AddParseAction("bump", m => CurrentMaterial.BumpMap = m);

            AddParseAction("disp", m => CurrentMaterial.DisplacementMap = m);

            AddParseAction("decal", m => CurrentMaterial.StencilDecalMap = m);

            AddParseAction("map_Pr", m => CurrentMaterial.RoughnessTextureMap = m);

            AddParseAction("map_Pm", m => CurrentMaterial.MetallicTextureMap = m);
        }

        private Material CurrentMaterial => _currentMaterial;

        private void AddParseAction(string key, Action<string> action)
        {
            _parseActionDictionary.Add(key.ToLowerInvariant(), action);
        }

        protected override void ParseLine(string keyword, string data)
        {
            var parseAction = GetKeywordAction(keyword);

            if (parseAction == null)
            {
                _unrecognizedLines.Add(keyword + " " + data);
                return;
            }

            parseAction(data);
        }

        private Action<string> GetKeywordAction(string keyword)
        {
            _ = _parseActionDictionary.TryGetValue(keyword.ToLowerInvariant(), out Action<string> action);

            return action;
        }

        private void PushMaterial(string materialName)
        {
            _currentMaterial = new Material(materialName);
            _materialLibrary.Push(_currentMaterial);
        }

        private static Vector3 ParseVec3(string data)
        {
            string[] parts = data.Split(' ');

            float x = parts[0].ParseInvariantFloat();
            float y = parts[1].ParseInvariantFloat();
            float z = parts[2].ParseInvariantFloat();

            return new Vector3(x, y, z);
        }

        public void Load(Stream lineStream)
        {
            StartLoad(lineStream);
        }
    }
}