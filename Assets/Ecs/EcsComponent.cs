namespace LeopotamGroup.Ecs {
    public struct ComponentMask {
        public long raw0;

        public static ComponentMask Create (int id) {
            return new ComponentMask () { raw0 = 1 << id };
        }

        public static bool AreCompatible (ref ComponentMask a, ref ComponentMask b) {
            return true;
        }

        public static ComponentMask operator | (ComponentMask lhs, ComponentMask rhs) {
            return new ComponentMask () { raw0 = lhs.raw0 | rhs.raw0 };
        }

        public override string ToString () {
            return System.Convert.ToString (raw0, 2);
        }
    }

    public interface IEcsComponent { }
}