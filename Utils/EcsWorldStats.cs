// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

namespace LeopotamGroup.Ecs {
    public struct EcsWorldStats {
        /// <summary>
        /// Amount of registered IEcsInitSystem systems.
        /// </summary>
        public int InitSystems;

        /// <summary>
        /// Amount of registered IEcsRunSystem systems with [EcsRunUpdate] attribute.
        /// </summary>
        public int RunUpdateSystems;

        /// <summary>
        /// Amount of registered IEcsRunSystem systems with [EcsRunFixedUpdate] attribute.
        /// </summary>
        public int RunFixedUpdateSystems;

        /// <summary>
        /// Amount of created entities.
        /// </summary>
        public int AllEntities;

        /// <summary>
        /// Amount of cached (not in use) entities.
        /// </summary>
        public int ReservedEntities;

        /// <summary>
        /// Amount of registered filters.
        /// </summary>
        public int Filters;

        /// <summary>
        /// Amount of registered component types.
        /// </summary>
        public int Components;

        /// <summary>
        /// Current amount of delayed updates.
        /// </summary>
        public int DelayedUpdates;
    }
}