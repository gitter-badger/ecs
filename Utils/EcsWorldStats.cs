namespace LeopotamGroup.Ecs {
    public struct EcsWorldStats {
        /// <summary>
        /// Amount of registered systems.
        /// </summary>
        public int AllSystems;
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