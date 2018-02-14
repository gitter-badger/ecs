// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
#if DEBUG
    /// <summary>
    /// Debug interface for world events processing.
    /// </summary>
    public interface IEcsWorldDebugListener {
        void OnEntityCreated (int entity);
        void OnEntityRemoved (int entity);
        void OnComponentAdded (int entity, object component);
        void OnComponentRemoved (int entity, object component);
    }
#endif

    /// <summary>
    /// Basic ecs world implementation.
    /// </summary>
    public class EcsWorld {
        /// <summary>
        /// Component pools, just for correct cleanup behaviour on Destroy.
        /// </summary>
        readonly List<IEcsComponentPool> _componentPools = new List<IEcsComponentPool> (EcsComponentMask.BitsCount);

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
        DelayedUpdate[] _delayedUpdates = new DelayedUpdate[1024];

        int _delayedUpdatesCount;

        /// <summary>
        /// List of requested filters.
        /// </summary>
        EcsFilter[] _filters = new EcsFilter[64];

        int _filtersCount;

        /// <summary>
        /// Temporary buffer for filter updates.
        /// </summary>
        readonly EcsComponentMask _delayedOpMask = new EcsComponentMask ();

#if DEBUG
        /// <summary>
        /// List of all debug listeners.
        /// </summary>
        readonly List<IEcsWorldDebugListener> _debugListeners = new List<IEcsWorldDebugListener> (4);

        /// <summary>
        /// Adds external event listener.
        /// </summary>
        /// <param name="observer">Event listener.</param>
        public void AddDebugListener (IEcsWorldDebugListener observer) {
            if (_debugListeners.Contains (observer)) {
                throw new Exception ("Listener already exists");
            }
            _debugListeners.Add (observer);
        }

        /// <summary>
        /// Removes external event listener.
        /// </summary>
        /// <param name="observer">Event listener.</param>
        public void RemoveDebugListener (IEcsWorldDebugListener observer) {
            _debugListeners.Remove (observer);
        }
#endif

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld RegisterComponentCreator<T> (Func<T> creator) where T : class, new () {
            var pool = EcsComponentPool<T>.Instance;
            if (pool.World != this) {
                pool.ConnectToWorld (this, _componentPools.Count);
                _componentPools.Add (pool);
            }
            pool.SetCreator (creator);
            return this;
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        public int CreateEntity () {
            return CreateEntityInternal (true);
        }

        /// <summary>
        /// Creates new entity and adds component to it.
        /// Faster than CreateEntity() + AddComponent() sequence.
        /// </summary>
        public T CreateEntityWith<T> () where T : class, new () {
            return AddComponent<T> (CreateEntityInternal (false));
        }

        /// <summary>
        /// Removes exists entity or throws exception on invalid one.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveEntity (int entity) {
            if (!_entities[entity].IsReserved) {
                AddDelayedUpdate (DelayedUpdate.Op.RemoveEntity, entity, null, -1);
            }
        }

        /// <summary>
        /// Adds component to entity. Will throw exception if component already exists.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public T AddComponent<T> (int entity) where T : class, new () {
            var entityData = _entities[entity];
            var pool = EcsComponentPool<T>.Instance;
            if (pool.World != this) {
                pool.ConnectToWorld (this, _componentPools.Count);
                _componentPools.Add (pool);
            }
#if DEBUG
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                if (entityData.Components[i].Pool == pool) {
                    break;
                }
            }
            if (i != -1) {
                throw new Exception (string.Format ("\"{0}\" component already exists on entity {1}", typeof (T).Name, entity));
            }
#endif
            var link = new ComponentLink (pool, pool.GetIndex ());
            if (entityData.ComponentsCount == entityData.Components.Length) {
                var newComponents = new ComponentLink[entityData.ComponentsCount << 1];
                Array.Copy (entityData.Components, newComponents, entityData.ComponentsCount);
                entityData.Components = newComponents;
            }
            entityData.Components[entityData.ComponentsCount++] = link;

            AddDelayedUpdate (DelayedUpdate.Op.AddComponent, entity, pool, link.ItemId);
#if DEBUG
            var component = pool.Items[link.ItemId];
            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                _debugListeners[ii].OnComponentAdded (entity, component);
            }
#endif
            return pool.Items[link.ItemId];
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveComponent<T> (int entity) where T : class, new () {
            var entityData = _entities[entity];
            var pool = EcsComponentPool<T>.Instance;
#if DEBUG
            if (pool.World != this) {
                throw new Exception (string.Format ("Component pool of {0} type not connected to world", typeof (T).Name));
            }
#endif
            ComponentLink link;
            link.ItemId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                link = entityData.Components[i];
                if (link.Pool == pool) {
                    break;
                }
            }
#if DEBUG
            if (i == -1) {
                throw new Exception (string.Format ("\"{0}\" component not exists on entity {1}", typeof (T).Name, entity));
            }
#endif
            AddDelayedUpdate (DelayedUpdate.Op.RemoveComponent, entity, pool, link.ItemId);
            entityData.ComponentsCount--;
            Array.Copy (entityData.Components, i + 1, entityData.Components, i, entityData.ComponentsCount - i);
        }

        /// <summary>
        /// Gets component on entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public T GetComponent<T> (int entity) where T : class, new () {
            var entityData = _entities[entity];
            var pool = EcsComponentPool<T>.Instance;
            ComponentLink link;
            link.ItemId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                link = entityData.Components[i];
                if (link.Pool == pool) {
                    break;
                }
            }
            return i != -1 ? pool.Items[link.ItemId] : null;
        }

        /// <summary>
        /// Updates component on entity - OnUpdated event will be raised on compatible filters.
        /// </summary>
        /// <param name="entity">Entity.</param>
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void UpdateComponent<T> (int entity) where T : class, new () {
#if DEBUG
            var entityData = _entities[entity];
            var pool = EcsComponentPool<T>.Instance;
            ComponentLink link;
            link.ItemId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                link = entityData.Components[i];
                if (link.Pool == pool) {
                    break;
                }
            }
            if (i == -1) {
                throw new Exception (string.Format ("\"{0}\" component not exists on entity {1}", typeof (T).Name, entity));
            }
#endif
            AddDelayedUpdate (DelayedUpdate.Op.UpdateComponent, entity, null, -1);
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
                    list.Add (link.Pool.GetItem (link.ItemId));
                }
            }
        }

        /// <summary>
        /// Gets stats of internal data.
        /// </summary>
        public EcsWorldStats GetStats () {
            var stats = new EcsWorldStats () {
                ActiveEntities = _entitiesCount - _reservedEntitiesCount,
                ReservedEntities = _reservedEntitiesCount,
                Filters = _filtersCount,
                Components = _componentPools.Count
            };
            return stats;
        }

        /// <summary>
        /// Manually processes delayed updates. Use carefully!
        /// </summary>
        public void ProcessDelayedUpdates () {
            var iMax = _delayedUpdatesCount;
            for (var i = 0; i < iMax; i++) {
                var op = _delayedUpdates[i];
                var entityData = _entities[op.Entity];
                _delayedOpMask.CopyFrom (entityData.Mask);
                switch (op.Type) {
                    case DelayedUpdate.Op.RemoveEntity:
#if DEBUG
                        if (entityData.IsReserved) {
                            throw new Exception (string.Format ("Entity {0} already removed", op.Entity));
                        }
#endif
                        while (entityData.ComponentsCount > 0) {
                            var link = entityData.Components[entityData.ComponentsCount - 1];
                            var componentId = link.Pool.GetComponentIndex ();
                            entityData.Mask.SetBit (componentId, false);
#if DEBUG
                            var componentToRemove = link.Pool.GetItem (link.ItemId);
                            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                                _debugListeners[ii].OnComponentRemoved (op.Entity, componentToRemove);
                            }
#endif
                            link.Pool.RecycleIndex (link.ItemId);
                            UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                            _delayedOpMask.SetBit (componentId, false);
                            entityData.ComponentsCount--;
                        }
                        ReserveEntity (op.Entity, entityData);
                        break;
                    case DelayedUpdate.Op.SafeRemoveEntity:
                        if (!entityData.IsReserved && entityData.ComponentsCount == 0) {
                            ReserveEntity (op.Entity, entityData);
                        }
                        break;
                    case DelayedUpdate.Op.AddComponent:
                        var bit = op.Pool.GetComponentIndex ();
#if DEBUG
                        if (entityData.Mask.GetBit (bit)) {
                            throw new Exception (string.Format ("Cant add component on entity {0}, already marked as added in mask", op.Entity));
                        }
#endif
                        entityData.Mask.SetBit (bit, true);
                        UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        var bitRemove = op.Pool.GetComponentIndex ();
#if DEBUG
                        if (!entityData.Mask.GetBit (bitRemove)) {
                            throw new Exception (string.Format ("Cant remove component on entity {0}, marked as not exits in mask", op.Entity));
                        }
                        var componentInstance = op.Pool.GetItem (op.ComponentId);
                        for (var ii = 0; ii < _debugListeners.Count; ii++) {
                            _debugListeners[ii].OnComponentRemoved (op.Entity, componentInstance);
                        }
#endif
                        entityData.Mask.SetBit (bitRemove, false);
                        UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                        op.Pool.RecycleIndex (op.ComponentId);
                        if (entityData.ComponentsCount == 0) {
                            AddDelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, op.Entity, null, -1);
                        }
                        break;
                    case DelayedUpdate.Op.UpdateComponent:
                        for (var filterId = 0; filterId < _filtersCount; filterId++) {
                            var filter = _filters[filterId];
                            if (_delayedOpMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                                filter.RaiseOnEntityUpdated (op.Entity);
                            }
                        }
                        break;
                }
            }
            if (iMax > 0) {
                if (_delayedUpdatesCount == iMax) {
                    _delayedUpdatesCount = 0;
                } else {
                    Array.Copy (_delayedUpdates, iMax, _delayedUpdates, 0, _delayedUpdatesCount - iMax);
                    ProcessDelayedUpdates ();
                }
            }
        }

        /// <summary>
        /// Removes empty filters. Use carefully, all subscriptions must be removed before!
        /// </summary>
        public void RemoveEmptyFilters () {
            for (var i = _filtersCount - 1; i >= 0; i--) {
                if (_filters[i].Entities.Count == 0) {
                    _filtersCount--;
                    Array.Copy (_filters, i + 1, _filters, i, _filtersCount - i);
                }
            }
        }

        /// <summary>
        /// Removes free space from cache, in-use items will be kept.
        /// Useful for free memory when this component will not be used in quantity as before.
        /// </summary>
        public void ShrinkComponentPool<T> () where T : class, new () {
            EcsComponentPool<T>.Instance.Shrink ();
        }

        /// <summary>
        /// Gets filter for specific components.
        /// </summary>
        /// <param name="include">Component mask for required components.</param>
        /// <param name="include">Component mask for denied components.</param>
        /// <param name="shouldBeFilled">New filter should be filled with compatible entities on creation.</param>
        internal EcsFilter GetFilter (EcsComponentMask include, EcsComponentMask exclude, bool shouldBeFilled) {
#if DEBUG
            if (include == null) {
                throw new ArgumentNullException ("include");
            }
            if (exclude == null) {
                throw new ArgumentNullException ("exclude");
            }
#endif
            var i = _filtersCount - 1;
            for (; i >= 0; i--) {
                if (this._filters[i].IncludeMask.IsEquals (include) && _filters[i].ExcludeMask.IsEquals (exclude)) {
                    break;
                }
            }
            if (i == -1) {
                i = _filtersCount;
                if (_filtersCount == _filters.Length) {
                    var newFilters = new EcsFilter[_filtersCount << 1];
                    Array.Copy (_filters, newFilters, _filtersCount);
                    _filters = newFilters;
                }
                var filter = new EcsFilter (include, exclude);
                if (shouldBeFilled) {
                    FillFilter (filter);
                }
                _filters[_filtersCount++] = filter;
            }
            return _filters[i];
        }

        /// <summary>
        /// Returns component type index from connected pools list.
        /// If instance not connected - process connection.
        /// </summary>
        /// <param name="poolInstance">Components pool.</param>
        internal int GetComponentPoolIndex (IEcsComponentPool poolInstance) {
            if (poolInstance == null) {
                throw new ArgumentNullException ();
            }
            var idx = _componentPools.IndexOf (poolInstance);
            if (idx == -1) {
                idx = _componentPools.Count;
                poolInstance.ConnectToWorld (this, idx);
                _componentPools.Add (poolInstance);
            }
            return idx;
        }

        /// <summary>
        /// Create entity with support of re-using reserved instances.
        /// </summary>
        /// <param name="addSafeRemove">Add delayed command for proper removing entities without components.</param>
        int CreateEntityInternal (bool addSafeRemove) {
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
            if (addSafeRemove) {
                AddDelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, entity, null, -1);
            }
#if DEBUG
            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                _debugListeners[ii].OnEntityCreated (entity);
            }
#endif
            return entity;
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        void AddDelayedUpdate (DelayedUpdate.Op type, int entity, IEcsComponentPool component, int componentId) {
            if (_delayedUpdatesCount == _delayedUpdates.Length) {
                var newDelayedUpdates = new DelayedUpdate[_delayedUpdatesCount << 1];
                Array.Copy (_delayedUpdates, newDelayedUpdates, _delayedUpdatesCount);
                _delayedUpdates = newDelayedUpdates;
            }
            _delayedUpdates[_delayedUpdatesCount++] = new DelayedUpdate (type, entity, component, componentId);
        }

        /// <summary>
        /// Puts entity to pool (reserved list) to reuse later.
        /// </summary>
        /// <param name="entity">Entity Id.</param>
        /// <param name="entityData">EcsEntity instance.</param>
        void ReserveEntity (int entity, EcsEntity entityData) {
            entityData.IsReserved = true;
            if (_reservedEntitiesCount == _reservedEntities.Length) {
                var newEntities = new int[_reservedEntitiesCount << 1];
                Array.Copy (_reservedEntities, newEntities, _reservedEntitiesCount);
                _reservedEntities = newEntities;
            }
            _reservedEntities[_reservedEntitiesCount++] = entity;
#if DEBUG
            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                _debugListeners[ii].OnEntityRemoved (entity);
            }
#endif
        }

        /// <summary>
        /// Fills filter with compatible entities.
        /// </summary>
        /// <param name="filter">Filter.</param>
        void FillFilter (EcsFilter filter) {
            for (var i = 0; i < _entitiesCount; i++) {
                var entity = _entities[i];
                if (!entity.IsReserved && entity.Mask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                    filter.Entities.Add (i);
                }
            }
        }

        /// <summary>
        /// Updates all filters for changed component mask.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="oldMask">Old component state.</param>
        /// <param name="newMask">New component state.</param>
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        void UpdateFilters (int entity, EcsComponentMask oldMask, EcsComponentMask newMask) {
            for (var i = _filtersCount - 1; i >= 0; i--) {
                var filter = _filters[i];
                var isNewMaskCompatible = newMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask);
                if (oldMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                    if (!isNewMaskCompatible) {
#if DEBUG
                        if (filter.Entities.IndexOf (entity) == -1) {
                            throw new Exception (
                                string.Format ("Something wrong - entity {0} should be in filter {1}, but not exits.", entity, filter));
                        }
#endif
                        filter.Entities.Remove (entity);
                        filter.RaiseOnEntityRemoved (entity);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        filter.Entities.Add (entity);
                        filter.RaiseOnEntityAdded (entity);
                    }
                }
            }
        }

        [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        struct DelayedUpdate {
            public enum Op : byte {
                RemoveEntity,
                SafeRemoveEntity,
                AddComponent,
                RemoveComponent,
                UpdateComponent
            }
            public Op Type;
            public int Entity;
            public IEcsComponentPool Pool;
            public int ComponentId;

            public DelayedUpdate (Op type, int entity, IEcsComponentPool component, int componentId) {
                Type = type;
                Entity = entity;
                Pool = component;
                ComponentId = componentId;
            }
        }

        struct ComponentLink {
            public IEcsComponentPool Pool;
            public int ItemId;

            public ComponentLink (IEcsComponentPool pool, int itemId) {
                Pool = pool;
                ItemId = itemId;
            }
        }

        sealed class EcsEntity {
            public bool IsReserved;
            public EcsComponentMask Mask = new EcsComponentMask ();
            public int ComponentsCount;
            public ComponentLink[] Components = new ComponentLink[6];
        }
    }
}