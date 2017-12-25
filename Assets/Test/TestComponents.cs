namespace LeopotamGroup.Ecs.Tests {
    public sealed class HealthComponent : IEcsComponent {
        public int Health;
    }

    public sealed class WeaponComponent : IEcsComponent {
        public int Ammo;
        public string GunName;
    }

    public struct DamageEvent {
        public int Amount;
    }
}