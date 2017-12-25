using System;
using System.Collections.Generic;

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
        /// Events processing.
        /// </summary>
        /// <returns></returns>
        readonly EcsEvents _events = new EcsEvents ();

        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld AddSystem (IEcsSystem system) {
            if (_inited) {
                throw new Exception ("Already initialized, cant add new system.");
            }
            _allSystems.Add (system);
            return this;
        }

        /// <summary>
        /// Closes registration for new external data, initialize all registered systems.
        /// </summary>
        public void Initialize () {
            _inited = true;
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                _allSystems[i].Initialize (this);
            }
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

            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                _allSystems[i].Destroy ();
            }

            _events.UnsubscribeAndClearAllEvents ();
            _allSystems.Clear ();
            _componentIds.Clear ();
            _componentPools.Clear ();
            _entities.Clear ();
            _reservedEntityIds.Clear ();
            _filters.Clear ();
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
        public T AddComponent<T> (int entity) where T : class, IEcsComponent {
            var componentId = GetComponentIndex<T> ();
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
        public void RemoveComponent<T> (int entity) where T : class, IEcsComponent {
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveComponent, entity, GetComponentIndex<T> ()));
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
        /// Subscribes to event.
        /// </summary>
        /// <param name="eventData">Event callback.</param>
        public void SubscribeToEvent<T> (Action<T> cb) where T : struct {
            _events.Subscribe (cb);
        }

        /// <summary>
        /// Unsubscribes from event.
        /// </summary>
        /// <param name="eventData">Event callback.</param>
        public void UnsubscribeFromEvent<T> (Action<T> cb) where T : struct {
            _events.Unsubscribe (cb);
        }

        /// <summary>
        /// Publishes event with custom data.
        /// </summary>
        /// <param name="eventData">Event data.</param>
        public void PublishEvent<T> (T eventData) where T : struct {
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
        /// Gets filter for specific component.
        /// </summary>
        public EcsFilter GetFilter<A> () where A : class, IEcsComponent {
            var mask = new EcsComponentMask (GetComponentIndex<A> ());
            return GetFilter (mask);
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        public EcsFilter GetFilter<A, B> () where A : class, IEcsComponent where B : class, IEcsComponent {
            var mask = new EcsComponentMask ();
            mask.SetBit (GetComponentIndex<A> (), true);
            mask.SetBit (GetComponentIndex<B> (), true);
            return GetFilter (mask);
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        public EcsFilter GetFilter<A, B, C> () where A : class, IEcsComponent where B : class, IEcsComponent where C : class, IEcsComponent {
            var mask = new EcsComponentMask ();
            mask.SetBit (GetComponentIndex<A> (), true);
            mask.SetBit (GetComponentIndex<B> (), true);
            mask.SetBit (GetComponentIndex<C> (), true);
            return GetFilter (mask);
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        public EcsFilter GetFilter<A, B, C, D> () where A : class, IEcsComponent where B : class, IEcsComponent where C : class, IEcsComponent where D : class, IEcsComponent {
            var mask = new EcsComponentMask ();
            mask.SetBit (GetComponentIndex<A> (), true);
            mask.SetBit (GetComponentIndex<B> (), true);
            mask.SetBit (GetComponentIndex<C> (), true);
            mask.SetBit (GetComponentIndex<D> (), true);
            return GetFilter (mask);
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        /// <param name="mask">Component selection.</param>
        public EcsFilter GetFilter (EcsComponentMask mask) {
            var i = _filters.Count - 1;
            for (; i >= 0; i--) {
                if (_filters[i].Mask.IsEquals (mask)) {
                    break;
                }
            }
            if (i == -1) {
                i = _filters.Count;
                _filters.Add (new EcsFilter (mask));
            }
            return _filters[i];
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
                Components = _componentIds.Count,
                DelayedUpdates = _delayedUpdates.Count
            };
            return stats;
        }

        /// <summary>
        /// Processes delayed updates. Use carefully!
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
            for (var i = _filters.Count - 1; i >= 0; i--) {
                var isNewMaskCompatible = newMask.IsCompatible (_filters[i].Mask);
                if (oldMask.IsCompatible (_filters[i].Mask)) {
                    if (!isNewMaskCompatible) {
                        _filters[i].Entities.Remove (entity);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        _filters[i].Entities.Add (entity);
                    }
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