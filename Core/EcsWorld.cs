// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
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
        readonly List<DelayedUpdate> _delayedUpdates = new List<DelayedUpdate> (1024);

        /// <summary>
        /// List of requested filters.
        /// </summary>
        readonly List<EcsFilter> _filters = new List<EcsFilter> (64);

        /// <summary>
        /// Temporary buffer for filter updates.
        /// </summary>
        readonly EcsComponentMask _delayedOpMask = new EcsComponentMask ();

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
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, entity, null));
            return entity;
        }

        /// <summary>
        /// Creates new entity and adds component to it.
        /// Faster than CreateEntity() + AddComponent() sequence.
        /// </summary>
        public T CreateEntityWith<T> () where T : class, new () {
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
            return AddComponent<T> (entity);
        }

        /// <summary>
        /// Removes exists entity or throws exception on invalid one.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveEntity (int entity) {
            if (!_entities[entity].IsReserved) {
                _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveEntity, entity, null));
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

            var itemId = -1;
            var i = entityData.ComponentsCount - 1;
            for (; i >= 0; i--) {
                if (entityData.Components[i].Pool == pool) {
                    itemId = entityData.Components[i].ItemId;
                    break;
                }
            }
            if (itemId != -1) {
                throw new Exception (string.Format ("\"{0}\" component already exists on entity {1}", typeof (T).Name, entity));
            }

            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.AddComponent, entity, pool));

            ComponentLink link;
            link.Pool = pool;
            link.ItemId = pool.GetIndex ();
            if (entityData.ComponentsCount == entityData.Components.Length) {
                var newComponents = new ComponentLink[entityData.ComponentsCount << 1];
                Array.Copy (entityData.Components, newComponents, entityData.ComponentsCount);
                entityData.Components = newComponents;
            }
            entityData.Components[entityData.ComponentsCount++] = link;
            return pool.Items[link.ItemId];
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveComponent<T> (int entity) where T : class, new () {
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.RemoveComponent, entity, EcsComponentPool<T>.Instance));
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
            // direct initialization - faster than constructor call.
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
            _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.UpdateComponent, entity, EcsComponentPool<T>.Instance));
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
                Filters = _filters.Count,
                Components = _componentPools.Count,
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
                _delayedOpMask.CopyFrom (entityData.Mask);
                switch (op.Type) {
                    case DelayedUpdate.Op.RemoveEntity:
                        if (!entityData.IsReserved) {
                            while (entityData.ComponentsCount > 0) {
                                var link = entityData.Components[entityData.ComponentsCount - 1];
                                var componentId = link.Pool.GetComponentIndex ();
                                entityData.Mask.SetBit (componentId, false);
                                DetachComponent (op.Entity, entityData, link.Pool);
                                UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                                _delayedOpMask.SetBit (componentId, false);
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
                        var bit = op.Component.GetComponentIndex ();
                        if (!entityData.Mask.GetBit (bit)) {
                            entityData.Mask.SetBit (bit, true);
                            UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                        }
                        break;
                    case DelayedUpdate.Op.RemoveComponent:
                        var bitRemove = op.Component.GetComponentIndex ();
                        if (entityData.Mask.GetBit (bitRemove)) {
                            entityData.Mask.SetBit (bitRemove, false);
                            UpdateFilters (op.Entity, _delayedOpMask, entityData.Mask);
                            DetachComponent (op.Entity, entityData, op.Component);
                            if (entityData.ComponentsCount == 0) {
                                _delayedUpdates.Add (new DelayedUpdate (DelayedUpdate.Op.SafeRemoveEntity, op.Entity, null));
                            }
                        }
                        break;
                    case DelayedUpdate.Op.UpdateComponent:
                        for (var filterId = 0; filterId < _filters.Count; filterId++) {
                            var filter = _filters[filterId];
                            if (_delayedOpMask.IsCompatible (filter.IncludeMask, filter.ExcludeMask)) {
                                filter.RaiseOnEntityUpdated (op.Entity);
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
        /// Removes empty filters. Use carefully, all subscriptions must be removed before!
        /// </summary>
        public void RemoveEmptyFilters () {
            for (var i = _filters.Count - 1; i >= 0; i--) {
                if (_filters[i].Entities.Count == 0) {
                    _filters.RemoveAt (i);
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
            var i = _filters.Count - 1;
            for (; i >= 0; i--) {
                if (this._filters[i].IncludeMask.IsEquals (include) && _filters[i].ExcludeMask.IsEquals (exclude)) {
                    break;
                }
            }
            if (i == -1) {
                i = _filters.Count;
                var filter = new EcsFilter (include, exclude);
                if (shouldBeFilled) {
                    FillFilter (filter);
                }
                _filters.Add (filter);
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
        /// Detaches component from entity and raise OnComponentDetach event.
        /// </summary>
        /// <param name="entityId">Entity Id.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="componentId">Detaching component.</param>
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        void DetachComponent (int entityId, EcsEntity entity, IEcsComponentPool componentId) {
            ComponentLink link;
            for (var i = entity.ComponentsCount - 1; i >= 0; i--) {
                link = entity.Components[i];
                if (link.Pool == componentId) {
                    entity.ComponentsCount--;
                    Array.Copy (entity.Components, i + 1, entity.Components, i, entity.ComponentsCount - i);
                    componentId.RecycleIndex (link.ItemId);
                    return;
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
            for (var i = _filters.Count - 1; i >= 0; i--) {
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
            public IEcsComponentPool Component;

            public DelayedUpdate (Op type, int entity, IEcsComponentPool component) {
                Type = type;
                Entity = entity;
                Component = component;
            }
        }

        struct ComponentLink {
            public IEcsComponentPool Pool;
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