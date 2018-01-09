// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Mask for components selection.
    /// </summary>
    public sealed class EcsComponentMask {
        const int RawLength = 4;

        const int RawItemSize = sizeof (ulong) * 8;

        public const int BitsCount = RawLength * RawItemSize;

        readonly ulong[] _raw = new ulong[RawLength];

        public override string ToString () {
            var str = "";
            for (int i = 0; i < RawLength; i++) {
                str += _raw[i].ToString ("{X16}");
            }
            return str;
        }

        public void SetBit (int bitId, bool state) {
#if DEBUG && !ECS_PERF_TEST
            if (bitId < 0 || bitId >= BitsCount) { throw new System.Exception ("Invalid bit"); }
#endif
            if (state) {
                _raw[bitId / RawItemSize] |= 1UL << (bitId % RawItemSize);
            } else {
                _raw[bitId / RawItemSize] &= ~(1UL << (bitId % RawItemSize));
            }
        }

        public bool IsEmpty () {
            for (var i = 0; i < RawLength; i++) {
                if (_raw[i] != 0) {
                    return false;
                }
            }
            return true;
        }

        public bool GetBit (int bitId) {
#if DEBUG && !ECS_PERF_TEST
            if (bitId < 0 || bitId >= BitsCount) { throw new System.Exception ("Invalid bit"); }
#endif
            return (_raw[bitId / RawItemSize] & (1UL << (bitId % RawItemSize))) != 0;
        }

        public void CopyFrom (EcsComponentMask mask) {
            for (var i = 0; i < RawLength; i++) {
                _raw[i] = mask._raw[i];
            }
        }

        public bool IsEquals (EcsComponentMask mask) {
            for (var i = 0; i < RawLength; i++) {
                if (_raw[i] != mask._raw[i]) {
                    return false;
                }
            }
            return true;
        }

        public bool IsCompatible (EcsComponentMask include, EcsComponentMask exclude) {
            ulong a;
            ulong b;
            for (var i = 0; i < RawLength; i++) {
                a = _raw[i];
                b = include._raw[i];
                if ((a & b) != b || (a & exclude._raw[i]) != 0) {
                    return false;
                }
            }
            return true;
        }

        public bool IsIntersects (EcsComponentMask mask) {
            for (var i = 0; i < RawLength; i++) {
                if ((_raw[i] & mask._raw[i]) != 0) {
                    return true;
                }
            }
            return false;
        }
    }
}