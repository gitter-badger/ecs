// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Leopotam.Ecs {
    /// <summary>
    /// Common interface for all filter listeners.
    /// </summary>
    public interface IEcsFilterListener {
        void OnEntityAdded (in EcsEntity entity);
        void OnEntityRemoved (in EcsEntity entity);
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public abstract class EcsFilter {
        public EcsEntity[] Entities = new EcsEntity[EcsHelpers.FilterEntitiesSize];
        protected int _entitiesCount;
        protected int _lockCount;

        DelayedOp[] _delayedOps = new DelayedOp[EcsHelpers.FilterEntitiesSize];
        int _delayedOpsCount;

        protected IEcsFilterListener[] _listeners = new IEcsFilterListener[4];
        protected int _listenersCount;

        internal protected int[] IncludedComponentTypes;
        internal protected int[] ExcludedComponentTypes;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator () {
            return new Enumerator (this);
        }

        /// <summary>
        /// Gets entities count.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetEntitiesCount () {
            return _entitiesCount;
        }

        /// <summary>
        /// Is filter not contains entities.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty () {
            return _entitiesCount == 0;
        }

        /// <summary>
        /// Subscribes listener to filter events.
        /// </summary>
        /// <param name="listener">Listener.</param>
        public void AddListener (IEcsFilterListener listener) {
#if DEBUG
            for (int i = 0, iMax = _listenersCount; i < iMax; i++) {
                if (_listeners[i] == listener) {
                    throw new Exception ("Listener already subscribed.");
                }
            }
#endif
            if (_listeners.Length == _listenersCount) {
                Array.Resize (ref _listeners, _listenersCount << 1);
            }
            _listeners[_listenersCount++] = listener;
        }

        /// <summary>
        /// Unsubscribes listener from filter events.
        /// </summary>
        /// <param name="listener">Listener.</param>
        public void RemoveListener (IEcsFilterListener listener) {
            for (int i = 0, iMax = _listenersCount; i < iMax; i++) {
                if (_listeners[i] == listener) {
                    _listenersCount--;
                    // cant fill gap with last element due listeners order is important.
                    Array.Copy (_listeners, i + 1, _listeners, i, _listenersCount - i);
                    break;
                }
            }
        }

        /// <summary>
        /// Is filter compatible with components on entity with optional added / removed component.
        /// </summary>
        /// <param name="filter">Filter for check.</param>
        /// <param name="entityData">Entity data.</param>
        /// <param name="addedRemovedTypeIndex">Optional added (> 0) or removed (< 0) component. Will be ignored if zero.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal bool IsCompatible (in EcsWorld.EcsEntityData entityData, int addedRemovedTypeIndex) {
            var incIdx = IncludedComponentTypes.Length - 1;
            for (; incIdx >= 0; incIdx--) {
                var typeIdx = IncludedComponentTypes[incIdx];
                var idx = entityData.ComponentsCountX2 - 2;
                for (; idx >= 0; idx -= 2) {
                    var typeIdx2 = entityData.Components[idx];
                    if (typeIdx2 == -addedRemovedTypeIndex) {
                        continue;
                    }
                    if (typeIdx2 == addedRemovedTypeIndex || typeIdx2 == typeIdx) {
                        break;
                    }
                }
                // not found.
                if (idx == -2) {
                    break;
                }
            }
            // one of required component not found.
            if (incIdx != -1) {
                return false;
            }
            // check for excluded components.
            if (ExcludedComponentTypes != null) {
                for (var excIdx = 0; excIdx < ExcludedComponentTypes.Length; excIdx++) {
                    var typeIdx = ExcludedComponentTypes[excIdx];
                    for (var idx = entityData.ComponentsCountX2 - 2; idx >= 0; idx -= 2) {
                        var typeIdx2 = entityData.Components[idx];
                        if (typeIdx2 == -addedRemovedTypeIndex) {
                            continue;
                        }
                        if (typeIdx2 == addedRemovedTypeIndex || typeIdx2 == typeIdx) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        protected bool AddDelayedOp (bool isAdd, in EcsEntity entity) {
            if (_lockCount <= 0) {
                return false;
            }
            if (_delayedOps.Length == _delayedOpsCount) {
                Array.Resize (ref _delayedOps, _delayedOpsCount << 1);
            }
            ref var op = ref _delayedOps[_delayedOpsCount++];
            op.IsAdd = isAdd;
            op.Entity = entity;
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        protected void ProcessListeners (bool isAdd, in EcsEntity entity) {
            if (isAdd) {
                for (int i = 0, iMax = _listenersCount; i < iMax; i++) {
                    _listeners[i].OnEntityAdded (entity);
                }
            } else {
                for (int i = 0, iMax = _listenersCount; i < iMax; i++) {
                    _listeners[i].OnEntityRemoved (entity);
                }
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Lock () {
            _lockCount++;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Unlock () {
#if DEBUG
            if (_lockCount <= 0) {
                throw new Exception ($"Invalid lock-unlock balance for \"{GetType().Name}\".");
            }
#endif
            _lockCount--;
            if (_lockCount == 0 && _delayedOpsCount > 0) {
                // process delayed operations.
                for (int i = 0, iMax = _delayedOpsCount; i < iMax; i++) {
                    ref var op = ref _delayedOps[i];
                    if (op.IsAdd) {
                        AddEntity (op.Entity);
                    } else {
                        RemoveEntity (op.Entity);
                    }
                }
                _delayedOpsCount = 0;
            }
        }

#if DEBUG
        /// <summary>
        /// For debug purposes. Check filters equality by included / excluded components.
        /// </summary>
        /// <param name="filter">Filter to compare.</param>
        internal bool AreComponentsSame (EcsFilter filter) {
            if (IncludedComponentTypes.Length != filter.IncludedComponentTypes.Length) {
                return false;
            }
            for (var i = 0; i < IncludedComponentTypes.Length; i++) {
                if (Array.IndexOf (filter.IncludedComponentTypes, IncludedComponentTypes[i]) == -1) {
                    return false;
                }
            }
            if ((ExcludedComponentTypes == null && filter.ExcludedComponentTypes != null) ||
                (ExcludedComponentTypes != null && filter.ExcludedComponentTypes == null)) {
                return false;
            }
            if (ExcludedComponentTypes != null) {
                if (ExcludedComponentTypes.Length != filter.ExcludedComponentTypes.Length) {
                    return false;
                }
                for (var i = 0; i < ExcludedComponentTypes.Length; i++) {
                    if (Array.IndexOf (filter.ExcludedComponentTypes, ExcludedComponentTypes[i]) == -1) {
                        return false;
                    }
                }
            }
            return true;
        }
#endif

        public abstract void AddEntity (in EcsEntity entity);
        public abstract void RemoveEntity (in EcsEntity entity);

        public struct Enumerator : IDisposable {
            readonly EcsFilter _filter;
            readonly int _count;
            int _idx;

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            internal Enumerator (EcsFilter filter) {
                _filter = filter;
                _count = _filter.GetEntitiesCount ();
                _idx = -1;
                _filter.Lock ();
            }

            public int Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get { return _idx; }
            }

#if ENABLE_IL2CPP
            [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
            [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public void Dispose () {
                _filter.Unlock ();
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                return ++_idx < _count;
            }
        }

        struct DelayedOp {
            public bool IsAdd;
            public EcsEntity Entity;
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1> : EcsFilter where Inc1 : class {
        public Inc1[] Get1;
        readonly bool _allow1;

        protected EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            Get1 = _allow1 ? new Inc1[EcsHelpers.FilterEntitiesSize] : null;
            IncludedComponentTypes = new [] { EcsComponentPool<Inc1>.Instance.TypeIndex };
        }

        #region Compatibility
        [Obsolete ("Use Get1 instead")]
        public Inc1[] Components1 { get { return Get1; } }
        #endregion

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void AddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == _entitiesCount) {
                Array.Resize (ref Entities, _entitiesCount << 1);
                if (_allow1) { Array.Resize (ref Get1, _entitiesCount << 1); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentPool<Inc1>.Instance.TypeIndex) {
                    Get1[_entitiesCount] = EcsComponentPool<Inc1>.Instance.Items[itemIdx];
                    allow1 = false;
                }
            }
            Entities[_entitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void RemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    _entitiesCount--;
                    if (i < _entitiesCount) {
                        Entities[i] = Entities[_entitiesCount];
                        if (_allow1) { Get1[i] = Get1[_entitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1> where Exc1 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1> where Exc1 : class where Exc2 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex, EcsComponentPool<Exc2>.Instance.TypeIndex };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2> : EcsFilter where Inc1 : class where Inc2 : class {
        public Inc1[] Get1;
        public Inc2[] Get2;
        readonly bool _allow1;
        readonly bool _allow2;

        protected EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            Get1 = _allow1 ? new Inc1[EcsHelpers.FilterEntitiesSize] : null;
            Get2 = _allow2 ? new Inc2[EcsHelpers.FilterEntitiesSize] : null;
            IncludedComponentTypes = new [] { EcsComponentPool<Inc1>.Instance.TypeIndex, EcsComponentPool<Inc2>.Instance.TypeIndex };
        }

        #region Compatibility
        [Obsolete ("Use Get1 instead")]
        public Inc1[] Components1 { get { return Get1; } }

        [Obsolete ("Use Get2 instead")]
        public Inc2[] Components2 { get { return Get2; } }
        #endregion

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void AddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == _entitiesCount) {
                Array.Resize (ref Entities, _entitiesCount << 1);
                if (_allow1) { Array.Resize (ref Get1, _entitiesCount << 1); }
                if (_allow2) { Array.Resize (ref Get2, _entitiesCount << 1); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentPool<Inc1>.Instance.TypeIndex) {
                    Get1[_entitiesCount] = EcsComponentPool<Inc1>.Instance.Items[itemIdx];
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentPool<Inc2>.Instance.TypeIndex) {
                    Get2[_entitiesCount] = EcsComponentPool<Inc2>.Instance.Items[itemIdx];
                    allow2 = false;
                }
            }
            Entities[_entitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void RemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    _entitiesCount--;
                    if (i < _entitiesCount) {
                        Entities[i] = Entities[_entitiesCount];
                        if (_allow1) { Get1[i] = Get1[_entitiesCount]; }
                        if (_allow2) { Get2[i] = Get2[_entitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2> where Exc1 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2> where Exc1 : class where Exc2 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex, EcsComponentPool<Exc2>.Instance.TypeIndex };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2, Inc3> : EcsFilter where Inc1 : class where Inc2 : class where Inc3 : class {
        public Inc1[] Get1;
        public Inc2[] Get2;
        public Inc3[] Get3;
        readonly bool _allow1;
        readonly bool _allow2;
        readonly bool _allow3;

        protected EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            _allow3 = !EcsComponentPool<Inc3>.Instance.IsIgnoreInFilter;
            Get1 = _allow1 ? new Inc1[EcsHelpers.FilterEntitiesSize] : null;
            Get2 = _allow2 ? new Inc2[EcsHelpers.FilterEntitiesSize] : null;
            Get3 = _allow3 ? new Inc3[EcsHelpers.FilterEntitiesSize] : null;
            IncludedComponentTypes = new [] {
                EcsComponentPool<Inc1>.Instance.TypeIndex,
                EcsComponentPool<Inc2>.Instance.TypeIndex,
                EcsComponentPool<Inc3>.Instance.TypeIndex
            };
        }

        #region Compatibility
        [Obsolete ("Use Get1 instead")]
        public Inc1[] Components1 { get { return Get1; } }

        [Obsolete ("Use Get2 instead")]
        public Inc2[] Components2 { get { return Get2; } }

        [Obsolete ("Use Get3 instead")]
        public Inc3[] Components3 { get { return Get3; } }
        #endregion

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void AddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == _entitiesCount) {
                Array.Resize (ref Entities, _entitiesCount << 1);
                if (_allow1) { Array.Resize (ref Get1, _entitiesCount << 1); }
                if (_allow2) { Array.Resize (ref Get2, _entitiesCount << 1); }
                if (_allow3) { Array.Resize (ref Get3, _entitiesCount << 1); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            var allow3 = _allow3;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentPool<Inc1>.Instance.TypeIndex) {
                    Get1[_entitiesCount] = EcsComponentPool<Inc1>.Instance.Items[itemIdx];
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentPool<Inc2>.Instance.TypeIndex) {
                    Get2[_entitiesCount] = EcsComponentPool<Inc2>.Instance.Items[itemIdx];
                    allow2 = false;
                }
                if (allow3 && typeIdx == EcsComponentPool<Inc3>.Instance.TypeIndex) {
                    Get3[_entitiesCount] = EcsComponentPool<Inc3>.Instance.Items[itemIdx];
                    allow3 = false;
                }
            }
            Entities[_entitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void RemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    _entitiesCount--;
                    if (i < _entitiesCount) {
                        Entities[i] = Entities[_entitiesCount];
                        if (_allow1) { Get1[i] = Get1[_entitiesCount]; }
                        if (_allow2) { Get2[i] = Get2[_entitiesCount]; }
                        if (_allow3) { Get3[i] = Get3[_entitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class where Exc2 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex, EcsComponentPool<Exc2>.Instance.TypeIndex };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2, Inc3, Inc4> : EcsFilter where Inc1 : class where Inc2 : class where Inc3 : class where Inc4 : class {
        public Inc1[] Get1;
        public Inc2[] Get2;
        public Inc3[] Get3;
        public Inc4[] Get4;
        readonly bool _allow1;
        readonly bool _allow2;
        readonly bool _allow3;
        readonly bool _allow4;

        protected EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            _allow3 = !EcsComponentPool<Inc3>.Instance.IsIgnoreInFilter;
            _allow4 = !EcsComponentPool<Inc4>.Instance.IsIgnoreInFilter;
            Get1 = _allow1 ? new Inc1[EcsHelpers.FilterEntitiesSize] : null;
            Get2 = _allow2 ? new Inc2[EcsHelpers.FilterEntitiesSize] : null;
            Get3 = _allow3 ? new Inc3[EcsHelpers.FilterEntitiesSize] : null;
            Get4 = _allow4 ? new Inc4[EcsHelpers.FilterEntitiesSize] : null;
            IncludedComponentTypes = new [] {
                EcsComponentPool<Inc1>.Instance.TypeIndex,
                EcsComponentPool<Inc2>.Instance.TypeIndex,
                EcsComponentPool<Inc3>.Instance.TypeIndex,
                EcsComponentPool<Inc4>.Instance.TypeIndex
            };
        }

        #region Compatibility
        [Obsolete ("Use Get1 instead")]
        public Inc1[] Components1 { get { return Get1; } }

        [Obsolete ("Use Get2 instead")]
        public Inc2[] Components2 { get { return Get2; } }

        [Obsolete ("Use Get3 instead")]
        public Inc3[] Components3 { get { return Get3; } }

        [Obsolete ("Use Get4 instead")]
        public Inc4[] Components4 { get { return Get4; } }
        #endregion

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void AddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == _entitiesCount) {
                Array.Resize (ref Entities, _entitiesCount << 1);
                if (_allow1) { Array.Resize (ref Get1, _entitiesCount << 1); }
                if (_allow2) { Array.Resize (ref Get2, _entitiesCount << 1); }
                if (_allow3) { Array.Resize (ref Get3, _entitiesCount << 1); }
                if (_allow4) { Array.Resize (ref Get4, _entitiesCount << 1); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            var allow3 = _allow3;
            var allow4 = _allow4;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentPool<Inc1>.Instance.TypeIndex) {
                    Get1[_entitiesCount] = EcsComponentPool<Inc1>.Instance.Items[itemIdx];
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentPool<Inc2>.Instance.TypeIndex) {
                    Get2[_entitiesCount] = EcsComponentPool<Inc2>.Instance.Items[itemIdx];
                    allow2 = false;
                }
                if (allow3 && typeIdx == EcsComponentPool<Inc3>.Instance.TypeIndex) {
                    Get3[_entitiesCount] = EcsComponentPool<Inc3>.Instance.Items[itemIdx];
                    allow3 = false;
                }
                if (allow4 && typeIdx == EcsComponentPool<Inc4>.Instance.TypeIndex) {
                    Get4[_entitiesCount] = EcsComponentPool<Inc4>.Instance.Items[itemIdx];
                    allow4 = false;
                }
            }
            Entities[_entitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void RemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    _entitiesCount--;
                    if (i < _entitiesCount) {
                        Entities[i] = Entities[_entitiesCount];
                        if (_allow1) { Get1[i] = Get1[_entitiesCount]; }
                        if (_allow2) { Get2[i] = Get2[_entitiesCount]; }
                        if (_allow3) { Get3[i] = Get3[_entitiesCount]; }
                        if (_allow4) { Get4[i] = Get4[_entitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class where Exc2 : class {
            protected Exclude () {
                ExcludedComponentTypes = new [] { EcsComponentPool<Exc1>.Instance.TypeIndex, EcsComponentPool<Exc2>.Instance.TypeIndex };
            }
        }
    }
}