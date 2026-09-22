namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System.Buffers.Binary;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using VoxelEngine.Debugging;

    /// <summary>
    /// Thread-safe shader cache
    /// </summary>
    public static class ShaderCache
    {
        public const string File = "cache/shadercache.bin";
        public const int Version = 2;

        private static readonly List<ShaderCacheEntry> entries = new();

        private static readonly SemaphoreSlim semaphore = new(1);
        private static readonly object _lock = new();

        static ShaderCache()
        {
            _ = Directory.CreateDirectory("cache");
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            Load();
        }

        private static void ProcessExit(object? sender, EventArgs e)
        {
            Save();
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Release();
            }
        }

        /// <summary>
        /// Disables the shader cache
        /// </summary>
        public static bool DisableCache { get; set; }

        /// <summary>
        /// Returns the list of shader cache entries, please use the SyncObject to avoid race conditions.
        /// </summary>
        public static IReadOnlyList<ShaderCacheEntry> Entries => entries;

        /// <summary>
        /// The sync object commonly used for lock() operations and to prevent race conditions.
        /// </summary>
        public static object SyncObject => _lock;

        /// <summary>
        /// To cache a shader,<br/>
        /// Note: if the shader cache is disabled the method returns immediately without any action, <br/>
        /// Note: all entries that are equal to the path and language and macros.<br/>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="language"></param>
        /// <param name="hash"></param>
        /// <param name="macros"></param>
        /// <param name="inputElements"></param>
        /// <param name="shader"></param>
        public static unsafe void CacheShader(string path, SourceLanguage language, uint hash, ShaderMacro[] macros, InputElementDescription[] inputElements, Shader* shader)
        {
            if (DisableCache)
            {
                return;
            }

            lock (_lock)
            {
                var entry = new ShaderCacheEntry(path, language, hash, macros, inputElements, shader->Clone());
                entries.RemoveAll(x => x.EqualsForDelete(entry));
                entries.Add(entry);
                SaveAsync();
            }
        }

        /// <summary>
        /// Returns true if successfully found a matching shader
        /// </summary>
        /// <param name="path"></param>
        /// <param name="language"></param>
        /// <param name="macros"></param>
        /// <param name="shader"></param>
        /// <param name="inputElements"></param>
        /// <returns></returns>
        public static unsafe bool GetShader(string path, SourceLanguage language, uint hash, ShaderMacro[] macros, Shader** shader, [MaybeNullWhen(false)] out InputElementDescription[]? inputElements)
        {
            *shader = default;
            inputElements = null;
            if (DisableCache)
            {
                return false;
            }

            lock (_lock)
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var ventry = new ShaderCacheEntry(path, language, hash, macros, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                var entry = entries.FirstOrDefault(x => x.Equals(ventry));
                if (entry != default)
                {
                    inputElements = entry.InputElements;
                    *shader = entry.Shader->Clone();
                    return true;
                }
                return false;
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                ImGuiConsole.Log(ConsoleMessageType.Info, "Clearing shader cache ...");
                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i].Release();
                }
                entries.Clear();
                ImGuiConsole.Log(ConsoleMessageType.Info, "Clearing shader cache ... done");
            }
        }

        private static void Load()
        {
            if (!System.IO.File.Exists(File))
            {
                return;
            }

            lock (_lock)
            {
                var span = (Span<byte>)System.IO.File.ReadAllBytes(File);
                var decoder = Encoding.UTF8.GetDecoder();
                var version = BinaryPrimitives.ReadInt32LittleEndian(span);
                if (version != Version)
                    return;
                var count = BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
                entries.EnsureCapacity(count);

                int idx = 8;
                for (int i = 0; i < count; i++)
                {
                    var entry = new ShaderCacheEntry();
                    idx += entry.Read(span[idx..], decoder);
                    entries.Add(entry);
                }
            }
        }

        private static void Save()
        {
            semaphore.Wait();
            lock (_lock)
            {
                var encoder = Encoding.UTF8.GetEncoder();
                var size = 8 + entries.Sum(x => x.SizeOf(encoder));
                var span = size < 2048 ? stackalloc byte[size] : new byte[size];

                BinaryPrimitives.WriteInt32LittleEndian(span[0..], Version);
                BinaryPrimitives.WriteInt32LittleEndian(span[4..], entries.Count);

                int idx = 8;
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    idx += entry.Write(span[idx..], encoder);
                }

                System.IO.File.WriteAllBytes(File, span.ToArray());
            }
            semaphore.Release();
        }

        private static Task SaveAsync()
        {
            return Task.Run(() =>
            {
                semaphore.Wait();
                lock (_lock)
                {
                    var encoder = Encoding.UTF8.GetEncoder();
                    var size = 8 + entries.Sum(x => x.SizeOf(encoder));
                    var span = size < 2048 ? stackalloc byte[size] : new byte[size];

                    BinaryPrimitives.WriteInt32LittleEndian(span[0..], Version);
                    BinaryPrimitives.WriteInt32LittleEndian(span[4..], entries.Count);

                    int idx = 8;
                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        idx += entry.Write(span[idx..], encoder);
                    }

                    System.IO.File.WriteAllBytes(File, span.ToArray());
                }
                semaphore.Release();
            });
        }
    }

    public unsafe struct ShaderCacheEntry : IEquatable<ShaderCacheEntry>
    {
        public string Name;
        public SourceLanguage Language;
        public uint Hash;
        public ShaderMacro[] Macros;
        public InputElementDescription[] InputElements;
        public Shader* Shader;

        public ShaderCacheEntry(string name, SourceLanguage language, uint hash, ShaderMacro[] macros, InputElementDescription[] inputElements, Shader* bytecode)
        {
            Name = name;
            Language = language;
            Macros = macros;
            InputElements = inputElements;
            Hash = hash;
            Shader = bytecode;
        }

        public int Write(Span<byte> dest, Encoder encoder)
        {
            int idx = 0;
            idx += WriteString(dest[idx..], Name, encoder);
            idx += WriteInt32(dest[idx..], (int)Language);
            idx += WriteUInt32(dest[idx..], Hash);
            idx += WriteInt32(dest[idx..], Macros.Length);

            for (int i = 0; i < Macros.Length; i++)
            {
                var macro = Macros[i];
                idx += WriteString(dest[idx..], macro.Name, encoder);
                idx += WriteString(dest[idx..], macro.Definition, encoder);
            }
            idx += WriteInt32(dest[idx..], InputElements.Length);
            for (int i = 0; i < InputElements.Length; i++)
            {
                var element = InputElements[i];
                idx += WriteString(dest[idx..], element.SemanticName, encoder);
                idx += WriteInt32(dest[idx..], element.SemanticIndex);
                idx += WriteInt32(dest[idx..], (int)element.Format);
                idx += WriteInt32(dest[idx..], element.Slot);
                idx += WriteInt32(dest[idx..], element.AlignedByteOffset);
                idx += WriteInt32(dest[idx..], (int)element.Classification);
                idx += WriteInt32(dest[idx..], element.InstanceDataStepRate);
            }
            if (Shader != null)
            {
                BinaryPrimitives.WriteInt32LittleEndian(dest[idx..], (int)Shader->Length);
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(dest[idx..], 0);
            }

            idx += 4;

            if (Shader != null)
            {
                Shader->CopyTo(dest[idx..]);
                idx += (int)Shader->Length;
            }
            else
            {
                idx += 0;
            }

            return idx;
        }

        public int Read(ReadOnlySpan<byte> src, Decoder decoder)
        {
            int idx = 0;
            idx += ReadString(src, out Name, decoder);
            idx += ReadInt32(src[idx..], out var lang);
            idx += ReadUInt32(src[idx..], out Hash);
            Language = (SourceLanguage)lang;

            // read macros
            int count = BinaryPrimitives.ReadInt32LittleEndian(src[idx..]);
            idx += 4;
            Macros = new ShaderMacro[count];
            for (var i = 0; i < count; i++)
            {
                idx += ReadString(src[idx..], out string name, decoder);
                idx += ReadString(src[idx..], out string definition, decoder);
                Macros[i] = new ShaderMacro(name, definition);
            }

            int countElements = BinaryPrimitives.ReadInt32LittleEndian(src[idx..]);
            idx += 4;
            InputElements = new InputElementDescription[countElements];
            for (var i = 0; i < countElements; i++)
            {
                idx += ReadString(src[idx..], out string semanticName, decoder);
                idx += ReadInt32(src[idx..], out int semanticIndex);
                idx += ReadInt32(src[idx..], out int format);
                idx += ReadInt32(src[idx..], out int slot);
                idx += ReadInt32(src[idx..], out int alignedByteOffset);
                idx += ReadInt32(src[idx..], out int classification);
                idx += ReadInt32(src[idx..], out int instanceDataStepRate);
                InputElements[i] = new(semanticName, semanticIndex, (Format)format, slot, alignedByteOffset, (InputClassification)classification, instanceDataStepRate);
            }

            int len = BinaryPrimitives.ReadInt32LittleEndian(src[idx..]);
            idx += 4;
            Shader = AllocT<Shader>();
            Shader->Bytecode = AllocT<byte>(len);
            Shader->Length = (nuint)len;
            fixed (void* ptr = src.Slice(idx, len))
            {
                Buffer.MemoryCopy(ptr, Shader->Bytecode, len, len);
            }
            idx += len;
            return idx;
        }

        private static int WriteString(Span<byte> dest, string str, Encoder encoder)
        {
            BinaryPrimitives.WriteInt32LittleEndian(dest, encoder.GetByteCount(str, true));
            return encoder.GetBytes(str, dest[4..], true) + 4;
        }

        private static int ReadString(ReadOnlySpan<byte> src, out string str, Decoder decoder)
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(src);
            ReadOnlySpan<byte> bytes = src.Slice(4, len);
            int charCount = decoder.GetCharCount(bytes, true);
            Span<char> chars = charCount < 2048 ? stackalloc char[charCount] : new char[charCount];
            decoder.GetChars(bytes, chars, true);
            str = new(chars);
            return len + 4;
        }

        private static int WriteInt32(Span<byte> dest, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(dest, value);
            return 4;
        }

        private static int ReadInt32(ReadOnlySpan<byte> src, out int value)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(src);
            return 4;
        }

        private static int WriteUInt32(Span<byte> dest, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(dest, value);
            return 4;
        }

        private static int ReadUInt32(ReadOnlySpan<byte> src, out uint value)
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(src);
            return 4;
        }

        private static int SizeOf(string str, Encoder encoder)
        {
            return 4 + encoder.GetByteCount(str, true);
        }

        public int SizeOf(Encoder encoder)
        {
            if (Shader != null)
            {
                return 28 +
                    SizeOf(Name, encoder) +
                    Macros.Sum(x => SizeOf(x.Name, encoder) + SizeOf(x.Definition, encoder)) +
                    InputElements.Sum(x => SizeOf(x.SemanticName, encoder) + 24) +
                    (int)Shader->Length;
            }
            else
            {
                return 28 +
                    SizeOf(Name, encoder) +
                    Macros.Sum(x => SizeOf(x.Name, encoder) + SizeOf(x.Definition, encoder)) +
                    InputElements.Sum(x => SizeOf(x.SemanticName, encoder) + 24);
            }
        }

        public void Release()
        {
            Free(Shader);
            Shader = null;
        }

        public readonly bool Equals(ShaderCacheEntry other)
        {
            if (Name != other.Name)
            {
                return false;
            }

            if (Language != other.Language)
            {
                return false;
            }

            if (Hash != other.Hash)
            {
                return false;
            }

            if (Macros == other.Macros && Macros == null && other.Macros == null)
            {
                return true;
            }

            if (Macros != other.Macros && (Macros == null || other.Macros == null))
            {
                return false;
            }

            if (Macros.Length != (other.Macros?.Length ?? 0))
            {
                return false;
            }

            for (int i = 0; i < Macros.Length; i++)
            {
#nullable disable
                if (Macros[i].Name != other.Macros[i].Name ||
                    Macros[i].Definition != other.Macros[i].Definition)
                {
                    return false;
                }
#nullable enable
            }
            return true;
        }

        public readonly bool EqualsForDelete(ShaderCacheEntry other)
        {
            if (Name != other.Name)
            {
                return false;
            }

            if (Language != other.Language)
            {
                return false;
            }

            if (Macros == other.Macros && Macros == null && other.Macros == null)
            {
                return true;
            }

            if (Macros != other.Macros && (Macros == null || other.Macros == null))
            {
                return false;
            }

            if (Macros.Length != (other.Macros?.Length ?? 0))
            {
                return false;
            }

            for (int i = 0; i < Macros.Length; i++)
            {
#nullable disable
                if (Macros[i].Name != other.Macros[i].Name ||
                    Macros[i].Definition != other.Macros[i].Definition)
                {
                    return false;
                }
#nullable enable
            }
            return true;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ShaderCacheEntry entry && Equals(entry);
        }

        public static bool operator ==(ShaderCacheEntry left, ShaderCacheEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShaderCacheEntry left, ShaderCacheEntry right)
        {
            return !(left == right);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Name, Language, Hash, Macros);
        }

        public override readonly string ToString()
        {
            return Name;
        }
    }
}