// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace LeopotamGroup.Ecs.Internals {
    sealed class EcsReactFilter {
        internal readonly EcsComponentMask IncludeMask;

        internal readonly EcsComponentMask ExcludeMask;

        public readonly List<int> Entities = new List<int> (512);

        public readonly List<IEcsReactSystem> Systems = new List<IEcsReactSystem> (64);

        internal EcsReactFilter (EcsComponentMask include, EcsComponentMask exclude) {
            IncludeMask = include;
            ExcludeMask = exclude;
        }

        public override string ToString () {
            return string.Format ("ReactFilter(+{0} -{1})", IncludeMask, ExcludeMask);
        }
    }
}