using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Base interface for all ecs components.
    /// </summary>
    public interface IEcsComponent { }

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
                throw new Exception ("Invalid bit");
            }
        }
    }

    /// <summary>
    /// Component pool container.
    /// </summary>
    sealed class EcsComponentPool {
        readonly Stack<IEcsComponent> _pool = new Stack<IEcsComponent> (512);

        readonly Type _type;

        public EcsComponentPool (Type type) {
            _type = type;
        }

        public IEcsComponent Get () {
            return _pool.Count > 0 ? _pool.Pop () : Activator.CreateInstance (_type) as IEcsComponent;
        }

        public void Recycle (IEcsComponent item) {
            if (item != null) {
                _pool.Push (item);
            }
        }

        public int GetCachedCount () {
            return _pool.Count;
        }
    }
}