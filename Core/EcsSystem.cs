// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

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
        FixedUpdate,
        LateUpdate
    }
}