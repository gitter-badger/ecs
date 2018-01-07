// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Base interface for all ecs systems.
    /// </summary>
    public interface IEcsSystem { }

    /// <summary>
    /// Allows custom initialization / deinitialization for ecs system.
    /// </summary>
    public interface IEcsInitSystem : IEcsSystem {
        /// <summary>
        /// Initializes system inside EcsWorld instance.
        /// </summary>
        void Initialize ();

        /// <summary>
        /// Destroys all internal allocated data.
        /// </summary>
        void Destroy ();
    }

    /// <summary>
    /// Allows custom initialization / deinitialization for ecs system before standard Initialize / Destroy calls.
    /// </summary>
    public interface IEcsPreInitSystem : IEcsSystem {
        /// <summary>
        /// Initializes system inside EcsWorld instance.
        /// </summary>
        void PreInitialize ();

        /// <summary>
        /// Destroys all internal allocated data.
        /// </summary>
        void PreDestroy ();
    }

    /// <summary>
    /// Allows custom logic processing.
    /// </summary>
    public interface IEcsRunSystem : IEcsSystem {
        /// <summary>
        /// Returns update type (Update(), FixedUpdate(), etc).
        /// </summary>
        EcsRunSystemType GetRunSystemType ();

        /// <summary>
        /// Custom logic.
        /// </summary>
        void Run ();
    }

    /// <summary>
    /// When IEcsRunSystem should be processed.
    /// </summary>
    public enum EcsRunSystemType {
        Update,
        FixedUpdate
    }

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

        void OnEntityAdded (int entity, int componentId) {
            if (_entities.IndexOf (entity) == -1) {
                _entities.Add (entity);
            }
        }

        void OnEntityRemoved (int entity, int componentId) {
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
    /// When react system should be processed.
    /// </summary>
    public enum EcsReactSystemType {
        OnAdd,
        OnUpdate
    }
}