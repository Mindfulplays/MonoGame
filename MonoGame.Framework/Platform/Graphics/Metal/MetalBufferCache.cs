// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Metal;
using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Caches Metal buffers across frames.
    /// MonoGame is an immediate mode renderer with or without
    /// <see cref="SpriteSortMode"/> = <see cref="SpriteSortMode.Deferred"/>.
    /// If the caller uses <code>SpriteMode=Immediate</code> it's a double whammy.
    /// Based on profiling, we would end up spending close to 95% of frame time just
    /// allocating buffers that we would throw away immediately after this frame.
    /// Here we assume that some of these buffers will be used later based on some
    /// LRU cache. Regardless, the buffers will be aged and discarded accordingly
    /// if they are not used across multiple frames to avoid hogging graphics memory.
    /// </summary>
    internal class MetalBufferCache : IDisposable
    {
        private MetalBufferHolder _currentFrame = new();
        private MetalBufferHolder _previousFrame = new();
        private int _frameIndex = -1;

        internal void ResetHeap()
        {
            _currentFrame.ClearCache();
            _previousFrame.ClearCache();
            _frameIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal unsafe IMTLBuffer CreateBuffer<T>(GraphicsDevice device, T[] data, int elementCount, int elementOffset,
            bool allowReuse)
            where T : struct
        {
            return _CreateBufferWithCache(device, data, elementCount, elementOffset, allowReuse);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private unsafe IMTLBuffer _CreateBufferWithCache<T>(GraphicsDevice device, T[] data, int elementCount,
            int elementOffset, bool allowReuse)
            where T : struct
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var sizeBytes = (UIntPtr)(ReflectionHelpers.SizeOf<T>.Get() * elementCount);
                var addr = (nint)(handle.AddrOfPinnedObject().ToInt64() +
                                  (ReflectionHelpers.SizeOf<T>.Get() * elementOffset));

                IMTLBuffer buffer = allowReuse ? _ObtainCachedBufferWithMatchingData(addr, sizeBytes) : null;
                if (buffer != null) { return buffer; }

                buffer = _ObtainCachedBufferWithSize(device, sizeBytes);
                Buffer.MemoryCopy(addr.ToPointer(), buffer.Contents.ToPointer(), (long)sizeBytes, (long)sizeBytes);
                //ReadOnlySpan<byte> source = new(addr.ToPointer(), (int)sizeBytes);
                //Span<byte> target = new(buffer.Contents.ToPointer(), (int)sizeBytes);
                //source.CopyTo(target);
                return buffer;
            }
            finally { handle.Free(); }
        }

        /// <summary>
        /// Checks if there is an existing buffer with matching size and data (bytes).
        /// This is usually much faster than creating a brand new buffer (including
        /// ObjC runtime pinvoke / kernel transitions), copying the buffer over and
        /// maintaining the lifetime of that buffer too.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe IMTLBuffer _ObtainCachedBufferWithMatchingData(IntPtr addr, UIntPtr sizeBytes)
        {
            var currentItems = _currentFrame.ObtainItemsForSize(sizeBytes, false);
            ReadOnlySpan<byte> source = new(addr.ToPointer(), (int)sizeBytes);
            // See https://stackoverflow.com/a/48599119 for some alternatives.
            // For small arrays, Span.SequenceEqual is faster than unrolled or long pointers.
            if (currentItems?.Count > 0)
            {
                foreach (var buffer in currentItems)
                {
                    ReadOnlySpan<byte> target = new(buffer.Contents.ToPointer(), (int)sizeBytes);
                    if (buffer.Length >= sizeBytes && source.SequenceEqual(target)) { return buffer; }
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private IMTLBuffer _ObtainCachedBufferWithSize(GraphicsDevice device, UIntPtr size)
        {
            _EnsureCacheCurrentFrame(device);

            var item = _previousFrame.ObtainOrCreateCacheItem(device, size);
            _currentFrame.AddExisting(size, item);
            return item;
        }

        private void _EnsureCacheCurrentFrame(GraphicsDevice device)
        {
            if (_frameIndex == device.CurrentFrame) { return; }

            //GraphicsDebug.Spam($"-Changing frames buffer cache item size {_previousFrame.TotalItems}");
            (_currentFrame, _previousFrame) = (_previousFrame, _currentFrame);
            _currentFrame.AddAll(_previousFrame);
            _previousFrame.ClearCache();
            _frameIndex = device.CurrentFrame;
        }

        // internal IntPtr GetVertexAddr<T>(T[] vertexData, )
        public void Dispose()
        {
            ResetHeap();
        }
    }

    internal class MetalBufferHolder : IDisposable
    {
        private const int MIN_CACHE_SIZE_ = 5;

        /// <summary>Maps a <code>size</code> to a bunch of Metal buffers.</summary>
        private readonly Dictionary<UInt64, Queue<IMTLBuffer>> _cache = new();

        public int TotalItems
        {
            get
            {
                int c = 0;
                foreach (var items in _cache.Values) { c += items.Count; }

                return c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public IMTLBuffer AddExisting(UIntPtr size, IMTLBuffer buffer)
        {
            ObtainItemsForSize(size, true).Enqueue(buffer);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void AddAll(MetalBufferHolder from, int maxElementsPerLine = 10)
        {
            foreach (var thatItems in from._cache)
            {
                var items = ObtainItemsForSize((nuint)thatItems.Key, true);
                while (items.Count < maxElementsPerLine && thatItems.Value.TryDequeue(out var thatItem))
                {
                    if (thatItem.Length >= thatItems.Key) { items.Enqueue(thatItem); }
                    else { thatItem.Dispose(); }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public IMTLBuffer ObtainOrCreateCacheItem(GraphicsDevice device, UIntPtr size)
        {
            if (!_cache.TryGetValue(size, out var items))
            {
                items = new();
                _cache.Add(size, items);
            }

            if (items.Count > 0)
            {
                var item = items.Dequeue();
                if (item.Length >= size) { return item; }
            }

            var buffer = device.MetalDevice.CreateBuffer(size, MTLResourceOptions.StorageModeShared);
            if (buffer == null || buffer.Length < size)
            {
                throw new Exception($"Unable to alloc buffer of size {size} bytes");
            }

            return buffer;
        }

        public void Dispose()
        {
            ClearCache();
        }

        internal void ClearCache()
        {
            foreach (var items in _cache.Values)
            {
                foreach (var item in items) { item.Dispose(); }

                items.Clear();
            }

            _cache.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal Queue<IMTLBuffer> ObtainItemsForSize(UIntPtr size, bool createIfNeeded)
        {
            if (!_cache.TryGetValue(size, out var items))
            {
                items = new(MIN_CACHE_SIZE_);
                _cache.Add(size, items);
            }

            return items;
        }

        private IMTLBuffer _CreateBufferWithoutCache<T>(GraphicsDevice device, T[] data, int elementCount,
            int elementOffset, bool _unused)
            where T : struct
        {
            // DO NOT USE THIS, it will end up copying the entire data array without regards to elementCount or offset!
            var buffer = device.MetalDevice.CreateBuffer(data, MTLResourceOptions.StorageModeShared);
            //buffer.SetPurgeableState(MTLPurgeableState.NonVolatile);
            return buffer;
        }
    }
}
