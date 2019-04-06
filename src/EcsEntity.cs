// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Leopotam.Ecs {
    /// <summary>
    /// Entity index descriptor.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct EcsEntity {
        /// <summary>
        /// Warning: for internal use, dont touch it directly!
        /// </summary>
        public int Id;

        /// <summary>
        /// Warning: for internal use, dont touch it directly!
        /// </summary>
        public short Gen;

        /// <summary>
        /// Null entity index, can be used to reset indices.
        /// </summary>
        public static readonly EcsEntity Null = new EcsEntity ();

        /// <summary>
        /// Returns true if entity is nulled.
        /// </summary>
        /// <returns></returns>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsNull () {
            return Id == 0 && Gen == 0;
        }

        [Obsolete ("Use EcsEntity instead")]
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static implicit operator int (in EcsEntity lhs) {
            return lhs.Id;
        }

        [Obsolete ("Use EcsEntity instead")]
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsEntity (int lhs) {
            EcsEntity idx;
            idx.Gen = 1;
            idx.Id = lhs;
            return idx;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator == (in EcsEntity lhs, in EcsEntity rhs) {
            return lhs.Id == rhs.Id && lhs.Gen == rhs.Gen;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator != (in EcsEntity lhs, in EcsEntity rhs) {
            return lhs.Id != rhs.Id || lhs.Gen != rhs.Gen;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode () {
            return Id.GetHashCode () ^ (Gen.GetHashCode () << 2);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override bool Equals (object other) {
            if (!(other is EcsEntity)) {
                return false;
            }
            var rhs = (EcsEntity) other;
            return Id == rhs.Id && Gen == rhs.Gen;
        }
#if DEBUG
        public override string ToString () {
            return string.Format ("Entity-{0}", GetHashCode ());
        }
#endif
    }
}