// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    public sealed class EcsWorld {
        public delegate void OnEntityComponentChangeHandler (int entity, int componentId);

        /// <summary>
        /// Raises on component attached to entity.
        /// </summary>
        public event OnEntityComponentChangeHandler OnEntityComponentAdded = delegate { };

        /// <summary>
        /// Raises on component detached from entity.
        /// </summary>
        public event OnEntityComponentChangeHandler OnEntityComponentRemoved = delegate { };

        /// <summary>
        /// Registered IEcsPreInitSystem systems.
        /// </summary>
        readonly List<IEcsPreInitSystem> _preInitSystems = new List<IEcsPreInitSystem> (16);

        /// <summary>
        /// Registered IEcsInitSystem systems.
        /// </summary>
        readonly List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem> (32);

        /// <summary>
        /// Registered IEcsRunSystem systems with EcsRunSystemType.Update.
        /// </summary>
        readonly List<IEcsRunSystem> _runUpdateSystems = new List<IEcsRunSystem> (64);

        /// <summary>
        /// Registered IEcsRunSystem systems with EcsRunSystemType.FixedUpdate.
        /// </summary>
        readonly List<IEcsRunSystem> _runFixedUpdateSystems = new List<IEcsRunSystem> (32);

        /// <summary>
        /// Dictionary for fast search component (type.hashcode) -> type id.
        /// </summary>
        readonly Dictionary<int, int> _componentIds = new Dictionary<int, int> (EcsComponentMask.BitsCount);

        /// <summary>
        /// Pools list for recycled component instances.
        /// </summary>
        readonly EcsComponentPool[] _componentPools = new EcsComponentPool[EcsComponentMask.BitsCount];

        /// <summary>
        /// List of all entities (their components).
        /// </summary>
        readonly List<EcsEntity> _entities = new List<EcsEntity> (1024);

        /// <summary>
        /// List of removed entities - they can be reused later.
        /// </summary>
        readonly List<int> _reservedEntityIds = new List<int> (256);

        /// <summary>
        /// List of add / remove operations for components on entities.
        /// </summary>
        readonly List<DelayedUpdate> _delayedUpdates = new List<DelayedUpdate> (128);

        /// <summary>
        /// List of requested filters.
        /// </summary>
        readonly List<EcsFilter> _filters = new List<EcsFilter> (64);

        /// <summary>
        /// Shared data, useful for ScriptableObjects / assets sharing.
        /// </summary>
        readonly Dictionary<int, object> _sharedData = new Dictionary<int, object> (32);

#if DEBUG && !ECS_PERF_TEST
        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;
#endif

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld AddSystem (IEcsSystem system) {
#if DEBUG && !ECS_PERF_TEST
            if (_inited) {
                throw new Exception ("Already initialized, cant add new system.");
            }
#endif
            EcsInjections.Inject (this, system);

            var preInitSystem = system as IEcsPreInitSystem;
            if (preInitSystem != null) {
                _preInitSystems.Add (preInitSystem);
            }

            var initSystem = system as IEcsInitSystem;
            if (initSystem != null) {
                _initSystems.Add (initSystem);
            }

            var runSystem = system as IEcsRunSystem;
            if (runSystem != null) {
                switch (runSystem.GetRunSystemType ()) {
                    case EcsRunSystemType.Update:
                        _runUpdateSystems.Add (runSystem);
                        break;
                    case EcsRunSystemType.FixedUpdate:
                        _runFixedUpdateSystems.Add (runSystem);
                        break;
                }
            }
            return this;
        }

        /// <summary>
        /// Sets shared data by key. Exists data will be overwritten.
        /// </summary>
        /// <param name="key">Key.</param>
        public EcsWorld SetSharedData (string key, object data) {
#if DEBUG && !ECS_PERF_TEST
            if (key == null || data == null) {
                throw new ArgumentNullException ("Invalid parameters");
            }
#endif
            _sharedData[key.GetHashCode ()] = data;
            return this;
        }

        /// <summary>
        /// Get shared data by key.
        /// </summary>
        /// <param name="key">Key.</param>
        public object GetSharedData (string key) {
#if DEBUG && !ECS_PERF_TEST
            if (key == null) {
                throw new ArgumentNullException ("Invalid parameter");
            }
#endif
            object retVal;
            _sharedData.TryGetValue (key.GetHashCode (), out retVal);
            return retVal;
        }

        /// <summary>
        /// Closes registration for new external data, initialize all registered systems.
        /// </summary>
        public void Initialize () {
#if DEBUG && !ECS_PERF_TEST
            _inited = true;
#endif
            for (var i = 0; i < _preInitSystems.Count; i++) {
                _preInitSystems[i].PreInitialize ();
                ProcessDelayedUpdates ();
            }
            for (var i = 0; i < _initSystems.Count; i++) {
                _initSystems[i].Initialize ();
                ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Destroys all registered external data, full cleanup for internal data.
        /// </summary>
        public void Destroy () {
            for (int i = 0, iMax = _entities.Count; i < iMax; i++) {
                RemoveEntity (i);
            }
            ProcessDelayedUpdates ();

            for (var i = 0; i < _preInitSystems.Count; i++) {
                _preInitSystems[i].PreDestroy ();
            }
            for (var i = 0; i < _initSystems.Count; i++) {
                _initSystems[i].Destroy ();
            }

            _initSystems.Clear ();
            _runUpdateSystems.Clear ();
            _runFixedUpdateSystems.Clear ();
            _componentIds.Clear ();
            _entities.Clear ();
            _reservedEntityIds.Clear ();
            _filters.Clear ();
            _sharedData.Clear ();
            for (var i = _componentPools.Length - 1; i >= 0; i--) {
                _componentPools[i] = null;
            }
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems with [EcsRunUpdate] attribute.
        /// </summary>
        public void RunUpdate () {
            for (var i = 0; i < _runUpdateSystems.Count; i++) {
                _runUpdateSystems[i].Run ();
                ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems with [EcsRunFixedUpdate] attribute.
        /// </summary>
        public void RunFixedUpdate () {
            for (var i = 0; i < _runFixedUpdateSystems.Count; i++) {
                _runFixedUpdateSystems[i].Run ();
                ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        public int CreateEntity () {
            int entity;
            if (_reservedEntityIds.Count > 0) {
                var id = _reservedEntityIds.Count - 1;
                entity = _reservedEntityIds[id];
                _entities[entity].IsReserved = false;
                _reservedEntityIds.RemoveAt (id);
            } else {
                entity = _entities.Count;
                _entities.Add (new EcsEntity ());
            }
            return entity;
        }

        /// <summary>
        /// Removes exists entity or throws exception on invalid one.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveEntity (int entity) {
            if (!_entities[entity].IsReserved) {
                _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveEntity, entity, 0));
            }
        }

        /// <summary>
        /// Adds component to entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public T AddComponent<T> (int entity, int componentId = -1) where T : class, IEcsComponent {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            var entityData = _entities[entity];
            var pool = _componentPools[componentId];
            ComponentLink link;
            // direct initialization - faster than constructor call.
            link.ItemId = -1;
            link.PoolId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                link = entityData.Components[i];
                if (link.PoolId == componentId) {
                    break;
                }
            }
            if (i != -1) {
                // already exists.
                return pool.Items[link.ItemId] as T;
            }

            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.AddComponent, entity, componentId));

            link.PoolId = componentId;
            link.ItemId = pool.GetIndex ();
            if (entityData.ComponentsCount == entityData.Components.Length) {
                var newComponents = new ComponentLink[entityData.ComponentsCount << 1];
                Array.Copy (entityData.Components, newComponents, entityData.ComponentsCount);
                entityData.Components = newComponents;
            }
            entityData.Components[entityData.ComponentsCount++] = link;
            return pool.Items[link.ItemId] as T;
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public void RemoveComponent<T> (int entity, int componentId = -1) where T : class, IEcsComponent {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveComponent, entity, componentId));
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public T GetComponent<T> (int entity, int componentId = -1) where T : class, IEcsComponent {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            var entityData = _entities[entity];
            ComponentLink link;
            // direct initialization - faster than constructor call.
            link.ItemId = -1;
            link.PoolId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                link = entityData.Components[i];
                if (link.PoolId == componentId) {
                    break;
                }
            }
            return i != -1 ? _componentPools[link.PoolId].Items[link.ItemId] as T : null;
        }

        /// <summary>
        /// Updates component on entity - OnUpdated event will be raised on compatible filters.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public void UpdateComponent<T> (int entity, int componentId = -1) where T : class, IEcsComponent {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.UpdateComponent, entity, componentId));
        }

        /// <summary>
        /// Gets component index. Useful for GetComponent() requests as second parameter for performance reason.
        /// </summary>
        public int GetComponentIndex<T> () where T : class, IEcsComponent {
            return GetComponentIndex (typeof (T));
        }

        /// <summary>
        /// Gets component index. Useful for GetComponent() requests as second parameter for performance reason.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public int GetComponentIndex (Type componentType) {
#if DEBUG && !ECS_PERF_TEST
            if (componentType == null || !typeof (IEcsComponent).IsAssignableFrom (componentType) || !componentType.IsClass) {
                throw new Exception ("Invalid component type");
            }
#endif
            int retVal;
            var type = componentType.GetHashCode ();
            if (!_componentIds.TryGetValue (type, out retVal)) {
                retVal = _componentIds.Count;
                _componentIds[type] = retVal;
                _componentPools[retVal] = new EcsComponentPool (componentType);
            }
            return retVal;
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        /// <param name="include">Component mask for required components.</param>
        /// <param name="include">Component mask for denied components.</param>
        internal EcsFilter GetFilter (EcsComponentMask include, EcsComponentMask exclude) {
#if DEBUG && !ECS_PERF_TEST
            if (include == null) {
                throw new ArgumentNullException ("include");
            }
            if (exclude == null) {
                throw new ArgumentNullException ("exclude");
            }
#endif
            var i = _filters.Count - 1;
            for (; i >= 0; i--) {
                if (this._filters[i].IncludeMask.IsEquals (include) && _filters[i].ExcludeMask.IsEquals (exclude)) {
                    break;
                }
            }
            if (i == -1) {
                i = _filters.Count;
                _filters.Add (new EcsFilter (include, exclude));
            }
            return _filters[i];
        }

        /// <summary>
        /// Gets stats of internal data.
        /// </summary>
        public EcsWorldStats GetStats () {
            var stats = new EcsWorldStats () {
                InitSystems = _initSystems.Count,
                RunUpdateSystems = _runUpdateSystems.Count,
                RunFixedUpdateSystems = _runFixedUpdateSystems.Count,
                AllEntities = _entities.Count,
                ReservedEntities = _reservedEntityIds.Count,
                Filters = _filters.Count,
                Components = _componentIds.Count,
                DelayedUpdates = _delayedUpdates.Count
            };
            return stats;
        }

        readonly EcsComponentMask _delayedOpMask = new EcsComponentMask ();

        /// <summary>
        /// Manually processes delayed updates. Use carefully!
        /// </summary>
        public void ProcessDelayedUpdates () {
            var iMax = _delayedUpdates.Count;
            for (var i = 0; i < iMax; i++) {
                var op = _delayedUpdates[i];
                var entityData = _entities[op.Entity];
                _delayedOpMask.CopyFrom (entityData.Mask);
                switch (op.Type) {
                    case DelayedUpdate.Op.RemoveEntity:
                        if (!entityData.IsReserved) {
                            var componentId = 0;
                            while (!entityData.Mask.IsEmpty ()) {
                                if (entityData.Mask.GetBit (componentId)) {
                                    entityData.Mask.SetBit (componentId, false);
                                    DetachComponent (op.Entity, entityData, componentId);
                                    UpdateFilters (op.Entity, componentId, _delayedOpMask, entityData.Mask);
                                    _delayedOpMask.SetBit (componentId, false);
                                }
                                componentId++;
                            }
                            entityData.IsReserved = true;
                            _reservedEntityIds.Add (op.Entity);
                        }
                        break;
                    case DelayedUpdate.Op.AddComponent:
                        if (!entityData.Mask.GetBit (op.Component)) {
                            entityData.Mask.SetBit (op.Component, true);
                            OnEntityComponentAdded (op.Entity, op.Component);
                            UpdateFilters (op.Entity, op.Component, _delayedOpMask, entityData.Mask);
                        }
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        if (entityData.Mask.GetBit (op.Component)) {
                            entityData.Mask.SetBit (op.Component, false);
                            DetachComponent (op.Entity, entityData, op.Component);
                            UpdateFilters (op.Entity, op.Component, _delayedOpMask, entityData.Mask);
                        }
                        break;
                    case DelayedUpdate.Op.UpdateComponent:
                        for (var filterId = 0; filterId < _filters.Count; filterId++) {
                            var filter = _filters[filterId];
                            if (_delayedOpMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                                filter.RaiseOnEntityUpdated (op.Entity, op.Component);
                            }
                        }
                        break;
                }
            }
            if (iMax > 0) {
                if (_delayedUpdates.Count == iMax) {
                    _delayedUpdates.Clear ();
                } else {
                    _delayedUpdates.RemoveRange (0, iMax);
                    ProcessDelayedUpdates ();
                }
            }
        }

        /// <summary>
        /// Detaches component from entity and raise OnComponentDetach event.
        /// </summary>
        /// <param name="entityId">Entity Id.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Detaching component.</param>
        void DetachComponent (int entityId, EcsEntity entity, int componentId) {
            ComponentLink link;
            for (var i = entity.ComponentsCount - 1; i >= 0; i--) {
                link = entity.Components[i];
                if (link.PoolId == componentId) {
                    entity.ComponentsCount--;
                    Array.Copy (entity.Components, i + 1, entity.Components, i, entity.ComponentsCount - i);
                    OnEntityComponentRemoved (entityId, componentId);
                    _componentPools[link.PoolId].RecycleIndex (link.ItemId);
                    return;
                }
            }
        }

        /// <summary>
        /// Updates all filters for changed component mask.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="component">Component.</param>
        /// <param name="oldMask">Old component state.</param>
        /// <param name="newMask">New component state.</param>
        void UpdateFilters (int entity, int component, EcsComponentMask oldMask, EcsComponentMask newMask) {
            for (var i = _filters.Count - 1; i >= 0; i--) {
                var filter = _filters[i];
                var isNewMaskCompatible = newMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask);
                if (oldMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                    if (!isNewMaskCompatible) {
#if DEBUG && !ECS_PERF_TEST
                        if (filter.Entities.IndexOf (entity) == -1) {
                            throw new Exception (
                                string.Format ("Something wrong - entity {0} should be in filter {1}, but not exits.", entity, filter));
                        }
#endif
                        filter.Entities.Remove (entity);
                        filter.RaiseOnEntityRemoved (entity, component);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        filter.Entities.Add (entity);
                        filter.RaiseOnEntityAdded (entity, component);
                    }
                }
            }
        }

        struct DelayedUpdate {
            public enum Op {
                RemoveEntity,
                AddComponent,
                RemoveComponent,
                UpdateComponent
            }
            public Op Type;
            public int Entity;
            public int Component;

            public DelayedUpdate (Op type, int entity, int component) {
                Type = type;
                Entity = entity;
                Component = component;
            }
        }

        struct ComponentLink {
            public int PoolId;
            public int ItemId;
        }

        sealed class EcsEntity {
            public bool IsReserved;
            public readonly EcsComponentMask Mask = new EcsComponentMask ();
            public int ComponentsCount;
            public ComponentLink[] Components = new ComponentLink[8];
        }
    }
}