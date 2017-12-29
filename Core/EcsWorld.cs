// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    public sealed class EcsWorld {
        public delegate void OnComponentChangeHandler (int entity, IEcsComponent component);

        /// <summary>
        /// Raises on component attaching to entity.
        /// </summary>
        public event OnComponentChangeHandler OnComponentAttach = delegate { };

        /// <summary>
        /// Raises on component detaching from entity.
        /// </summary>
        public event OnComponentChangeHandler OnComponentDetach = delegate { };

        /// <summary>
        /// All registered systems.
        /// </summary>
        readonly List<IEcsSystem> _allSystems = new List<IEcsSystem> (64);

        /// <summary>
        /// Dictionary for fast search component (type.hashcode) -> type id.
        /// </summary>
        readonly Dictionary<int, int> _componentIds = new Dictionary<int, int> (64);

        /// <summary>
        /// Pools list for recycled component instances.
        /// </summary>
        readonly Dictionary<int, EcsComponentPool> _componentPools = new Dictionary<int, EcsComponentPool> (64);

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
        readonly List<DelayedUpdate> _delayedUpdates = new List<DelayedUpdate> (64);

        /// <summary>
        /// List of requested filters.
        /// </summary>
        readonly List<EcsFilter> _filters = new List<EcsFilter> (64);

        /// <summary>
        /// List of requested react filters.
        /// </summary>
        readonly List<EcsFilter> _reactFilters = new List<EcsFilter> (64);

        /// <summary>
        /// Events processing.
        /// </summary>
        /// <returns></returns>
        readonly EcsEvents _events = new EcsEvents ();

#if DEBUG
        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;

        /// <summary>
        /// In react system update loop.
        /// </summary>
        bool _inReact;
#endif

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld AddSystem (IEcsSystem system) {
#if DEBUG
            if (_inited) {
                throw new Exception ("Already initialized, cant add new system.");
            }
#endif
            _allSystems.Add (system);
            EcsInjections.Inject (this, system);
            return this;
        }

        /// <summary>
        /// Closes registration for new external data, initialize all registered systems.
        /// </summary>
        public void Initialize () {
#if DEBUG
            _inited = true;
#endif
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var initSystem = _allSystems[i] as IEcsInitSystem;
                if (initSystem != null) {
                    initSystem.Initialize ();
                }
            }
            ProcessDelayedUpdates ();
            ProcessReactSystems ();
            ProcessDelayedUpdates ();
        }

        /// <summary>
        /// Destroys all registered external data, full cleanup for internal data.
        /// </summary>
        public void Destroy () {
            for (int i = 0, iMax = _entities.Count; i < iMax; i++) {
                RemoveEntity (i);
            }
            ProcessDelayedUpdates ();
            ProcessReactSystems ();
            ProcessDelayedUpdates ();

            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                var initSystem = _allSystems[i] as IEcsInitSystem;
                if (initSystem != null) {
                    initSystem.Destroy ();
                }
            }

            _events.UnsubscribeAndClearAllEvents ();
            _allSystems.Clear ();
            _componentIds.Clear ();
            _componentPools.Clear ();
            _entities.Clear ();
            _reservedEntityIds.Clear ();
            _filters.Clear ();
            _reactFilters.Clear ();
        }

        /// <summary>
        /// Processes all IEcsUpdateSystem systems.
        /// </summary>
        public void Update () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.Update ();
                }
            }
            ProcessDelayedUpdates ();
            ProcessReactSystems ();
            ProcessDelayedUpdates ();
        }

        /// <summary>
        /// Processes all IEcsFixedUpdateSystem systems.
        /// </summary>
        public void FixedUpdate () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsFixedUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.FixedUpdate ();
                }
            }
            ProcessDelayedUpdates ();
            ProcessReactSystems ();
            ProcessDelayedUpdates ();
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
            if (entityData.Mask.GetBit (componentId)) {
                return entityData.Components[componentId] as T;
            }
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.AddComponent, entity, componentId));

            EcsComponentPool pool;
            if (!_componentPools.TryGetValue (componentId, out pool)) {
                pool = new EcsComponentPool (typeof (T));
                _componentPools[componentId] = pool;
            }
            var component = pool.Get () as T;

            while (entityData.ComponentsCount <= componentId) {
                entityData.Components.Add (null);
                entityData.ComponentsCount++;
            }
            entityData.Components[componentId] = component;
            return component;
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
            return componentId < entityData.ComponentsCount ? entityData.Components[componentId] as T : null;
        }

        /// <summary>
        /// Marks component as changed for process it at react systems.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Component index. If equals to "-1" - will try to find registered type.</param>
        public void MarkComponentAsChanged<T> (int entity, int componentId = -1) where T : class, IEcsComponent {
#if DEBUG
            if (_inReact) {
                throw new Exception ("Cant mark component as changed in react system.");
            }
#endif
            if (componentId == -1) {
                componentId = GetComponentIndex<T> ();
            }
            // cant check mask - AddComponent mask fix can be delayed if called right before this method.
            var entityData = _entities[entity];
            if (componentId >= entityData.ComponentsCount || entityData.Components[componentId] == null) {
                throw new Exception (string.Format ("Component {0} not exists on entity {1}.", typeof (T).Name, entity));
            }
            for (var i = _reactFilters.Count - 1; i >= 0; i--) {
                var filter = _reactFilters[i];
                if (filter.IncludeMask.GetBit (componentId) && filter.Entities.IndexOf (entity) == -1) {
                    filter.Entities.Add (entity);
                }
            }
        }

        /// <summary>
        /// Subscribes to event.
        /// </summary>
        /// <param name="eventData">Event callback.</param>
        public void AddEventAction<T> (Action<T> cb) where T : struct {
            _events.Subscribe (cb);
        }

        /// <summary>
        /// Unsubscribes from event.
        /// </summary>
        /// <param name="eventData">Event callback.</param>
        public void RemoveEventAction<T> (Action<T> cb) where T : struct {
            _events.Unsubscribe (cb);
        }

        /// <summary>
        /// Publishes event with custom data.
        /// </summary>
        /// <param name="eventData">Event data.</param>
        public void SendEvent<T> (T eventData) where T : struct {
            _events.Publish (eventData);
        }

        /// <summary>
        /// Gets component index. Useful for GetComponent() requests as second parameter for performance reason.
        /// </summary>
        public int GetComponentIndex<T> () where T : class, IEcsComponent {
            int retVal;
            var type = typeof (T).GetHashCode ();
            if (!_componentIds.TryGetValue (type, out retVal)) {
                retVal = _componentIds.Count;
                _componentIds[type] = retVal;
            }
            return retVal;
        }

        /// <summary>
        /// Gets component index. Slower than generic version, use carefully!
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public int GetComponentIndex (Type componentType) {
            if (componentType == null || !typeof (IEcsComponent).IsAssignableFrom (componentType) || !componentType.IsClass) {
                throw new Exception ("Invalid component type");
            }
            int retVal;
            var type = componentType.GetHashCode ();
            if (!_componentIds.TryGetValue (type, out retVal)) {
                retVal = _componentIds.Count;
                _componentIds[type] = retVal;
            }
            return retVal;
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        /// <param name="include">Component mask for required components.</param>
        /// <param name="include">Component mask for denied components.</param>
        internal EcsFilter GetFilter (EcsComponentMask include, EcsComponentMask exclude, bool isReact = false) {
            var list = isReact ? _reactFilters : _filters;
            var i = list.Count - 1;
            for (; i >= 0; i--) {
                if (_filters[i].IncludeMask.IsEquals (include) && list[i].ExcludeMask.IsEquals (exclude)) {
                    break;
                }
            }
            if (i == -1) {
#if DEBUG
                if (_inited) {
                    throw new Exception ("Already initialized, cant add new filter.");
                }
#endif
                i = list.Count;
                list.Add (new EcsFilter (include, exclude));
            }
            return list[i];
        }

        /// <summary>
        /// Gets all components on entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="list">List to put results in it.</param>
        public void GetComponents (int entity, IList<IEcsComponent> list) {
            if (list != null) {
                list.Clear ();
                var entityData = _entities[entity];
                var componentId = 0;
                var mask = entityData.Mask;
                while (!mask.IsEmpty ()) {
                    if (mask.GetBit (componentId)) {
                        mask.SetBit (componentId, false);
                        list.Add (entityData.Components[componentId]);
                    }
                    componentId++;
                }
            }
        }

        /// <summary>
        /// Gets stats of internal data.
        /// </summary>
        public EcsWorldStats GetStats () {
            var stats = new EcsWorldStats () {
                AllSystems = _allSystems.Count,
                AllEntities = _entities.Count,
                ReservedEntities = _reservedEntityIds.Count,
                Filters = _filters.Count,
                ReactFilters = _reactFilters.Count,
                Components = _componentIds.Count,
                DelayedUpdates = _delayedUpdates.Count
            };
            return stats;
        }

        /// <summary>
        /// Manually processes delayed updates. Use carefully!
        /// </summary>
        public void ProcessDelayedUpdates () {
            var iMax = _delayedUpdates.Count;
            for (var i = 0; i < iMax; i++) {
                var op = _delayedUpdates[i];
                var entityData = _entities[op.Entity];
                var oldMask = entityData.Mask;
                switch (op.Type) {
                    case DelayedUpdate.Op.RemoveEntity:
                        if (!entityData.IsReserved) {
                            var componentId = 0;
                            var empty = new EcsComponentMask ();
                            while (!entityData.Mask.IsEmpty ()) {
                                if (entityData.Mask.GetBit (componentId)) {
                                    entityData.Mask.SetBit (componentId, false);
                                    DetachComponent (op.Entity, entityData, componentId);
                                }
                                componentId++;
                            }
                            UpdateFilters (op.Entity, oldMask, empty);
                            entityData.IsReserved = true;
                            _reservedEntityIds.Add (op.Entity);
                        }
                        break;
                    case DelayedUpdate.Op.AddComponent:
                        if (!entityData.Mask.GetBit (op.Component)) {
                            entityData.Mask.SetBit (op.Component, true);
                            OnComponentAttach (op.Entity, entityData.Components[op.Component]);
                            UpdateFilters (op.Entity, oldMask, entityData.Mask);
                        }
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        if (entityData.Mask.GetBit (op.Component)) {
                            entityData.Mask.SetBit (op.Component, false);
                            DetachComponent (op.Entity, entityData, op.Component);
                            UpdateFilters (op.Entity, oldMask, entityData.Mask);
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
        /// Manually processes react systems. Use carefully!
        /// </summary>
        public void ProcessReactSystems () {
#if DEBUG
            _inReact = true;
#endif
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var reactSystem = _allSystems[i] as IEcsReactSystem;
                if (reactSystem != null) {
                    reactSystem.React ();
                }
            }
            for (var i = _reactFilters.Count - 1; i >= 0; i--) {
                _reactFilters[i].Entities.Clear ();
            }
#if DEBUG
            _inReact = false;
#endif
        }

        /// <summary>
        /// Detaches component from entity and raise OnComponentDetach event.
        /// </summary>
        /// <param name="entityId">Entity Id.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Detaching component.</param>
        void DetachComponent (int entityId, EcsEntity entity, int componentId) {
            var comp = entity.Components[componentId];
            entity.Components[componentId] = null;
            OnComponentDetach (entityId, comp);
            _componentPools[componentId].Recycle (comp);
        }

        /// <summary>
        /// Updates all filters for changed component mask.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="oldMask">Old component state.</param>
        /// <param name="newMask">New component state.</param>
        void UpdateFilters (int entity, EcsComponentMask oldMask, EcsComponentMask newMask) {
            EcsFilter filter;
            for (var i = _filters.Count - 1; i >= 0; i--) {
                filter = _filters[i];
                var isNewMaskCompatible = newMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask);
                if (oldMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                    if (!isNewMaskCompatible) {
                        filter.Entities.Remove (entity);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        filter.Entities.Add (entity);
                    }
                }
            }
            // react filters cleanup.
            for (var i = _reactFilters.Count - 1; i >= 0; i--) {
                filter = _reactFilters[i];
                if (oldMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask) &&
                    !newMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                    filter.Entities.Remove (entity);
                }
            }
        }

        struct DelayedUpdate {
            public enum Op {
                RemoveEntity,
                AddComponent,
                RemoveComponent
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

        sealed class EcsEntity {
            public bool IsReserved;
            public EcsComponentMask Mask = new EcsComponentMask ();
            public int ComponentsCount;
            public readonly List<IEcsComponent> Components = new List<IEcsComponent> (8);
        }
    }
}