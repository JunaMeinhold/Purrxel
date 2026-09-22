namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System;

    [Flags]
    public enum RegisterComponentMaskFlags : byte
    {
        None = 0,
        ComponentX = 1,
        ComponentY = 2,
        ComponentZ = 4,
        ComponentW = 8,
        All
    }

    public struct InputElementDescription : IEquatable<InputElementDescription>
    {
        public string SemanticName;
        public int SemanticIndex;
        public Format Format;
        public int Slot;
        public int AlignedByteOffset;
        public InputClassification Classification;
        public int InstanceDataStepRate;

        public InputElementDescription(string semanticName, int semanticIndex, Format format, int inputSlot, int alignedByteOffset, InputClassification inputSlotClass, int instanceDataStepRate)
        {
            SemanticName = semanticName;
            SemanticIndex = semanticIndex;
            Format = format;
            Slot = inputSlot;
            AlignedByteOffset = alignedByteOffset;
            Classification = inputSlotClass;
            InstanceDataStepRate = instanceDataStepRate;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is InputElementDescription description && Equals(description);
        }

        public readonly bool Equals(InputElementDescription other)
        {
            return SemanticName == other.SemanticName &&
                   SemanticIndex == other.SemanticIndex &&
                   Format == other.Format &&
                   Slot == other.Slot &&
                   AlignedByteOffset == other.AlignedByteOffset &&
                   Classification == other.Classification &&
                   InstanceDataStepRate == other.InstanceDataStepRate;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(SemanticName, SemanticIndex, Format, Slot, AlignedByteOffset, Classification, InstanceDataStepRate);
        }

        public static bool operator ==(InputElementDescription left, InputElementDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InputElementDescription left, InputElementDescription right)
        {
            return !(left == right);
        }
    }
}