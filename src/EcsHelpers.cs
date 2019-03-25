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
        /// Unique component pools. Dont change manually!
        public static readonly Dictionary<int, IEcsComponentPool> ComponentPools = new Dictionary<int, IEcsComponentPool> (512);

        /// <summary>
        /// Unique components count. Dont change manually!
        /// </summary>
        public static int ComponentPoolsCount;
    }
}