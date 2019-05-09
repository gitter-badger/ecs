// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Leopotam.Ecs {
    /// <summary>
    /// Entity index descriptor.
    /// </summary>
    [StructLayout (LayoutKind.Sequential)]
    [Serializable]
    public struct EcsEntity {
        /// <summary>
        /// Warning: for internal use, dont touch it directly!
        /// </summary>
        internal int Id;

        /// <summary>
        /// Warning: for internal use, dont touch it directly!
        /// </summary>
        internal ushort Gen;

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
        public int GetDebugId () {
            return Id;
        }

        public int GetDebugGen () {
            return Gen;
        }

        public static EcsEntity CreateDebugEntity (int gen, int id) {
            EcsEntity entity;
            entity.Gen = (ushort) gen;
            entity.Id = id;
            return entity;
        }

        public override string ToString () {
            return IsNull () ? "Entity-Null" : string.Format ("Entity-{0}:{1}", Id, Gen);
        }
#endif
    }
}