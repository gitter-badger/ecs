namespace LeopotamGroup.Ecs {
    public struct ComponentMask {
        ulong _raw0;

        public ComponentMask (int bitId) {
            _raw0 = 1UL << bitId;
        }

        public override string ToString () {
            return System.Convert.ToString ((long) _raw0, 2);
        }

        public void SetBit (int id, bool state) {
            if (state) {
                _raw0 |= 1UL << id;
            } else {
                _raw0 &= ~(1UL << id);
            }
        }

        public bool IsEmpty () {
            return _raw0 == 0;
        }

        public bool GetBit (int id) {
            return (_raw0 & (1UL << id)) != 0;
        }

        public bool IsEquals (ComponentMask a) {
            return _raw0 == a._raw0;
        }

        public bool IsCompatible (ComponentMask a) {
            return (_raw0 & a._raw0) == a._raw0;
        }
    }

    public interface IEcsComponent { }
}