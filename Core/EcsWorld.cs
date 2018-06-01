// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;

#if ENABLE_IL2CPP
// Unity IL2CPP performance optimization attribute.
namespace Unity.IL2CPP.CompilerServices {
    enum Option {
        NullChecks = 1,
        ArrayBoundsChecks = 2
    }

    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    class Il2CppSetOptionAttribute : Attribute {
        public Option Option { get; private set; }
        public object Value { get; private set; }

        public Il2CppSetOptionAttribute (Option option, object value) { Option = option; Value = value; }
    }
}
#endif

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
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public class EcsWorld : IEcsReadOnlyWorld, IDisposable {
        /// <summary>
        /// Last created instance of EcsWorld.
        /// Can be force reassigned manually when multiple worlds in use.
        /// </summary>
        public static EcsWorld Active = null;

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

        public EcsWorld () {
            Active = this;
        }

        public void Dispose () {
            if (this == Active) {
                Active = null;
            }
            for (var i = 0; i < _entitiesCount; i++) {
                // already reserved entities cant contains components.
                if (_entities[i].ComponentsCount > 0) {
                    var entity = _entities[i];
                    for (var ii = 0; ii < entity.ComponentsCount; ii++) {
                        entity.Components[ii].Pool.RecycleById (entity.Components[ii].ItemId);
                    }
                }
            }
            // any next usage of this EcsWorld instance will throw exception.
            _entities = null;
            _entitiesCount = 0;
            _filters = null;
            _filtersCount = 0;
            _reservedEntities = null;
            _reservedEntitiesCount = 0;
            _delayedUpdates = null;
            _delayedUpdatesCount = 0;
        }

#if DEBUG
        /// <summary>
        /// List of all debug listeners.
        /// </summary>
        readonly System.Collections.Generic.List<IEcsWorldDebugListener> _debugListeners = new System.Collections.Generic.List<IEcsWorldDebugListener> (4);

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
            for (var i = 0; i < entityData.ComponentsCount; i++) {
                if (entityData.Components[i].Pool == pool) {
                    return pool.Items[entityData.Components[i].ItemId];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all components on entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="list">List to put results in it. if null - will be created.</param>
        /// <returns>Amount of components in list.</returns>
        public int GetComponents (int entity, ref object[] list) {
            var entityData = _entities[entity];
            var count = entityData.ComponentsCount;
            if (list == null || list.Length < count) {
                list = new object[entityData.ComponentsCount];
            }
            for (var i = 0; i < count; i++) {
                var link = entityData.Components[i];
                list[i] = link.Pool.GetExistItemById (link.ItemId);
            }
            return count;
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
#if DEBUG
                            var componentToRemove = link.Pool.GetExistItemById (link.ItemId);
                            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                                _debugListeners[ii].OnComponentRemoved (op.Entity, componentToRemove);
                            }
#endif
                            UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
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
                        UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        var bitRemove = op.Pool.GetComponentTypeIndex ();
#if DEBUG
                        if (!entityData.Mask.GetBit (bitRemove)) {
                            throw new Exception (string.Format ("Cant remove component on entity {0}, marked as not exits in mask", op.Entity));
                        }

                        var componentInstance = op.Pool.GetExistItemById (op.ComponentId);
                        for (var ii = 0; ii < _debugListeners.Count; ii++) {
                            _debugListeners[ii].OnComponentRemoved (op.Entity, componentInstance);
                        }
#endif
                        entityData.Mask.SetBit (bitRemove, false);
                        UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                        op.Pool.RecycleById (op.ComponentId);
                        if (entityData.ComponentsCount == 0) {
                            AddDelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, op.Entity, null, -1);
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
        /// Gets filter with specific include / exclude masks.
        /// </summary>
        public T GetFilter<T> () where T : EcsFilter {
            return GetFilter (typeof (T)) as T;
        }

        /// <summary>
        /// Gets filter with specific include / exclude masks.
        /// </summary>
        /// <param name="filterType">Type of filter.</param>
        public EcsFilter GetFilter (Type filterType) {
#if DEBUG
            if (filterType == null) {
                throw new ArgumentNullException ("filterType");
            }
            if (!filterType.IsSubclassOf (typeof (EcsFilter))) {
                throw new ArgumentException (string.Format ("Invalid filter-type: {0}", filterType));
            }
#endif
            var i = _filtersCount - 1;
            for (; i >= 0; i--) {
                if (this._filters[i].GetType () == filterType) {
                    break;
                }
            }
            if (i == -1) {
                i = _filtersCount;

                var filter = Activator.CreateInstance (filterType, true) as EcsFilter;
                filter.SetWorld (this);
#if DEBUG
                for (var j = 0; j < _filtersCount; j++) {
                    if (_filters[j].IncludeMask.IsEquals (filter.IncludeMask) &&
                        _filters[j].ExcludeMask.IsEquals (filter.ExcludeMask)) {
                        throw new Exception (
                            string.Format ("Duplicate filter type \"{0}\": filter type \"{1}\" already has same types in different order.",
                                filterType, _filters[j].GetType ()));
                    }
                }
#endif
                if (_filtersCount == _filters.Length) {
                    Array.Resize (ref _filters, _filtersCount << 1);
                }

                _filters[_filtersCount++] = filter;
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
                        filter.RaiseOnRemoveEvent (entity);
                    }
                } else {
                    if (isNewMaskCompatible) {
                        filter.RaiseOnAddEvent (entity);
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
                RemoveComponent
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
            public ComponentLink[] Components = new ComponentLink[8];
        }
    }
}