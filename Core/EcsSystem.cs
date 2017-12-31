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
    public interface IEcsInitSystem {
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
    /// Allows integration to unity Update() state.
    /// </summary>
    public interface IEcsUpdateSystem {
        /// <summary>
        /// Will be called on unity Update() stage.
        /// </summary>
        void Update ();
    }

    /// <summary>
    /// Allows integration to unity FixedUpdate() state.
    /// </summary>
    public interface IEcsFixedUpdateSystem {
        /// <summary>
        /// Will be called on unity FixedUpdate() stage.
        /// </summary>
        void FixedUpdate ();
    }
}