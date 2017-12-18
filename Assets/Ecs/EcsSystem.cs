namespace LeopotamGroup.Ecs {
    public interface IEcsUpdatableSystem {
        void Update ();
    }

    public interface IEcsFixedUpdatableSystem {
        void FixedUpdate ();
    }

    public interface IEcsDestroyableSystem {
        void Destroy ();
    }

    public interface IEcsSystem {
        void Initialize (EcsWorld world);
    }
}