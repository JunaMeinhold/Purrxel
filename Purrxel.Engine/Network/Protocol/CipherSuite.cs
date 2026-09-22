namespace VoxelEngine.Network.Protocol
{
    public enum CipherSuite
    {
        None,
        AES_GCM_128_HMAC_SHA_256,
        AES_GCM_256_HMAC_SHA_384,
        AES_GCM_256_HMAC_SHA_512,
    }
}