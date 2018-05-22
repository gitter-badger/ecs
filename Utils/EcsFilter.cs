// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Marks component class to be not autofilled as ComponentX in filter.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class EcsIgnoreInFilterAttribute : Attribute { }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public class EcsFilter<Inc1> : EcsFilter where Inc1 : class, new () {
        public Inc1[] Components1;
        bool _allow1;

        internal EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            Components1 = _allow1 ? new Inc1[MinSize] : null;
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                if (_allow1) {
                    Array.Resize (ref Components1, EntitiesCount << 1);
                }
            }
            if (_allow1) {
                Components1[EntitiesCount] = _world.GetComponent<Inc1> (entity);
            }
            Entities[EntitiesCount++] = entity;
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnRemoveEvent (int entity) {
            for (var i = 0; i < EntitiesCount; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
                    if (_allow1) {
                        Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1> : EcsFilter<Inc1> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (1, 1);
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (1, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public class EcsFilter<Inc1, Inc2> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () {
        public Inc1[] Components1;
        public Inc2[] Components2;
        bool _allow1;
        bool _allow2;

        internal EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            Components1 = _allow1 ? new Inc1[MinSize] : null;
            Components2 = _allow2 ? new Inc2[MinSize] : null;
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (2, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                if (_allow1) {
                    Array.Resize (ref Components1, EntitiesCount << 1);
                }
                if (_allow2) {
                    Array.Resize (ref Components2, EntitiesCount << 1);
                }
            }
            if (_allow1) {
                Components1[EntitiesCount] = _world.GetComponent<Inc1> (entity);
            }
            if (_allow2) {
                Components2[EntitiesCount] = _world.GetComponent<Inc2> (entity);
            }
            Entities[EntitiesCount++] = entity;
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnRemoveEvent (int entity) {
            for (var i = 0; i < EntitiesCount; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
                    if (_allow1) {
                        Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    }
                    if (_allow2) {
                        Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (2, 1);
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (2, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public class EcsFilter<Inc1, Inc2, Inc3> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () {
        public Inc1[] Components1;
        public Inc2[] Components2;
        public Inc3[] Components3;
        bool _allow1;
        bool _allow2;
        bool _allow3;

        internal EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            _allow3 = !EcsComponentPool<Inc3>.Instance.IsIgnoreInFilter;
            Components1 = _allow1 ? new Inc1[MinSize] : null;
            Components2 = _allow2 ? new Inc2[MinSize] : null;
            Components3 = _allow3 ? new Inc3[MinSize] : null;
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc3>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (3, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                if (_allow1) {
                    Array.Resize (ref Components1, EntitiesCount << 1);
                }
                if (_allow2) {
                    Array.Resize (ref Components2, EntitiesCount << 1);
                }
                if (_allow3) {
                    Array.Resize (ref Components3, EntitiesCount << 1);
                }
            }
            if (_allow1) {
                Components1[EntitiesCount] = _world.GetComponent<Inc1> (entity);
            }
            if (_allow2) {
                Components2[EntitiesCount] = _world.GetComponent<Inc2> (entity);
            }
            if (_allow3) {
                Components3[EntitiesCount] = _world.GetComponent<Inc3> (entity);
            }
            Entities[EntitiesCount++] = entity;
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnRemoveEvent (int entity) {
            for (var i = 0; i < EntitiesCount; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
                    if (_allow1) {
                        Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    }
                    if (_allow2) {
                        Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    }
                    if (_allow3) {
                        Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (3, 1);
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (3, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public class EcsFilter<Inc1, Inc2, Inc3, Inc4> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () where Inc4 : class, new () {
        public Inc1[] Components1;
        public Inc2[] Components2;
        public Inc3[] Components3;
        public Inc4[] Components4;
        bool _allow1;
        bool _allow2;
        bool _allow3;
        bool _allow4;

        internal EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            _allow3 = !EcsComponentPool<Inc3>.Instance.IsIgnoreInFilter;
            _allow4 = !EcsComponentPool<Inc4>.Instance.IsIgnoreInFilter;
            Components1 = _allow1 ? new Inc1[MinSize] : null;
            Components2 = _allow2 ? new Inc2[MinSize] : null;
            Components3 = _allow3 ? new Inc3[MinSize] : null;
            Components4 = _allow4 ? new Inc4[MinSize] : null;
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc3>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc4>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (4, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                if (_allow1) {
                    Array.Resize (ref Components1, EntitiesCount << 1);
                }
                if (_allow2) {
                    Array.Resize (ref Components2, EntitiesCount << 1);
                }
                if (_allow3) {
                    Array.Resize (ref Components3, EntitiesCount << 1);
                }
                if (_allow4) {
                    Array.Resize (ref Components4, EntitiesCount << 1);
                }
            }
            if (_allow1) {
                Components1[EntitiesCount] = _world.GetComponent<Inc1> (entity);
            }
            if (_allow2) {
                Components2[EntitiesCount] = _world.GetComponent<Inc2> (entity);
            }
            if (_allow3) {
                Components3[EntitiesCount] = _world.GetComponent<Inc3> (entity);
            }
            if (_allow4) {
                Components4[EntitiesCount] = _world.GetComponent<Inc4> (entity);
            }
            Entities[EntitiesCount++] = entity;
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnRemoveEvent (int entity) {
            for (var i = 0; i < EntitiesCount; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
                    if (_allow1) {
                        Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    }
                    if (_allow2) {
                        Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    }
                    if (_allow3) {
                        Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    }
                    if (_allow4) {
                        Array.Copy (Components4, i + 1, Components4, i, EntitiesCount - i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (4, 1);
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (4, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public class EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () where Inc4 : class, new () where Inc5 : class, new () {
        public Inc1[] Components1;
        public Inc2[] Components2;
        public Inc3[] Components3;
        public Inc4[] Components4;
        public Inc5[] Components5;
        bool _allow1;
        bool _allow2;
        bool _allow3;
        bool _allow4;
        bool _allow5;

        internal EcsFilter () {
            _allow1 = !EcsComponentPool<Inc1>.Instance.IsIgnoreInFilter;
            _allow2 = !EcsComponentPool<Inc2>.Instance.IsIgnoreInFilter;
            _allow3 = !EcsComponentPool<Inc3>.Instance.IsIgnoreInFilter;
            _allow4 = !EcsComponentPool<Inc4>.Instance.IsIgnoreInFilter;
            _allow5 = !EcsComponentPool<Inc5>.Instance.IsIgnoreInFilter;
            Components1 = _allow1 ? new Inc1[MinSize] : null;
            Components2 = _allow2 ? new Inc2[MinSize] : null;
            Components3 = _allow3 ? new Inc3[MinSize] : null;
            Components4 = _allow4 ? new Inc4[MinSize] : null;
            Components5 = _allow5 ? new Inc5[MinSize] : null;
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc3>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc4>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc5>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (5, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                if (_allow1) {
                    Array.Resize (ref Components1, EntitiesCount << 1);
                }
                if (_allow2) {
                    Array.Resize (ref Components2, EntitiesCount << 1);
                }
                if (_allow3) {
                    Array.Resize (ref Components3, EntitiesCount << 1);
                }
                if (_allow4) {
                    Array.Resize (ref Components4, EntitiesCount << 1);
                }
                if (_allow5) {
                    Array.Resize (ref Components5, EntitiesCount << 1);
                }
            }
            if (_allow1) {
                Components1[EntitiesCount] = _world.GetComponent<Inc1> (entity);
            }
            if (_allow2) {
                Components2[EntitiesCount] = _world.GetComponent<Inc2> (entity);
            }
            if (_allow3) {
                Components3[EntitiesCount] = _world.GetComponent<Inc3> (entity);
            }
            if (_allow4) {
                Components4[EntitiesCount] = _world.GetComponent<Inc4> (entity);
            }
            if (_allow5) {
                Components5[EntitiesCount] = _world.GetComponent<Inc5> (entity);
            }
            Entities[EntitiesCount++] = entity;
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnRemoveEvent (int entity) {
            for (var i = 0; i < EntitiesCount; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
                    if (_allow1) {
                        Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    }
                    if (_allow2) {
                        Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    }
                    if (_allow3) {
                        Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    }
                    if (_allow4) {
                        Array.Copy (Components4, i + 1, Components4, i, EntitiesCount - i);
                    }
                    if (_allow5) {
                        Array.Copy (Components5, i + 1, Components5, i, EntitiesCount - i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (5, 1);
            }
        }

        /// <summary>
        /// Container for filtered entities based on specified constraints.
        /// </summary>
        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (5, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
    public abstract class EcsFilter {
        internal const int MinSize = 32;

        internal readonly EcsComponentMask IncludeMask = new EcsComponentMask ();

        internal readonly EcsComponentMask ExcludeMask = new EcsComponentMask ();

        protected EcsWorld _world;

        internal void SetWorld (EcsWorld world) {
            _world = world;
        }

        internal abstract void RaiseOnAddEvent (int entity);

        internal abstract void RaiseOnRemoveEvent (int entity);

        /// <summary>
        /// Storage of filtered entities.
        /// Important: Length of this storage can be larger than real amount of items,
        /// use EntitiesCount instead of Entities.Length!
        /// Do not change it manually!
        /// </summary>
        public int[] Entities = new int[MinSize];

        /// <summary>
        /// Amount of filtered entities.
        /// </summary>
        public int EntitiesCount;

        [System.Diagnostics.Conditional ("DEBUG")]
        internal void ValidateMasks (int inc, int exc) {
            if (IncludeMask.BitsCount != inc || ExcludeMask.BitsCount != exc) {
                throw new Exception (string.Format ("Invalid filter type \"{0}\": duplicated component types.", GetType ()));
            }
            if (IncludeMask.IsIntersects (ExcludeMask)) {
                throw new Exception (string.Format ("Invalid filter type \"{0}\": Include types intersects with exclude types.", GetType ()));
            }
        }
#if DEBUG
        public override string ToString () {
            return string.Format ("Filter(+{0} -{1})", IncludeMask, ExcludeMask);
        }
#endif
    }
}