// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Threading;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Provides an atomic, globally monotonically increasing ID that can be added to an instance
    /// of a specific class <param name="T"/>.
    /// </summary>
    internal class MetalId<T> where T : class
    {
        /// <summary>
        /// Use a unique id - may be called every frame (or even sub-frame).
        /// Id will wrap around if it overflows which is fine.
        /// One group of ids per class exploting how static on generic classes works.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType (as we want this functionality).
        private static volatile int _GLOBAL_ID = 0;

        internal readonly int Id;

        internal MetalId()
        {
            Id = Interlocked.Increment(ref _GLOBAL_ID);
        }

        protected bool Equals(MetalId<T> other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MetalId<T>)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return $"{typeof(T)} #{Id}";
        }
    }
}
