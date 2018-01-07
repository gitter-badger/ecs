// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsFilter {
        /// <summary>
        /// Components mask for filtering entities with required components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask IncludeMask;

        /// <summary>
        /// Components mask for filtering entities with denied components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask ExcludeMask;

        /// <summary>
        /// List of filtered entities.
        /// Do not change it manually!
        /// </summary>
        public readonly List<int> Entities = new List<int> (512);

        /// <summary>
        /// Raises on entity added to filter.
        /// </summary>
        public event EcsWorld.OnEntityComponentChangeHandler OnEntityAdded = delegate { };

        /// <summary>
        /// Raises on entity removed from filter.
        /// </summary>
        public event EcsWorld.OnEntityComponentChangeHandler OnEntityRemoved = delegate { };

        /// <summary>
        /// Raises on entity changed inplace.
        /// </summary>
        public event EcsWorld.OnEntityComponentChangeHandler OnEntityUpdated = delegate { };

        internal void RaiseOnEntityAdded (int entity, int componentId) {
            OnEntityAdded (entity, componentId);
        }

        internal void RaiseOnEntityRemoved (int entity, int componentId) {
            OnEntityRemoved (entity, componentId);
        }

        internal void RaiseOnEntityUpdated (int entity, int componentId) {
            OnEntityUpdated (entity, componentId);
        }

        internal EcsFilter (EcsComponentMask include, EcsComponentMask exclude) {
            IncludeMask = include;
            ExcludeMask = exclude;
        }

        public override string ToString () {
            return string.Format ("Filter(+{0} -{1})", IncludeMask, ExcludeMask);
        }
    }
}