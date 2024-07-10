# VFXGraph CPU Readback GPU Data
Notes on reading particle data from the GPU back to the CPU in VFX Graph.

Referencing Unity's official sample:
https://github.com/PaulDemeulenaere/vfx-readback

Tested with Unity 6000.0.8f1
![圖片](https://github.com/lilacsky824/VFXGraph-CPU-Read-GPU-Data/assets/75205949/4320ace5-db35-40bc-910d-381534816006)

## Walkthrough
1. Create a VFX Graph particle system
2. Confirm the data structure of the attributes to be read in the Attribute
   
  ![圖片](https://github.com/lilacsky824/VFXGraph-CPU-Read-GPU-Data/assets/75205949/eebb935d-f4ab-4d01-a51d-f296a257d249)

3. Add a Custom HLSL Block at the appropriate timing, such as during the Update phase, to write the corresponding particle attribute data into the GraphicsBuffer.
   You can use RWStructuredBuffer or RWByteAddressBuffer.
   HLSL code is something looks like this.
```
void CustomHLSL(inout VFXAttributes attributes, in RWStructuredBuffer<float3> positions)
{
  uint address = attributes.particleId % 64;
	positions[address] = attributes.position;
}
```
  In the demo, we use RWByteAddressBuffer to store color, collision position, and normal. 
  RWByteAddressBuffer can only read/write the uint type, but we can store floats as uints, then interpret uints back to floats on the CPU.
```
/// <summary>
/// Stores collision data into a RWByteAddressBuffer.
/// </summary>

void StoreCollisionDataIntoBuffer(inout VFXAttributes attributes, RWByteAddressBuffer readback, uint collisionBufferCapacity, uint elementSize)
{
    uint freeSlot;
    // Atomically increment the collision count and get the next free slot
    readback.InterlockedAdd(0, 1, freeSlot);
    if (freeSlot < collisionBufferCapacity)
    {
         uint offset = 1u + freeSlot * elementSize;
         float3 readPosition = attributes.collisionEventPosition;
         float3 readNormal = attributes.collisionEventNormal;
         float3 color = attributes.color;
         // RWByteAddressBuffer API only allows reading/writing uint values.
         // We use asuint() to reinterpret float data as uint without changing the bit pattern.
         readback.Store3((offset + 0) << 2u, asuint(readPosition));
         readback.Store3((offset + 3) << 2u, asuint(readNormal));
         readback.Store3((offset + 6) << 2u, asuint(color));
    }   
}
```
4. Create an exposed property to pass your GraphicsBuffer into the Custom HLSL Block.
5. Create a C# script to pass the GraphicsBuffer to your VFX Graph and read it back using AsyncGPUReadback.Request.
   Please see [ReadCollision.cs](Assets/ReadCollision.cs)
