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

    public interface IEcsReadOnlyWorld {
        T GetComponent<T> (int entity) where T : class, new ();
    }

    /// <summary>
    /// Basic ecs world implementation.
    /// </summary>
    public class EcsWorld : IEcsReadOnlyWorld {
        /// <summary>
        /// Filter lists sorted by components for fast UpdateComponent processing.
        /// </summary>
        EcsFilterList[] _componentPoolFilters = new EcsFilterList[512];

        /// <summary>
        /// List of all entities (their components).
        /// </summary>
        EcsEntity[] _entities = new EcsEntity[1024];

        int _entitiesCount;

        /// <summary>
        /// List of removed entities - they can be reused later.
        /// </summary>
        int[] _reservedEntities = new int[256];

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
        /// Registers custom activator for creating instances of specified type.
        /// </summary>
        /// <param name="creator">Custom callback for instance creation.</param>
        public static void RegisterComponentCreator<T> (Func<T> creator) where T : class, new () {
            EcsComponentPool<T>.Instance.SetCreator (creator);
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
            var link = new ComponentLink (pool, pool.RequestNewId ());
            if (entityData.ComponentsCount == entityData.Components.Length) {
                Array.Resize (ref entityData.Components, entityData.ComponentsCount << 1);
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
#if DEBUG
            if (i == -1) {
                throw new Exception (string.Format ("\"{0}\" component not exists on entity {1}", typeof (T).Name, entity));
            }
#endif
            var typeId = EcsComponentPool<T>.Instance.GetComponentTypeIndex ();
            if (typeId < _componentPoolFilters.Length && _componentPoolFilters[typeId] != null) {
                AddDelayedUpdate (DelayedUpdate.Op.UpdateComponent, entity, pool, link.ItemId);
            }
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
                    list.Add (link.Pool.GetExistItemById (link.ItemId));
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
                Components = EcsHelpers.ComponentsCount
            };
            return stats;
        }

        /// <summary>
        /// Manually processes delayed updates. Use carefully!
        /// </summary>
        /// <param name="level">Level of recursion for internal usage, always use 0.</param>
        public void ProcessDelayedUpdates (int level = 0) {
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
                            var componentId = link.Pool.GetComponentTypeIndex ();
                            entityData.Mask.SetBit (componentId, false);
                            var componentToRemove = link.Pool.GetExistItemById (link.ItemId);
#if DEBUG
                            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                                _debugListeners[ii].OnComponentRemoved (op.Entity, componentToRemove);
                            }
#endif
                            UpdateFilters (op.Entity, componentToRemove, _delayedOpMask, entityData.Mask);
                            link.Pool.RecycleById (link.ItemId);
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
                        var bit = op.Pool.GetComponentTypeIndex ();
#if DEBUG
                        if (entityData.Mask.GetBit (bit)) {
                            throw new Exception (string.Format ("Cant add component on entity {0}, already marked as added in mask", op.Entity));
                        }
#endif
                        entityData.Mask.SetBit (bit, true);
                        UpdateFilters (op.Entity, op.Pool.GetExistItemById (op.ComponentId), _delayedOpMask, entityData.Mask);
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        var bitRemove = op.Pool.GetComponentTypeIndex ();
                        var componentInstance = op.Pool.GetExistItemById (op.ComponentId);
#if DEBUG
                        if (!entityData.Mask.GetBit (bitRemove)) {
                            throw new Exception (string.Format ("Cant remove component on entity {0}, marked as not exits in mask", op.Entity));
                        }

                        for (var ii = 0; ii < _debugListeners.Count; ii++) {
                            _debugListeners[ii].OnComponentRemoved (op.Entity, componentInstance);
                        }
#endif
                        entityData.Mask.SetBit (bitRemove, false);
                        UpdateFilters (op.Entity, componentInstance, _delayedOpMask, entityData.Mask);
                        op.Pool.RecycleById (op.ComponentId);
                        if (entityData.ComponentsCount == 0) {
                            AddDelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, op.Entity, null, -1);
                        }
                        break;
                    case DelayedUpdate.Op.UpdateComponent:
                        var filterList = _componentPoolFilters[op.Pool.GetComponentTypeIndex ()];
                        var componentToUpdate = op.Pool.GetExistItemById (op.ComponentId);
                        for (var filterId = 0; filterId < filterList.Count; filterId++) {
                            var filter = filterList.Filters[filterId];
                            if (filter.ExcludeMask.BitsCount == 0 || !_delayedOpMask.IsIntersects (filter.ExcludeMask)) {
                                filter.RaiseOnUpdateEvent (op.Entity, componentToUpdate);
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
                    _delayedUpdatesCount -= iMax;
#if DEBUG
                    if (level > 0) {
                        throw new Exception ("Recursive updating in filters");
                    }
#endif
                    ProcessDelayedUpdates (level + 1);
                }
            }
        }

        /// <summary>
        /// Removes free space from cache, in-use items will be kept.
        /// Useful for free memory when this component will not be used in quantity as before.
        /// </summary>
        public static void ShrinkComponentPool<T> () where T : class, new () {
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

                var filter = new EcsFilter (include, exclude);
                if (shouldBeFilled) {
                    FillFilter (filter);
                }

                if (_filtersCount == _filters.Length) {
                    Array.Resize (ref _filters, _filtersCount << 1);
                }
                _filters[_filtersCount++] = filter;

                for (var bit = 0; bit < include.BitsCount; bit++) {
                    var typeId = include.Bits[bit];
                    if (typeId >= _componentPoolFilters.Length) {
                        Array.Resize (ref _componentPoolFilters, EcsHelpers.GetPowerOfTwoSize (typeId + 1));
                    }
                    var filterList = _componentPoolFilters[typeId];
                    if (filterList == null) {
                        filterList = new EcsFilterList ();
                        _componentPoolFilters[typeId] = filterList;
                    }
                    if (filterList.Count == filterList.Filters.Length) {
                        Array.Resize (ref filterList.Filters, filterList.Count << 1);
                    }
                    filterList.Filters[filterList.Count++] = filter;
                }
            }
            return _filters[i];
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
                    Array.Resize (ref _entities, _entitiesCount << 1);
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
                Array.Resize (ref _delayedUpdates, _delayedUpdatesCount << 1);
            }
            _delayedUpdates[_delayedUpdatesCount++] = new DelayedUpdate (type, entity, component, componentId);
        }

        /// <summary>
        /// Puts entity to pool (reserved list) to reuse later.
        /// </summary>
        /// <param name="entity">Entity Id.</param>
        /// <param name="entityData">EcsEntity instance.</param>
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        void ReserveEntity (int entity, EcsEntity entityData) {
            entityData.IsReserved = true;
            if (_reservedEntitiesCount == _reservedEntities.Length) {
                Array.Resize (ref _reservedEntities, _reservedEntitiesCount << 1);
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
                if (!entity.IsReserved && entity.Mask.IsCompatible (filter)) {
                    if (filter.Entities.Length == filter.EntitiesCount) {
                        Array.Resize (ref filter.Entities, filter.EntitiesCount << 1);
                    }
                    filter.Entities[filter.EntitiesCount++] = i;
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
        void UpdateFilters (int entity, object component, EcsComponentMask oldMask, EcsComponentMask newMask) {
            for (var i = _filtersCount - 1; i >= 0; i--) {
                var filter = _filters[i];
                var isNewMaskCompatible = newMask.IsCompatible (filter);
                if (oldMask.IsCompatible (filter)) {
                    if (!isNewMaskCompatible) {
#if DEBUG
                        var ii = filter.EntitiesCount - 1;
                        for (; ii >= 0; ii--) {
                            if (filter.Entities[ii] == entity) {
                                break;
                            }
                        }
                        if (ii == -1) {
                            throw new Exception (
                                string.Format ("Something wrong - entity {0} should be in filter {1}, but not exits.", entity, filter));
                        }
#endif
                        filter.RaiseOnRemoveEvent (entity, component);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        filter.RaiseOnAddEvent (entity, component);
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

        sealed class EcsFilterList {
            public EcsFilter[] Filters = new EcsFilter[4];
            public int Count;
        }

        sealed class EcsEntity {
            public bool IsReserved;
            public EcsComponentMask Mask = new EcsComponentMask ();
            public int ComponentsCount;
            public ComponentLink[] Components = new ComponentLink[8];
        }
    }
}