namespace LeopotamGroup.Ecs {
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

    /// <summary>
    /// Base interface for all ecs systems.
    /// </summary>
    public interface IEcsSystem {
        /// <summary>
        /// Initializes system inside EcsWorld instance.
        /// </summary>
        /// <param name="world">EcsWorld instance.</param>
        void Initialize (EcsWorld world);

        /// <summary>
        /// Destroys all internal allocated data.
        /// </summary>
        void Destroy ();
    }
}