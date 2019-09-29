// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Leopotam.Ecs {
    /// <summary>
    /// Ecs data context.
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public class EcsWorld {
        internal EcsEntityData[] _entities = new EcsEntityData[1024];
        internal int _entitiesCount;
        internal readonly EcsGrowList<int> _freeEntities = new EcsGrowList<int> (1024);
        internal readonly EcsGrowList<EcsFilter> _filters = new EcsGrowList<EcsFilter> (128);
        protected readonly Dictionary<int, EcsGrowList<EcsFilter>> _filterByIncludedComponents = new Dictionary<int, EcsGrowList<EcsFilter>> (64);
        protected readonly Dictionary<int, EcsGrowList<EcsFilter>> _filterByExcludedComponents = new Dictionary<int, EcsGrowList<EcsFilter>> (64);
        protected readonly Dictionary<int, EcsFilter> _oneFrameFilters = new Dictionary<int, EcsFilter> (64);
#if DEBUG
        internal bool _isDestroyed;
        internal bool _inDestroying;
        internal EcsGrowList<EcsEntity> _leakedEntities = new EcsGrowList<EcsEntity> (256);
        internal readonly List<IEcsWorldDebugListener> _debugListeners = new List<IEcsWorldDebugListener> (4);

        /// <summary>
        /// Adds external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void AddDebugListener (IEcsWorldDebugListener listener) {
            if (listener == null) { throw new Exception ("Listener is null."); }
            _debugListeners.Add (listener);
        }

        /// <summary>
        /// Removes external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void RemoveDebugListener (IEcsWorldDebugListener listener) {
            if (listener == null) { throw new Exception ("Listener is null."); }
            _debugListeners.Remove (listener);
        }
#endif

        /// <summary>
        /// Destroyes world and exist entities.
        /// </summary>
        public void Destroy () {
#if DEBUG
            if (_isDestroyed || _inDestroying) { throw new Exception ("EcsWorld already destroyed."); }
            _inDestroying = true;
            CheckForLeakedEntities ("Destroy");
#endif
            var entity = new EcsEntity ();
            entity.Owner = this;
            var maxCount = _entitiesCount;
            for (var i = _entitiesCount - 1; i >= 0; i--) {
                ref var entityData = ref _entities[i];
                if (entityData.ComponentsCountX2 > 0) {
                    entity.Id = i;
                    entity.Gen = entityData.Gen;
                    entity.Destroy ();
                }
            }
#if DEBUG
            _isDestroyed = true;
            for (var i = _debugListeners.Count - 1; i >= 0; i--) {
                _debugListeners[i].OnWorldDestroyed ();
            }
#endif
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity NewEntity () {
#if DEBUG
            if (_isDestroyed) { throw new Exception ("EcsWorld already destroyed."); }
#endif
            EcsEntity entity;
            entity.Owner = this;
            // try to reuse entity from pool.
            if (_freeEntities.Count > 0) {
                entity.Id = _freeEntities.Items[--_freeEntities.Count];
                ref var entityData = ref _entities[entity.Id];
                entity.Gen = entityData.Gen;
                entityData.ComponentsCountX2 = 0;
            } else {
                // create new entity.
                if (_entitiesCount == _entities.Length) {
                    Array.Resize (ref _entities, _entitiesCount << 1);
                }
                entity.Id = _entitiesCount++;
                ref var entityData = ref _entities[entity.Id];
                entityData.Components = new int[EcsHelpers.EntityComponentsCountX2];
                entityData.Gen = 1;
                entity.Gen = entityData.Gen;
                entityData.ComponentsCountX2 = 0;
            }
#if DEBUG
            _leakedEntities.Add (entity);
            for (var ii = 0; ii < _debugListeners.Count; ii++) {
                _debugListeners[ii].OnEntityCreated (entity);
            }
#endif
            return entity;
        }

        /// <summary>
        /// Creates entity and attaches component.
        /// </summary>
        /// <typeparam name="C1">Type of component1.</typeparam>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity NewEntityWith<C1> (out C1 c1) where C1 : class {
            var entity = NewEntity ();
            c1 = entity.Set<C1> ();
            return entity;
        }

        /// <summary>
        /// Creates entity and attaches components.
        /// </summary>
        /// <typeparam name="C1">Type of component1.</typeparam>
        /// <typeparam name="C2">Type of component2.</typeparam>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity NewEntityWith<C1, C2> (out C1 c1, out C2 c2) where C1 : class where C2 : class {
            var entity = NewEntity ();
            c1 = entity.Set<C1> ();
            c2 = entity.Set<C2> ();
            return entity;
        }

        /// <summary>
        /// Creates entity and attaches components.
        /// </summary>
        /// <typeparam name="C1">Type of component1.</typeparam>
        /// <typeparam name="C2">Type of component2.</typeparam>
        /// <typeparam name="C3">Type of component3.</typeparam>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity NewEntityWith<C1, C2, C3> (out C1 c1, out C2 c2, out C3 c3) where C1 : class where C2 : class where C3 : class {
            var entity = NewEntity ();
            c1 = entity.Set<C1> ();
            c2 = entity.Set<C2> ();
            c3 = entity.Set<C3> ();
            return entity;
        }

        /// <summary>
        /// Creates entity and attaches components.
        /// </summary>
        /// <typeparam name="C1">Type of component1.</typeparam>
        /// <typeparam name="C2">Type of component2.</typeparam>
        /// <typeparam name="C3">Type of component3.</typeparam>
        /// <typeparam name="C4">Type of component4.</typeparam>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity NewEntityWith<C1, C2, C3, C4> (out C1 c1, out C2 c2, out C3 c3, out C4 c4) where C1 : class where C2 : class where C3 : class where C4 : class {
            var entity = NewEntity ();
            c1 = entity.Set<C1> ();
            c2 = entity.Set<C2> ();
            c3 = entity.Set<C3> ();
            c4 = entity.Set<C4> ();
            return entity;
        }

        /// <summary>
        /// Restores EcsEntity from internal id.
        /// </summary>
        /// <param name="id">Internal id.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsEntity RestoreEntityFromInternalId (int id) {
            EcsEntity entity;
            entity.Owner = this;
            entity.Id = id;
            entity.Gen = 0;
            ref var entityData = ref GetEntityData (entity);
            entity.Gen = entityData.Gen;
            return entity;
        }

        /// <summary>
        /// Request exist filter or create new one.
        /// </summary>
        /// <param name="filterType"></param>
        public EcsFilter GetFilter (Type filterType) {
#if DEBUG
            if (filterType == null) { throw new Exception ("FilterType is null."); }
            if (!filterType.IsSubclassOf (typeof (EcsFilter))) { throw new Exception ($"Invalid filter type: {filterType}."); }
            if (_isDestroyed) { throw new Exception ("EcsWorld already destroyed."); }
#endif
            // check already exist filters.
            for (int i = 0, iMax = _filters.Count; i < iMax; i++) {
                if (_filters.Items[i].GetType () == filterType) {
                    return _filters.Items[i];
                }
            }
            // create new filter.
            var filter = (EcsFilter) Activator.CreateInstance (filterType, true);
#if DEBUG
            for (var filterIdx = 0; filterIdx < _filters.Count; filterIdx++) {
                if (filter.AreComponentsSame (_filters.Items[filterIdx])) {
                    throw new Exception ($"Invalid filter \"{filter.GetType()}\": Another filter \"{_filters.Items[filterIdx].GetType()}\" already has same components, but in different order.");
                }
            }
#endif
            _filters.Add (filter);
            // add to component dicts for fast compatibility scan.
            for (int i = 0, iMax = filter.IncludedComponentTypes.Length; i < iMax; i++) {
                if (!_filterByIncludedComponents.TryGetValue (filter.IncludedComponentTypes[i], out var filtersList)) {
                    filtersList = new EcsGrowList<EcsFilter> (8);
                    _filterByIncludedComponents[filter.IncludedComponentTypes[i]] = filtersList;
                }
                filtersList.Add (filter);
            }
            if (filter.ExcludedComponentTypes != null) {
                for (int i = 0, iMax = filter.ExcludedComponentTypes.Length; i < iMax; i++) {
                    if (!_filterByExcludedComponents.TryGetValue (filter.ExcludedComponentTypes[i], out var filtersList)) {
                        filtersList = new EcsGrowList<EcsFilter> (8);
                        _filterByExcludedComponents[filter.ExcludedComponentTypes[i]] = filtersList;
                    }
                    filtersList.Add (filter);
                }
            }
            return filter;
        }

        /// <summary>
        /// Informs world that frame ended and one-frame components can be removed.
        /// </summary>
        public void EndFrame () {
            // FIXME: process one-frame components.
            foreach (var pair in _oneFrameFilters) {
                var typeIdx = pair.Key;
                var filter = pair.Value;
                foreach (var idx in filter) {
                    filter.Entities[idx].Unset (typeIdx);
                }
            }
        }

        /// <summary>
        /// Gets stats of internal data.
        /// </summary>
        public EcsWorldStats GetStats () {
            var stats = new EcsWorldStats () {
                ActiveEntities = _entitiesCount - _freeEntities.Count,
                ReservedEntities = _freeEntities.Count,
                Filters = _filters.Count,
                Components = EcsComponentPools.Count,
                OneFrameComponents = _oneFrameFilters.Count
            };
            return stats;
        }

        /// <summary>
        /// Creates one-frame filter if not exists.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void ValidateOneFrameFilter<T> () where T : class {
            var idx = EcsComponentPool<T>.Instance.TypeIndex;
            if (!_oneFrameFilters.ContainsKey (idx)) {
                _oneFrameFilters[idx] = GetFilter (typeof (EcsFilter<T>));
            }
        }

        /// <summary>
        /// Recycles internal entity data to pool.
        /// </summary>
        /// <param name="id">Entity id.</param>
        /// <param name="entityData">Entity internal data.</param>
        internal protected void RecycleEntityData (int id, ref EcsEntityData entityData) {
#if DEBUG
            if (entityData.ComponentsCountX2 != 0) { throw new Exception ("Cant recycle invalid entity."); }
#endif
            entityData.ComponentsCountX2 = -2;
            entityData.Gen = (ushort) ((entityData.Gen + 1) % ushort.MaxValue);
            _freeEntities.Add (id);
        }

#if DEBUG
        /// <summary>
        /// Checks exist entities but without components.
        /// </summary>
        /// <param name="errorMsg">Prefix for error message.</param>
        public void CheckForLeakedEntities (string errorMsg) {
            if (_leakedEntities.Count > 0) {
                for (int i = 0, iMax = _leakedEntities.Count; i < iMax; i++) {
                    if (GetEntityData (_leakedEntities.Items[i]).ComponentsCountX2 == 0) {
                        throw new Exception ($"{errorMsg}: Empty entity detected, possible memory leak.");
                    }
                }
                _leakedEntities.Count = 0;
            }
        }
#endif

        /// <summary>
        /// Updates filters.
        /// </summary>
        /// <param name="typeIdx">Component type index.abstract Positive for add operation, negative for remove operation.</param>
        /// <param name="entity">Target entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal protected void UpdateFilters (int typeIdx, in EcsEntity entity, in EcsEntityData entityData) {
#if DEBUG
            if (_isDestroyed) { throw new Exception ("EcsWorld already destroyed."); }
#endif
            EcsGrowList<EcsFilter> filters;
            if (typeIdx < 0) {
                // remove component.
                if (_filterByIncludedComponents.TryGetValue (-typeIdx, out filters)) {
                    for (int i = 0, iMax = filters.Count; i < iMax; i++) {
                        if (filters.Items[i].IsCompatible (entityData, 0)) {
#if DEBUG
                            var isValid = false;
                            foreach (var idx in filters.Items[i]) {
                                if (filters.Items[i].Entities[idx].Id == entity.Id) { isValid = true; break; }
                            }
                            if (!isValid) { throw new Exception ("Entity not in filter."); }
#endif
                            filters.Items[i].RemoveEntity (entity);
                        }
                    }
                }
                if (_filterByExcludedComponents.TryGetValue (-typeIdx, out filters)) {
                    for (int i = 0, iMax = filters.Count; i < iMax; i++) {
                        if (filters.Items[i].IsCompatible (entityData, typeIdx)) {
#if DEBUG
                            var isValid = true;
                            foreach (var idx in filters.Items[i]) {
                                if (filters.Items[i].Entities[idx].Id == entity.Id) {
                                    isValid = false;
                                    break;
                                }
                            }
                            if (!isValid) { throw new Exception ("Entity already in filter."); }
#endif
                            filters.Items[i].AddEntity (entity);
                        }
                    }
                }
            } else {
                // add component.
                if (_filterByIncludedComponents.TryGetValue (typeIdx, out filters)) {
                    for (int i = 0, iMax = filters.Count; i < iMax; i++) {
                        if (filters.Items[i].IsCompatible (entityData, 0)) {
#if DEBUG
                            var isValid = true;
                            foreach (var idx in filters.Items[i]) {
                                if (filters.Items[i].Entities[idx].Id == entity.Id) { isValid = false; break; }
                            }
                            if (!isValid) { throw new Exception ("Entity already in filter."); }
#endif
                            filters.Items[i].AddEntity (entity);
                        }
                    }
                }
                if (_filterByExcludedComponents.TryGetValue (typeIdx, out filters)) {
                    for (int i = 0, iMax = filters.Count; i < iMax; i++) {
                        if (filters.Items[i].IsCompatible (entityData, -typeIdx)) {
#if DEBUG
                            var isValid = false;
                            foreach (var idx in filters.Items[i]) {
                                if (filters.Items[i].Entities[idx].Id == entity.Id) { isValid = true; break; }
                            }
                            if (!isValid) { throw new Exception ("Entity not in filter."); }
#endif
                            filters.Items[i].RemoveEntity (entity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns internal state of entity. For internal use!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref EcsEntityData GetEntityData (in EcsEntity entity) {
#if DEBUG
            if (_isDestroyed) { throw new Exception ("EcsWorld already destroyed."); }
            if (entity.Id < 0 || entity.Id > _entitiesCount) { throw new Exception ($"Invalid entity {entity.Id}"); }
#endif
            return ref _entities[entity.Id];
        }

        /// <summary>
        /// Internal state of entity.
        /// </summary>
        [StructLayout (LayoutKind.Sequential, Pack = 2)]
        public struct EcsEntityData {
            public ushort Gen;
            public short ComponentsCountX2;
            public int[] Components;
        }
    }

    /// <summary>
    /// Stats of EcsWorld instance.
    /// </summary>
    public struct EcsWorldStats {
        /// <summary>
        /// Amount of active entities.
        /// </summary>
        public int ActiveEntities;

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
        /// Amount of one-frame registered components.
        /// </summary>
        public int OneFrameComponents;
    }

#if DEBUG
    /// <summary>
    /// Debug interface for world events processing.
    /// </summary>
    public interface IEcsWorldDebugListener {
        void OnEntityCreated (in EcsEntity entity);
        void OnEntityDestroyed (in EcsEntity entity);
        void OnComponentAdded (in EcsEntity entity, object component);
        void OnComponentRemoved (in EcsEntity entity, object component);
        void OnWorldDestroyed ();
    }
#endif
}