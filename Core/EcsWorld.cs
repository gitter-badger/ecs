// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    public delegate void OnEntityComponentChangeHandler (int entity, int componentId);

    /// <summary>
    /// Basic ecs world implementation.
    /// </summary>
    public class EcsWorld {
        /// <summary>
        /// Raises on component attached to entity.
        /// </summary>
        public event OnEntityComponentChangeHandler OnEntityComponentAdded = delegate { };

        /// <summary>
        /// Raises on component detached from entity.
        /// </summary>
        public event OnEntityComponentChangeHandler OnEntityComponentRemoved = delegate { };

        /// <summary>
        /// Should DI be used or skipped for manual initialization?
        /// </summary>
        protected bool UseDependencyInjection = true;

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
        EcsEntity[] _entities = new EcsEntity[1024];

        /// <summary>
        /// Amount of created entities at _entities array.
        /// </summary>
        int _entitiesCount;

        /// <summary>
        /// List of removed entities - they can be reused later.
        /// </summary>
        int[] _reservedEntities = new int[256];

        /// <summary>
        /// Amount of created entities at _entities array.
        /// </summary>
        int _reservedEntitiesCount;

        /// <summary>
        /// List of add / remove operations for components on entities.
        /// </summary>
        readonly List<DelayedUpdate> _delayedUpdates = new List<DelayedUpdate> (1024);

        /// <summary>
        /// List of requested filters.
        /// </summary>
        readonly List<EcsFilter> _filters = new List<EcsFilter> (64);

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
            if (UseDependencyInjection) {
                EcsInjections.Inject (this, system);
            }

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
            for (var i = 0; i < _entitiesCount; i++) {
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
            _filters.Clear ();
            _entitiesCount = 0;
            _reservedEntitiesCount = 0;
            for (var i = _componentPools.Length - 1; i >= 0; i--) {
                _componentPools[i] = null;
            }
            for (var i = _entities.Length - 1; i >= 0; i--) {
                _entities[i] = null;
            }
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems with [EcsRunUpdate] attribute.
        /// </summary>
        public void RunUpdate () {
#if DEBUG && !ECS_PERF_TEST
            if (!_inited) {
                throw new Exception ("World not initialized.");
            }
#endif
            for (var i = 0; i < _runUpdateSystems.Count; i++) {
                _runUpdateSystems[i].Run ();
                ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems with [EcsRunFixedUpdate] attribute.
        /// </summary>
        public void RunFixedUpdate () {
#if DEBUG && !ECS_PERF_TEST
            if (!_inited) {
                throw new Exception ("World not initialized.");
            }
#endif
            for (var i = 0; i < _runFixedUpdateSystems.Count; i++) {
                _runFixedUpdateSystems[i].Run ();
                ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        public int CreateEntity () {
#if DEBUG && !ECS_PERF_TEST
            if (!_inited) {
                throw new Exception ("World not initialized.");
            }
#endif
            int entity;
            if (_reservedEntitiesCount > 0) {
                _reservedEntitiesCount--;
                entity = _reservedEntities[_reservedEntitiesCount];
                _entities[entity].IsReserved = false;
            } else {
                entity = _entitiesCount;
                if (_entitiesCount == _entities.Length) {
                    var newEntities = new EcsEntity[_entitiesCount << 1];
                    Array.Copy (_entities, newEntities, _entitiesCount);
                    _entities = newEntities;
                }
                _entities[_entitiesCount++] = new EcsEntity ();
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, entity, -1));
            return entity;
        }

        /// <summary>
        /// Creates new entity and adds component to it.
        /// Faster than CreateEntity() + AddComponent() sequence.
        /// </summary>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public T CreateEntityWith<T> (int componentId = -1) where T : class {
#if DEBUG && !ECS_PERF_TEST
            if (!_inited) {
                throw new Exception ("World not initialized.");
            }
#endif
            int entity;
            if (_reservedEntitiesCount > 0) {
                _reservedEntitiesCount--;
                entity = _reservedEntities[_reservedEntitiesCount];
                _entities[entity].IsReserved = false;
            } else {
                entity = _entitiesCount;
                if (_entitiesCount == _entities.Length) {
                    var newEntities = new EcsEntity[_entitiesCount << 1];
                    Array.Copy (_entities, newEntities, _entitiesCount);
                    _entities = newEntities;
                }
                _entities[_entitiesCount++] = new EcsEntity ();
            }
            return AddComponent<T> (entity, componentId);
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
        public T AddComponent<T> (int entity, int componentId = -1) where T : class {
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

            link.PoolId = (short) componentId;
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
        public void RemoveComponent<T> (int entity, int componentId = -1) where T : class {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveComponent, entity, componentId));
        }

        /// <summary>
        /// Gets component on entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public T GetComponent<T> (int entity, int componentId = -1) where T : class {
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
        public void UpdateComponent<T> (int entity, int componentId = -1) where T : class {
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.UpdateComponent, entity, componentId));
        }

        /// <summary>
        /// Gets component index. Useful for GetComponent() requests as second parameter for performance reason.
        /// </summary>
        public int GetComponentIndex<T> () where T : class {
            return GetComponentIndex (typeof (T));
        }

        /// <summary>
        /// Gets component index. Useful for GetComponent() requests as second parameter for performance reason.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public int GetComponentIndex (Type componentType) {
#if DEBUG && !ECS_PERF_TEST
            if (componentType == null || !componentType.IsClass) {
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
        /// Gets all components on entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="list">List to put results in it.</param>
        public void GetComponents (int entity, IList<object> list) {
            if (list != null) {
                list.Clear ();
                var entityData = _entities[entity];
                for (var i = 0; i < entityData.ComponentsCount; i++) {
                    var link = entityData.Components[i];
                    list.Add (_componentPools[link.PoolId].Items[link.ItemId]);
                }
            }
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
                ActiveEntities = _entitiesCount - _reservedEntitiesCount,
                ReservedEntities = _reservedEntitiesCount,
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
                            if (_reservedEntitiesCount == _reservedEntities.Length) {
                                var newEntities = new int[_reservedEntitiesCount << 1];
                                Array.Copy (_reservedEntities, newEntities, _reservedEntitiesCount);
                                _reservedEntities = newEntities;
                            }
                            _reservedEntities[_reservedEntitiesCount++] = op.Entity;
                        }
                        break;
                    case DelayedUpdate.Op.SafeRemoveEntity:
                        if (!entityData.IsReserved && entityData.ComponentsCount == 0) {
                            entityData.IsReserved = true;
                            if (_reservedEntitiesCount == _reservedEntities.Length) {
                                var newEntities = new int[_reservedEntitiesCount << 1];
                                Array.Copy (_reservedEntities, newEntities, _reservedEntitiesCount);
                                _reservedEntities = newEntities;
                            }
                            _reservedEntities[_reservedEntitiesCount++] = op.Entity;
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
                            UpdateFilters (op.Entity, op.Component, _delayedOpMask, entityData.Mask);
                            DetachComponent (op.Entity, entityData, op.Component);
                            if (entityData.ComponentsCount == 0) {
                                _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, op.Entity, -1));
                            }
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

        [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
        struct DelayedUpdate {
            public enum Op : short {
                RemoveEntity,
                SafeRemoveEntity,
                AddComponent,
                RemoveComponent,
                UpdateComponent
            }
            public Op Type;
            public int Entity;
            public short Component;

            public DelayedUpdate (Op type, int entity, int component) {
                Type = type;
                Entity = entity;
                Component = (short) component;
            }
        }

        [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
        struct ComponentLink {
            public short PoolId;
            public int ItemId;
        }

        sealed class EcsEntity {
            public bool IsReserved;
            public EcsComponentMask Mask = new EcsComponentMask ();
            public int ComponentsCount;
            public ComponentLink[] Components = new ComponentLink[6];
        }
    }
}