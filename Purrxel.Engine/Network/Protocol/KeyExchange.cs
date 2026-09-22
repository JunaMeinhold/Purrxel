namespace VoxelEngine.Network.Protocol
{
    public enum KeyExchange
    {
        ECDH_RSA,
        ECDH_DSA,
        ECDH_ECDSA,
        ChaCha20Poly1305,
    }
}