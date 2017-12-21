namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Mask for components selection.
    /// </summary>
    public struct EcsComponentMask {
        ulong _raw0;

        public EcsComponentMask (int bitId) {
            CheckBitId (bitId);
            _raw0 = 1UL << bitId;
        }

        public override string ToString () {
            return string.Format ("{0:X16}", _raw0);
        }

        public void SetBit (int bitId, bool state) {
            CheckBitId (bitId);
            if (state) {
                _raw0 |= 1UL << bitId;
            } else {
                _raw0 &= ~(1UL << bitId);
            }
        }

        public bool IsEmpty () {
            return _raw0 == 0;
        }

        public bool GetBit (int id) {
            return (_raw0 & (1UL << id)) != 0;
        }

        public bool IsEquals (EcsComponentMask a) {
            return _raw0 == a._raw0;
        }

        public bool IsCompatible (EcsComponentMask a) {
            return (_raw0 & a._raw0) == a._raw0;
        }

        [System.Diagnostics.Conditional ("DEBUG")]
        static void CheckBitId (int bitId) {
            if (bitId < 0 || bitId >= 64) {
                throw new System.Exception ("Invalid bit");
            }
        }
    }
}