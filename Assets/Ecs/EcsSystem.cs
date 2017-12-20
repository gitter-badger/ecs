namespace LeopotamGroup.Ecs {
    public interface IEcsUpdateSystem {
        void Update ();
    }

    public interface IEcsFixedUpdateSystem {
        void FixedUpdate ();
    }

    public interface IEcsSystem {
        void Initialize (EcsWorld world);
        void Destroy ();
    }
}