// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
#if DEBUG
    /// <summary>
    /// Debug interface for systems events processing.
    /// </summary>
    public interface IEcsSystemsDebugListener {
        void OnSystemsDestroyed ();
    }
#endif

    /// <summary>
    /// Logical group of systems.
    /// </summary>
    public sealed class EcsSystems {
#if DEBUG
        /// <summary>
        /// Temporary disable RunUpdate / RunFixedUpdate / RunLateUpdate calls for debug reason.
        /// </summary>
        public bool IsRunActive = true;

        /// <summary>
        /// List of all debug listeners.
        /// </summary>
        readonly List<IEcsSystemsDebugListener> _debugListeners = new List<IEcsSystemsDebugListener> (4);
#endif

        /// <summary>
        /// Ecs world instance.
        /// </summary>
        readonly EcsWorld _world;

        /// <summary>
        /// Registered IEcsPreInitSystem systems.
        /// </summary>
        readonly List<IEcsPreInitSystem> _preInitSystems = new List<IEcsPreInitSystem> (4);

        /// <summary>
        /// Registered IEcsInitSystem systems.
        /// </summary>
        readonly List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem> (8);

        /// <summary>
        /// Registered IEcsRunSystem systems.
        /// </summary>
        readonly List<IEcsRunSystem> _runSystems = new List<IEcsRunSystem> (16);

#if DEBUG
        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;
#endif

        public EcsSystems (EcsWorld world) {
#if DEBUG
            if (world == null) {
                throw new ArgumentNullException ();
            }
#endif
            _world = world;
        }

#if DEBUG
        /// <summary>
        /// Adds external event listener.
        /// </summary>
        /// <param name="observer">Event listener.</param>
        public void AddDebugListener (IEcsSystemsDebugListener observer) {
            if (_debugListeners.Contains (observer)) {
                throw new Exception ("Listener already exists");
            }
            _debugListeners.Add (observer);
        }

        /// <summary>
        /// Removes external event listener.
        /// </summary>
        /// <param name="observer">Event listener.</param>
        public void RemoveDebugListener (IEcsSystemsDebugListener observer) {
            _debugListeners.Remove (observer);
        }
#endif

        /// <summary>
        /// Gets all pre-init systems.
        /// </summary>
        /// <param name="list">List to put results in it.</param>
        public void GetPreInitSystems (List<IEcsPreInitSystem> list) {
            if (list != null) {
                list.Clear ();
                list.AddRange (_preInitSystems);
            }
        }

        /// <summary>
        /// Gets all init systems.
        /// </summary>
        /// <param name="list">List to put results in it.</param>
        public void GetInitSystems (List<IEcsInitSystem> list) {
            if (list != null) {
                list.Clear ();
                list.AddRange (_initSystems);
            }
        }

        /// <summary>
        /// Gets all run systems.
        /// </summary>
        /// <param name="list">List to put results in it.</param>
        public void GetRunSystems (List<IEcsRunSystem> list) {
            if (list != null) {
                list.Clear ();
                list.AddRange (_runSystems);
            }
        }

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsSystems Add (IEcsSystem system) {
#if DEBUG
            if (system == null) {
                throw new ArgumentNullException ();
            }
#endif
            EcsInjections.Inject (_world, system);

            var preInitSystem = system as IEcsPreInitSystem;
            if (preInitSystem != null) {
                _preInitSystems.Add (preInitSystem);
            }

            var initSystem = system as IEcsInitSystem;
            if (initSystem != null) {
                _initSystems.Add (initSystem);
            }

            var runSystem = system as IEcsRunSystem;
            if (runSystem != null) {
                _runSystems.Add (runSystem);
            }
            return this;
        }

        /// <summary>
        /// Closes registration for new systems, initialize all registered.
        /// </summary>
        public void Initialize () {
#if DEBUG
            if (_inited) {
                throw new Exception ("Group already initialized.");
            }
            _inited = true;
#endif
            for (var i = 0; i < _preInitSystems.Count; i++) {
                _preInitSystems[i].PreInitialize ();
                _world.ProcessDelayedUpdates ();
            }
            for (var i = 0; i < _initSystems.Count; i++) {
                _initSystems[i].Initialize ();
                _world.ProcessDelayedUpdates ();
            }
        }

        /// <summary>
        /// Destroys all registered external data, full cleanup for internal data.
        /// </summary>
        public void Destroy () {
#if DEBUG
            if (!_inited) {
                throw new Exception ("Group not initialized.");
            }
            for (var i = _debugListeners.Count - 1; i >= 0; i--) {
                _debugListeners[i].OnSystemsDestroyed ();
            }
            _debugListeners.Clear ();
#endif

            for (var i = 0; i < _initSystems.Count; i++) {
                _initSystems[i].Destroy ();
            }
            for (var i = 0; i < _preInitSystems.Count; i++) {
                _preInitSystems[i].PreDestroy ();
            }

            _initSystems.Clear ();
            _runSystems.Clear ();
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems.
        /// </summary>
        public void Run () {
#if DEBUG
            if (!_inited) {
                throw new Exception ("Group not initialized.");
            }
            if (!IsRunActive) { return; }
#endif
            for (var i = 0; i < _runSystems.Count; i++) {
                _runSystems[i].Run ();
                _world.ProcessDelayedUpdates ();
            }
        }
    }
}