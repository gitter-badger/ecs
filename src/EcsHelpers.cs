// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Leopotam.Ecs.Internals {
    /// <summary>
    /// Internal helpers.
    /// </summary>
    public static class EcsHelpers {
#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int GetPowerOfTwoSize (int n) {
            if (n < 2) {
                return 2;
            }
            n--;
            n = n | (n >> 1);
            n = n | (n >> 2);
            n = n | (n >> 4);
            n = n | (n >> 8);
            n = n | (n >> 16);
            return n + 1;
        }

        public const int EntityIdBits = 21;
        public const int EntityGenBits = 31 - EntityIdBits;
        public const int EntityIdMask = (1 << EntityIdBits) - 1;
        public const int MaxEntityGen = 1 << EntityGenBits;

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static short DecodeEntityGen (ref int entityId) {
            var generation = entityId < 0 ? (short) 0 : (short) ((uint) entityId >> EntityIdBits);
            entityId &= EntityIdMask;
            return generation;
        }

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static void EncodeEntityGen (ref int entityId, short generation) {
            entityId |= generation << EntityIdBits;
        }

        /// Unique component pools. Dont change manually!
        public static readonly Dictionary<int, IEcsComponentPool> ComponentPools = new Dictionary<int, IEcsComponentPool> (512);

        /// <summary>
        /// Unique components count. Dont change manually!
        /// </summary>
        public static int ComponentPoolsCount;
    }
}