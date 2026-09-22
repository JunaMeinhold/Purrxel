namespace VoxelEngine.Graphics.D3D11
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime;
    using HexaGen.Runtime.COM;
    using VoxelEngine.IO;
    using Format = Hexa.NET.DXGI.Format;
    using ID3D11Device = Hexa.NET.D3D11.ID3D11Device;

    public static class TextureHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeMipLevels(int width, int height)
        {
            return (int)MathF.Log2(MathF.Max(width, height));
        }

        public static bool GenerateMipMaps { get; set; } = true;

        public static unsafe ComPtr<ID3D11Texture1D> LoadTexture1DFile(ComPtr<ID3D11Device> device, string path, Texture1DDesc desc, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Hexa.NET.DirectXTex.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Hexa.NET.DirectXTex.ID3D11Device*)device.Handle,
                scratchImage,
                (uint)desc.Usage,
                desc.BindFlags,
                desc.CPUAccessFlags,
                desc.MiscFlags,
                CreateTexFlags.Default,
                &res).ThrowIf();

            scratchImage.Release();
            ComPtr<ID3D11Texture1D> texture = default;
            texture.Handle = (ID3D11Texture1D*)res;
            return texture;
        }

        public static unsafe ComPtr<ID3D11Texture2D> LoadTexture2DFile(ComPtr<ID3D11Device> device, string path, Texture2DDesc desc, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Hexa.NET.DirectXTex.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Hexa.NET.DirectXTex.ID3D11Device*)device.Handle,
                scratchImage,
                (uint)desc.Usage,
                desc.BindFlags,
                desc.CPUAccessFlags,
                desc.MiscFlags,
                CreateTexFlags.Default,
                &res).ThrowIf();

            scratchImage.Release();
            ComPtr<ID3D11Texture2D> texture = default;
            texture.Handle = (ID3D11Texture2D*)res;
            return texture;
        }

        public static unsafe ComPtr<ID3D11Texture3D> LoadTexture3DFile(ComPtr<ID3D11Device> device, string path, Texture3DDesc desc, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Hexa.NET.DirectXTex.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Hexa.NET.DirectXTex.ID3D11Device*)device.Handle,
                scratchImage,
                (uint)desc.Usage,
                desc.BindFlags,
                desc.CPUAccessFlags,
                desc.MiscFlags,
                CreateTexFlags.Default,
                &res).ThrowIf();

            scratchImage.Release();
            ComPtr<ID3D11Texture3D> texture = default;
            texture.Handle = (ID3D11Texture3D*)res;
            return texture;
        }

        private static void SwapTexture(ref ScratchImage target, ScratchImage newTex)
        {
            target.Release();
            target = newTex;
        }

        private static unsafe void ProcessTexture(ref ScratchImage image)
        {
            TexMetadata metadata = image.GetMetadata();

            HResult result;
            if (GenerateMipMaps && metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), &metadata, TexFilterFlags.Default, (nuint)ComputeMipLevels((int)metadata.Width, (int)metadata.Height), &image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                SwapTexture(ref image, image1);
            }
        }

        /// <summary>
        /// Loads a Texture2DArray from image files.<br/>
        /// Automatically selects WIC, DDS, TGA, HDR after the file extension.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        public static unsafe ComPtr<ID3D11Texture2D> LoadFromFiles(ComPtr<ID3D11Device> device, string[] paths)
        {
            ScratchImage[] textures = new ScratchImage[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ScratchImage tex = LoadFromFile(Paths.CurrentTexturePath + paths[i]);
                textures[i] = tex;
            }

            TexMetadata meta = textures[0].GetMetadata();
            Texture2DDesc desc = new()
            {
                Width = (uint)meta.Width,
                Height = (uint)meta.Height,
                ArraySize = (uint)paths.Length,
                BindFlags = (uint)BindFlag.ShaderResource,
                Usage = Usage.Immutable,
                CPUAccessFlags = 0,
                Format = (Format)meta.Format,
                MipLevels = (uint)meta.MipLevels,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
            };

            SubresourceData[] subresources = new SubresourceData[(textures.Length * (int)meta.MipLevels)];
            int a = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                for (int j = 0; j < (int)meta.MipLevels; j++)
                {
                    Image* img = textures[i].GetImage((nuint)j, 0, 0);
                    subresources[a++] = new(img->Pixels, (uint)img->RowPitch, (uint)img->SlicePitch);
                }
            }

            device.CreateTexture2D(ref desc, ref subresources[0], out ComPtr<ID3D11Texture2D> texture).ThrowIf();

            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Release();
            }

            return texture;
        }

        public static ScratchImage LoadFromFile(string path)
        {
            string extension = Path.GetExtension(path);
            return extension switch
            {
                ".dds" => LoadFromDDSFile(path),
                ".tga" => LoadFromTGAFile(path),
                ".hdr" => LoadFromHDRFile(path),
                _ => LoadFromWICFile(path),
            };
        }

        public static unsafe ScratchImage LoadFromWICFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            nint ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromWICMemory((void*)ptr, (nuint)fs.Length, WICFlags.None, null, &image, default);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromDDSFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            nint ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromDDSMemory((void*)ptr, (nuint)fs.Length, DDSFlags.None, null, &image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromTGAFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            nint ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromTGAMemory((void*)ptr, (nuint)fs.Length, TGAFlags.None, null, &image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromHDRFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            nint ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromHDRMemory((void*)ptr, (nuint)fs.Length, null, &image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static (Usage Usage, uint BindFlag) ConvertToUB(CpuAccessFlags CpuAccessFlags, GpuAccessFlags gpuAccessFlags)
        {
            if ((CpuAccessFlags & CpuAccessFlags.Read) != 0 && (gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot read at the same time");
            }

            if ((CpuAccessFlags & CpuAccessFlags.Write) != 0 && (gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot write at the same time");
            }

            if (CpuAccessFlags != 0 && (gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot use rw with uva at the same time");
            }

            (Usage Usage, uint BindFlag) result = default;

            if ((gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                result.Usage = Usage.Default;
                result.BindFlag |= (uint)BindFlag.ShaderResource;
            }

            if ((gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                result.Usage = Usage.Default;
                result.BindFlag |= (uint)BindFlag.RenderTarget;
            }

            if ((gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                result.Usage = Usage.Default;
                result.BindFlag |= (uint)BindFlag.UnorderedAccess;
            }

            if ((CpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                result.Usage = Usage.Dynamic;
                result.BindFlag = (uint)BindFlag.ShaderResource;
            }

            if ((CpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                result.Usage = Usage.Staging;
                result.BindFlag = 0;
            }

            return result;
        }
    }
}