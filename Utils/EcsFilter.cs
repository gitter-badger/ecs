// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsFilter {
        public delegate void OnFilterEntitiesChangeHandler (int entity);

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
        public event OnFilterEntitiesChangeHandler OnEntityAdded = delegate { };

        /// <summary>
        /// Raises on entity removed from filter.
        /// </summary>
        public event OnFilterEntitiesChangeHandler OnEntityRemoved = delegate { };

        /// <summary>
        /// Raises on entity changed inplace.
        /// </summary>
        public event OnFilterEntitiesChangeHandler OnEntityUpdated = delegate { };

        internal void RaiseOnEntityAdded (int entity) {
            UnityEngine.Debug.Log ("FILTER-ENTITY-ADDED");
            OnEntityAdded (entity);
        }

        internal void RaiseOnEntityRemoved (int entity) {
            OnEntityRemoved (entity);
        }

        internal void RaiseOnEntityUpdated (int entity) {
            OnEntityUpdated (entity);
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