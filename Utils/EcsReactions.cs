// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Ecs system for stream processing events from EcsFilter.
    /// </summary>
    public abstract class EcsReactSystem : IEcsPreInitSystem, IEcsRunSystem, IEcsFilterListener {
        public abstract EcsFilter GetReactFilter ();

        public abstract EcsReactSystemType GetReactSystemType ();

        public abstract void RunReact (int[] entities, int count);

        EcsFilter _reactFilter;

        EcsReactSystemType _type;

        int[] _entities = new int[32];

        int _entitiesCount;

        readonly EcsEntityHashSet _entityHashes = new EcsEntityHashSet ();

        public void Run () {
            if (_entitiesCount > 0) {
                RunReact (_entities, _entitiesCount);
                _entityHashes.Clear ();
                _entitiesCount = 0;
            }
        }

        void IEcsFilterListener.OnFilterEntityAdded (int entity, object component) {
#if DEBUG
            if (_entityHashes.Contains (entity)) {
                throw new System.Exception ("Entity already in processing list.");
            }
#endif
            if (_type == EcsReactSystemType.OnAdd) {
                if (_entities.Length == _entitiesCount) {
                    Array.Resize (ref _entities, _entitiesCount << 1);
                }
                _entities[_entitiesCount++] = entity;
                _entityHashes.Add (entity);
            }
        }

        void IEcsFilterListener.OnFilterEntityUpdated (int entity, object component) {
            if (_type == EcsReactSystemType.OnUpdate) {
                if (_entityHashes.Add (entity)) {
                    if (_entities.Length == _entitiesCount) {
                        Array.Resize (ref _entities, _entitiesCount << 1);
                    }
                    _entities[_entitiesCount++] = entity;
                }
            }
        }

        void IEcsFilterListener.OnFilterEntityRemoved (int entity, object component) {
            if (_entityHashes.Remove (entity)) {
                for (var i = 0; i < _entitiesCount; i++) {
                    if (_entities[i] == entity) {
                        _entitiesCount--;
                        Array.Copy (_entities, i + 1, _entities, i, _entitiesCount - i);
                        break;
                    }
                }
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

        public abstract void RunReact (int entity, object reason);

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

        void IEcsFilterListener.OnFilterEntityAdded (int entity, object reason) {
            if (_type == EcsReactSystemType.OnAdd) {
                RunReact (entity, reason);
            }
        }

        void IEcsFilterListener.OnFilterEntityRemoved (int entity, object reason) {
            if (_type == EcsReactSystemType.OnRemove) {
                RunReact (entity, reason);
            }
        }

        void IEcsFilterListener.OnFilterEntityUpdated (int entity, object reason) {
            if (_type == EcsReactSystemType.OnUpdate) {
                RunReact (entity, reason);
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