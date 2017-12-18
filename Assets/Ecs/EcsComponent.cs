namespace LeopotamGroup.Ecs {
    public struct ComponentMask {
        public long raw0;

        public ComponentMask (int bitId) {
            raw0 = 1 << bitId;
        }

        public override string ToString () {
            return System.Convert.ToString (raw0, 2);
        }

        public void EnableBits (ComponentMask mask) {
            raw0 |= mask.raw0;
        }

        public ComponentMask GetInversedBits () {
            return new ComponentMask () { raw0 = ~this.raw0 };
        }

        public void DisableBits (ComponentMask mask) {
            var inv = mask.GetInversedBits ();
            raw0 &= inv.raw0;
        }

        public static bool AreCompatible (ref ComponentMask a, ref ComponentMask b) {
            return (a.raw0 & b.raw0) == b.raw0;
        }

        public static bool AreEquals (ref ComponentMask a, ref ComponentMask b) {
            return a.raw0 == b.raw0;
        }
    }

    public interface IEcsComponent { }
}