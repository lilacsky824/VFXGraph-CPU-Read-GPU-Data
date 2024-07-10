using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

/// <summary>
/// Manages the reading and visualization of collision data from a GPU particle system.
/// Please read https://github.com/lilacsky824/VFXGraph-CPU-Read-GPU-Data.git
/// </summary>
public class ReadCollision : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Visual Effect Graph component to read collision data from.")]
    private VisualEffect _vfx;
    [SerializeField, Range(0, 64)]
    [Tooltip("Maximum number of collisions to process.")]
    private uint _collisionCount = 16u;
    [SerializeField]
    private AudioSource _audioSource;

    /// <summary>
    /// Each Collision Data entry consists of 9 floats: 3 for position, 3 for the normal vector, and 3 for color.
    /// </summary>
    private const uint _perDataCount = 9;
    private readonly int _bufferID = Shader.PropertyToID("CollisionBuffer");
    private readonly int _bufferCapacityID = Shader.PropertyToID("CollisionBufferCapacity");
    private readonly int _bufferElementSizeID = Shader.PropertyToID("CollisionBufferElementSize");
    private GraphicsBuffer _buffer;
    private AsyncGPUReadbackRequest _readback;

    /// <summary>
    /// Reinitializes the buffer when properties are changed in the inspector.
    /// </summary>
    void OnValidate() => InitializeBuffer();

    void Update()
    {
        if (_readback.done)
        {
            _readback = AsyncGPUReadback.Request(_buffer, OnAsyncReadBack);
            _buffer.SetData(new[] { 0 });
        }
    }

    /// <summary>
    /// Initializes or reinitializes the graphics buffer.
    /// </summary>
    private void InitializeBuffer ()
    {
        if(_buffer != null)
            _buffer.Release();

        // We add 1 to reserve the first uint element to store the actual collision count.
        uint count = _collisionCount * _perDataCount + 1;
        _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)count, Marshal.SizeOf(typeof(uint)));
        _buffer.SetData(new[] { 0 });

        _vfx.SetGraphicsBuffer(_bufferID, _buffer);
        _vfx.SetUInt(_bufferCapacityID, _collisionCount);
        _vfx.SetUInt(_bufferElementSizeID, _perDataCount);
    }

    void OnAsyncReadBack(AsyncGPUReadbackRequest asyncGpuReadbackRequest)
    {
        uint count = _readback.GetData<uint>()[0];
        if (count > 0)
        {
            Debug.Log(count);
            var data = _readback.GetData<float>();
            // Since the first element stores the count, we start at index 1 to read the first collision data entry
            int cursor = 1;
            for (uint collisionIndex = 0; collisionIndex < count && collisionIndex < _collisionCount; collisionIndex++)
            {
                // Sequentially read 3 floats each for position, normal, and color data of the collision
                Vector3 position = new Vector3(data[cursor++], data[cursor++], data[cursor++]);
                Vector3 normal = new Vector3(data[cursor++], data[cursor++], data[cursor++]);
                Color color = new Color(data[cursor++], data[cursor++], data[cursor++]);

                _audioSource.Play();

                Debug.DrawLine(position, position + normal * 0.5f, color, 1.0f);
            }
        }
    }

    void OnDisable()
    {
        _buffer.Release();
    }
}
