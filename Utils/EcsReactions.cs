// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Ecs system for stream processing events from EcsFilter.
    /// </summary>
    public abstract class EcsReactSystem : IEcsPreInitSystem, IEcsRunSystem, IEcsFilterListener {
        public abstract EcsFilter GetReactFilter ();

        public abstract EcsReactSystemType GetReactSystemType ();

        public abstract void RunReact (List<int> entities);

        EcsFilter _reactFilter;

        EcsReactSystemType _type;

        readonly List<int> _entities = new List<int> (64);

        int _entitiesCount;

        readonly EcsEntityHashSet _entityHashes = new EcsEntityHashSet ();

        public void Run () {
            if (_entitiesCount > 0) {
                RunReact (_entities);
                _entities.Clear ();
                _entityHashes.Clear ();
                _entitiesCount = 0;
            }
        }

        void IEcsFilterListener.OnFilterEntityAdded (int entity) {
#if DEBUG
            if (_entityHashes.Contains (entity)) {
                throw new System.Exception ("Entity already in processing list.");
            }
#endif
            if (_type == EcsReactSystemType.OnAdd) {
                _entities.Add (entity);
                _entityHashes.Add (entity);
                _entitiesCount++;
            }
        }

        void IEcsFilterListener.OnFilterEntityUpdated (int entity) {
            if (_type == EcsReactSystemType.OnUpdate) {
                if (_entityHashes.Add (entity)) {
                    _entities.Add (entity);
                    _entitiesCount++;
                }
            }
        }

        void IEcsFilterListener.OnFilterEntityRemoved (int entity) {
            if (_entityHashes.Remove (entity)) {
                _entities.Remove (entity);
                _entitiesCount--;
            }
        }

        void IEcsPreInitSystem.PreInitialize () {
            _reactFilter = GetReactFilter ();
            _type = GetReactSystemType ();
#if DEBUG
            if (_type == EcsReactSystemType.OnRemove) {
                throw new System.NotSupportedException (
                    "OnRemove type not supported for delayed processing, use EcsReactInstantSystem instead.");
            }
#endif
            _reactFilter.AddListener (this);
        }

        void IEcsPreInitSystem.PreDestroy () {
            _reactFilter.RemoveListener (this);
        }
    }

    /// <summary>
    /// Ecs system for instant processing events from EcsFilter.
    /// </summary>
    public abstract class EcsReactInstantSystem : IEcsPreInitSystem, IEcsFilterListener {
        public abstract EcsFilter GetReactFilter ();

        public abstract EcsReactSystemType GetReactSystemType ();

        public abstract void RunReact (int entity);

        EcsFilter _reactFilter;

        EcsReactSystemType _type;

        void IEcsPreInitSystem.PreInitialize () {
            _reactFilter = GetReactFilter ();
            _type = GetReactSystemType ();
            _reactFilter.AddListener (this);
        }

        void IEcsPreInitSystem.PreDestroy () {
            _reactFilter.RemoveListener (this);
        }

        void IEcsFilterListener.OnFilterEntityAdded (int entity) {
            if (_type == EcsReactSystemType.OnAdd) {
                RunReact (entity);
            }
        }

        void IEcsFilterListener.OnFilterEntityRemoved (int entity) {
            if (_type == EcsReactSystemType.OnRemove) {
                RunReact (entity);
            }
        }

        void IEcsFilterListener.OnFilterEntityUpdated (int entity) {
            if (_type == EcsReactSystemType.OnUpdate) {
                RunReact (entity);
            }
        }
    }

    /// <summary>
    /// When react system should be processed.
    /// </summary>
    public enum EcsReactSystemType {
        OnAdd,
        OnRemove,
        OnUpdate
    }
}