// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Ecs system for stream processing events from EcsFilter.
    /// </summary>
    public abstract class EcsReactSystem : IEcsPreInitSystem, IEcsRunSystem {
        public abstract EcsFilter GetReactFilter ();

        public abstract EcsRunSystemType GetRunSystemType ();

        public abstract EcsReactSystemType GetReactSystemType ();

        public abstract void RunReact (List<int> entities);

        EcsFilter _reactFilter;

        EcsReactSystemType _type;

        readonly List<int> _entities = new List<int> (512);

        public void Run () {
            if (_entities.Count > 0) {
                RunReact (_entities);
                _entities.Clear ();
            }
        }

        void OnEntityAdded (int entity) {
            if (_entities.IndexOf (entity) == -1) {
                _entities.Add (entity);
            }
        }

        void OnEntityRemoved (int entity) {
            _entities.Remove (entity);
        }

        void IEcsPreInitSystem.PreInitialize () {
            _reactFilter = GetReactFilter ();
            _type = GetReactSystemType ();
            switch (_type) {
                case EcsReactSystemType.OnAdd:
                    _reactFilter.OnEntityAdded += OnEntityAdded;
                    _reactFilter.OnEntityRemoved += OnEntityRemoved;
                    break;
                case EcsReactSystemType.OnUpdate:
                    _reactFilter.OnEntityUpdated += OnEntityAdded;
                    break;
                case EcsReactSystemType.OnRemove:
                    throw new System.NotSupportedException (
                        "OnRemove type not supported for delayed processing, use EcsReactInstantSystem instead.");
            }
        }

        void IEcsPreInitSystem.PreDestroy () {
            switch (_type) {
                case EcsReactSystemType.OnAdd:
                    _reactFilter.OnEntityAdded -= OnEntityAdded;
                    _reactFilter.OnEntityRemoved -= OnEntityRemoved;
                    break;
                case EcsReactSystemType.OnUpdate:
                    _reactFilter.OnEntityUpdated -= OnEntityAdded;
                    break;
            }
        }
    }

    /// <summary>
    /// Ecs system for instant processing events from EcsFilter.
    /// </summary>
    public abstract class EcsReactInstantSystem : IEcsPreInitSystem {
        public abstract EcsFilter GetReactFilter ();

        public abstract EcsRunSystemType GetRunSystemType ();

        public abstract EcsReactSystemType GetReactSystemType ();

        public abstract void RunReact (int entity);

        EcsFilter _reactFilter;

        EcsReactSystemType _type;

        void IEcsPreInitSystem.PreInitialize () {
            _reactFilter = GetReactFilter ();
            _type = GetReactSystemType ();
            switch (_type) {
                case EcsReactSystemType.OnAdd:
                    _reactFilter.OnEntityAdded += RunReact;
                    break;
                case EcsReactSystemType.OnRemove:
                    _reactFilter.OnEntityRemoved += RunReact;
                    break;
                case EcsReactSystemType.OnUpdate:
                    _reactFilter.OnEntityUpdated += RunReact;
                    break;
            }
        }

        void IEcsPreInitSystem.PreDestroy () {
            switch (_type) {
                case EcsReactSystemType.OnAdd:
                    _reactFilter.OnEntityAdded -= RunReact;
                    break;
                case EcsReactSystemType.OnRemove:
                    _reactFilter.OnEntityRemoved -= RunReact;
                    break;
                case EcsReactSystemType.OnUpdate:
                    _reactFilter.OnEntityUpdated -= RunReact;
                    break;
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