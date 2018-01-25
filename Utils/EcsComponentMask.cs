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
        // Can be changed if you need more than 256 components per world.
        // Each number adds room for 64 components. If there are more than
        // 32k components (WUT?!) - you should fix:
        // * EcsWorld.DelayedUpdate.Component field type.
        // * EcsWorld.ComponentLink.PoolId field type.
#if ECS_COMPONENT_LIMIT_2048
        const int RawLength = 32;
#elif ECS_COMPONENT_LIMIT_1024
        const int RawLength = 16;
#elif ECS_COMPONENT_LIMIT_512
        const int RawLength = 8;
#else
        const int RawLength = 4;
#endif

        const int RawItemSize = sizeof (ulong) * 8;

        public const int BitsCount = RawLength * RawItemSize;

        readonly ulong[] _raw = new ulong[RawLength];
#if DEBUG
        public override string ToString () {
            var str = "";
            for (int i = 0; i < RawLength; i++) {
                str += _raw[i].ToString ("X16");
            }
            return str;
        }
#endif

        public void SetBit (int bitId, bool state) {
#if DEBUG
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
#if DEBUG
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