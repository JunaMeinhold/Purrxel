# 🚀 Purrxel - High-Performance Voxel Engine (Minecraft-Like)

A **high-performance, fully multi-threaded voxel engine** that supports **massive render distances without FPS drops**. Designed for **real-time procedural terrain generation, fast world streaming, and ultra-efficient memory management.**  

## 🌟 Features

- **128 Render Distance at Stable FPS**  
  - Tested on an **RTX 3070 8GB** at **165Hz VSync ON** with **no FPS drops**.  
- **Lightning-Fast Disk Reads/Writes**  
  - **Ultra-efficient region format** keeps file sizes small while maintaining **instant chunk streaming**.  
- **Full World Streaming**  
  - Loads and unloads chunks dynamically, supporting **seamless infinite terrain exploration**.  
- **Procedural Terrain, Caves, and Trees**  
  - **Fully procedural world generation** with **realistic cave systems, biomes, and natural structures**.  
- **Multi-Threaded World Generation & Updates**  
  - **Dedicated IO threads** ensure that the **main thread only handles GPU uploads & coordination**, keeping performance ultra-smooth.  
- **Render Regions for Optimized Draw Calls**  
  - **Batched rendering** reduces CPU overhead, allowing for **massive draw distances with minimal performance impact**.  
- **Zero Garbage Collection Pressure**  
  - Uses **manual memory management (`unsafe` pointers for voxel data)** to completely **eliminate GC pauses**, ensuring **stable frame times** even under heavy load.  

## 🛠️ Technical Details

- **Written in C# with Ahead-of-Time (AOT) Compilation** for near-native performance.  
- **Uses RLE + LZ4 OPT 10 compression** to **minimize storage requirements** while keeping chunk loading near-instant.  
- **Full camera-relative rendering**, preventing **floating-point precision issues at extreme world distances**.  

## 🚀 Performance Benchmarks

| Render Distance | FPS (RTX 3070 8GB, 165Hz VSync ON) | VRAM Usage | RAM Usage |
|---------------|--------------------------------|------------|-----------|
| **32**       | **165 FPS (VSync)**           | **~400MB**  | **~1.5GB** |
| **64**       | **165 FPS (VSync)**           | **~1.1GB**   | **~5.5GB**   |
| **128**      | **165 FPS (VSync)**           | **~4,9GB**   | **~18GB**   |
