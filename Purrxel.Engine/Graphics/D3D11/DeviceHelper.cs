namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using System.Text;

    public static unsafe class DeviceHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T value) where T : unmanaged
        {
            MappedSubresource mapped;
            context.Map(buffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mapped);
            Buffer.MemoryCopy(&value, mapped.PData, mapped.RowPitch, sizeof(T));
            context.Unmap(buffer.As<ID3D11Resource>(), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T[] values) where T : unmanaged
        {
            fixed (T* pData = values)
            {
                Write(context, buffer, pData, values.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T* values, int count) where T : unmanaged
        {
            MappedSubresource mapped;
            context.Map(buffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mapped);
            Buffer.MemoryCopy(values, mapped.PData, mapped.RowPitch, sizeof(T) * count);
            context.Unmap(buffer.As<ID3D11Resource>(), 0);
        }
    }

    public static unsafe class Utils
    {
        public static readonly Guid D3DDebugObjectName = new(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);

        internal static string? GetDebugName<T>(ComPtr<T> target) where T : unmanaged, IComObject<T>
        {
            return GetDebugName(target.Handle);
        }

        internal static string? GetDebugName(void* target)
        {
            ID3D11DeviceChild* child = (ID3D11DeviceChild*)target;
            if (child == null)
            {
                return null;
            }

            uint len;
            Guid guid = D3DDebugObjectName;
            child->GetPrivateData(&guid, &len, null);
            if (len == 0)
            {
                return string.Empty;
            }

            byte* pName = AllocT<byte>(len);
            child->GetPrivateData(&guid, &len, pName);
            string str = ToStr(pName, len);
            Free(pName);
            return str;
        }

        internal static string ToStr(byte* name, uint length)
        {
            return Encoding.UTF8.GetString(new Span<byte>(name, (int)length));
        }

        internal static void SetDebugName<T>(ComPtr<T> target, string? name) where T : unmanaged, IComObject<T>
        {
            if (name == null) return;
            SetDebugName(target.Handle, name);
        }

        internal static void SetDebugName(void* target, string name)
        {
            ID3D11DeviceChild* child = (ID3D11DeviceChild*)target;
            if (child == null)
            {
                return;
            }

            Guid guid = D3DDebugObjectName;
            if (name != null)
            {
                byte* pName = name.ToUTF8Ptr();
                child->SetPrivateData(&guid, (uint)name.Length, pName);
                Free(pName);
            }
            else
            {
                child->SetPrivateData(&guid, 0, null);
            }
        }
    }
}