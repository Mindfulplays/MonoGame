// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Maintains a cache of <see cref="MetalPipelineState"/> with an LRU eviction policy.
    /// </summary>
    internal class MetalPipelineStateCache
    {
        // If the number of items exceeds the max, then trim the cache down to an acceptable minimum.
        private const int TRIM_PIPELINE_CACHE_ITEMS_ = 64;
        private const int MAX_PIPELINE_CACHE_ITEMS_ = 128;

        /// <summary>
        /// Maintains a map of pipeline states based on internal state: A combination of
        /// shaders, blend states and such. State *creation* is expensive and hence is cached
        /// once per-GraphicsDevice. However, state application is cheap (and mandatory: Every
        /// render encoder requires a <code>MetalPipelineState</code>).
        /// </summary>
        private readonly Dictionary<MetalPipelineStateKey, MetalPipelineState> _pipelineStates = new();

        /// <summary>
        /// Gets or creates a new metal pipeline state for the provided key.
        /// Cache eviction may be triggered if too many pipeline states are present.
        /// Passed-in key may be a cached object to prevent allocations: this method creates a 
        /// copy of the passed in key.
        /// </summary>
        internal MetalPipelineState ObtainPipelineState(MetalPipelineStateKey key)
        {
            TrimCacheIfNeeded_();
            MetalPipelineState state;
            if (!_pipelineStates.TryGetValue(key, out state))
            {
                state = new();
                // Only create a new key when we add a new item. Avoids extra allocs.
                var newKey = new MetalPipelineStateKey(key);
                GraphicsDebug.Spam($"-----Creating new pipeline state {newKey}");
#if DEBUG
                if (GraphicsDebug.OutputDebugMessagesToConsole)
                {
                    GraphicsDebug.Spam(" --- Existing pipeline states:");
                    foreach (var existing in _pipelineStates.Keys)
                    {
                        GraphicsDebug.Spam($" ------ {existing} comparison: {newKey.GetComparison(existing)}");
                    }
                }
#endif                

                _pipelineStates.Add(newKey, state);
            }
            else { ++state.UsageCount; }

            return state;
        }

        /// This is similar to <see cref="System.Runtime.Caching.ObjectCache"/> but that's
        /// a large, complex implementation plus it operates off <code>string</code> keys which we do not
        /// have.
        private void TrimCacheIfNeeded_()
        {
            if (_pipelineStates.Count < MAX_PIPELINE_CACHE_ITEMS_) return;
            var stateKVs = _pipelineStates.OrderBy((kv) => kv.Value.UsageCount)
                .Take(TRIM_PIPELINE_CACHE_ITEMS_);

            // Evict the oldest states (i.e. lowest usage counts) to bring the cache down to the trim level.
            foreach (var oldState in stateKVs) { _pipelineStates.Remove(oldState.Key); }

            _pipelineStates.TrimExcess();
        }

        /// <summary>
        /// Ensure all Metal objects are cleanly disposed and the cache is completely evicted.
        /// </summary>
        internal void Clear()
        {
            foreach (var val in _pipelineStates.Values)
            {
                var valCapture = val;
                MetalGraphicsHelpers.CleanDispose(ref valCapture);
            }

            _pipelineStates.Clear();
        }
    }
}
