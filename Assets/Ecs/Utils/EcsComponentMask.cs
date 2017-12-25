namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Mask for components selection.
    /// </summary>
    public struct EcsComponentMask {
        ulong _raw0;

        ulong _raw1;

        public EcsComponentMask (int bitId) {
            CheckBitId (bitId);
            if (bitId >= 64) {
                _raw0 = 0;
                _raw1 = 1UL << (bitId - 64);
            } else {
                _raw0 = 1UL << bitId;
                _raw1 = 0;
            }
        }

        public override string ToString () {
            return string.Format ("{0:X16}{1:X16}", _raw1, _raw0);
        }

        public void SetBit (int bitId, bool state) {
            CheckBitId (bitId);
            if (state) {
                if (bitId >= 64) {
                    _raw1 |= 1UL << (bitId - 64);
                } else {
                    _raw0 |= 1UL << bitId;
                }
            } else {
                if (bitId >= 64) {
                    _raw1 &= ~(1UL << (bitId - 64));
                } else {
                    _raw0 &= ~(1UL << bitId);
                }
            }
        }

        public bool IsEmpty () {
            return _raw0 == 0 && _raw1 == 0;
        }

        public bool GetBit (int bitId) {
            CheckBitId (bitId);
            if (bitId >= 64) {
                return (_raw1 & (1UL << (bitId - 64))) != 0;
            } else {
                return (_raw0 & (1UL << bitId)) != 0;
            }
        }

        public bool IsEquals (EcsComponentMask a) {
            return _raw0 == a._raw0 && _raw1 == a._raw1;
        }

        public bool IsCompatible (EcsComponentMask a) {
            return (_raw0 & a._raw0) == a._raw0 && (_raw1 & a._raw1) == a._raw1;
        }

        [System.Diagnostics.Conditional ("DEBUG")]
        static void CheckBitId (int bitId) {
            if (bitId < 0 || bitId >= 128) {
                throw new System.Exception ("Invalid bit");
            }
        }
    }
}