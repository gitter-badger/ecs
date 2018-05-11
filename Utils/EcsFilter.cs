// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    public class EcsFilter<Inc1> : EcsFilter where Inc1 : class, new () {
        public Inc1[] Components1 = new Inc1[32];

        internal EcsFilter () {
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (EcsWorld world, int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                Array.Resize (ref Components1, EntitiesCount << 1);
            }
            Components1[EntitiesCount] = world.GetComponent<Inc1> (entity);
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
                    Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (1, 1);
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (1, 2);
            }
        }
    }

    public class EcsFilter<Inc1, Inc2> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () {
        public Inc1[] Components1 = new Inc1[32];
        public Inc2[] Components2 = new Inc2[32];

        internal EcsFilter () {
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (2, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (EcsWorld world, int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                Array.Resize (ref Components1, EntitiesCount << 1);
                Array.Resize (ref Components2, EntitiesCount << 1);
            }
            Components1[EntitiesCount] = world.GetComponent<Inc1> (entity);
            Components2[EntitiesCount] = world.GetComponent<Inc2> (entity);
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
                    Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (2, 1);
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (2, 2);
            }
        }
    }

    public class EcsFilter<Inc1, Inc2, Inc3> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () {
        public Inc1[] Components1 = new Inc1[32];
        public Inc2[] Components2 = new Inc2[32];
        public Inc3[] Components3 = new Inc3[32];

        internal EcsFilter () {
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc3>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (3, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (EcsWorld world, int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                Array.Resize (ref Components1, EntitiesCount << 1);
                Array.Resize (ref Components2, EntitiesCount << 1);
                Array.Resize (ref Components3, EntitiesCount << 1);
            }
            Components1[EntitiesCount] = world.GetComponent<Inc1> (entity);
            Components2[EntitiesCount] = world.GetComponent<Inc2> (entity);
            Components3[EntitiesCount] = world.GetComponent<Inc3> (entity);
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
                    Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (3, 1);
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (3, 2);
            }
        }
    }

    public class EcsFilter<Inc1, Inc2, Inc3, Inc4> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () where Inc4 : class, new () {
        public Inc1[] Components1 = new Inc1[32];
        public Inc2[] Components2 = new Inc2[32];
        public Inc3[] Components3 = new Inc3[32];
        public Inc4[] Components4 = new Inc4[32];

        internal EcsFilter () {
            IncludeMask.SetBit (EcsComponentPool<Inc1>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc2>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc3>.Instance.GetComponentTypeIndex (), true);
            IncludeMask.SetBit (EcsComponentPool<Inc4>.Instance.GetComponentTypeIndex (), true);
            ValidateMasks (4, 0);
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal override void RaiseOnAddEvent (EcsWorld world, int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                Array.Resize (ref Components1, EntitiesCount << 1);
                Array.Resize (ref Components2, EntitiesCount << 1);
                Array.Resize (ref Components3, EntitiesCount << 1);
                Array.Resize (ref Components4, EntitiesCount << 1);
            }
            Components1[EntitiesCount] = world.GetComponent<Inc1> (entity);
            Components2[EntitiesCount] = world.GetComponent<Inc2> (entity);
            Components3[EntitiesCount] = world.GetComponent<Inc3> (entity);
            Components4[EntitiesCount] = world.GetComponent<Inc4> (entity);
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
                    Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    Array.Copy (Components4, i + 1, Components4, i, EntitiesCount - i);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (4, 1);
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (4, 2);
            }
        }
    }

    public class EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> : EcsFilter where Inc1 : class, new () where Inc2 : class, new () where Inc3 : class, new () where Inc4 : class, new () where Inc5 : class, new () {
        public Inc1[] Components1 = new Inc1[32];
        public Inc2[] Components2 = new Inc2[32];
        public Inc3[] Components3 = new Inc3[32];
        public Inc4[] Components4 = new Inc4[32];
        public Inc5[] Components5 = new Inc5[32];

        internal EcsFilter () {
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
        internal override void RaiseOnAddEvent (EcsWorld world, int entity) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
                Array.Resize (ref Components1, EntitiesCount << 1);
                Array.Resize (ref Components2, EntitiesCount << 1);
                Array.Resize (ref Components3, EntitiesCount << 1);
                Array.Resize (ref Components4, EntitiesCount << 1);
                Array.Resize (ref Components5, EntitiesCount << 1);
            }
            Components1[EntitiesCount] = world.GetComponent<Inc1> (entity);
            Components2[EntitiesCount] = world.GetComponent<Inc2> (entity);
            Components3[EntitiesCount] = world.GetComponent<Inc3> (entity);
            Components4[EntitiesCount] = world.GetComponent<Inc4> (entity);
            Components5[EntitiesCount] = world.GetComponent<Inc5> (entity);
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
                    Array.Copy (Components1, i + 1, Components1, i, EntitiesCount - i);
                    Array.Copy (Components2, i + 1, Components2, i, EntitiesCount - i);
                    Array.Copy (Components3, i + 1, Components3, i, EntitiesCount - i);
                    Array.Copy (Components4, i + 1, Components4, i, EntitiesCount - i);
                    Array.Copy (Components5, i + 1, Components5, i, EntitiesCount - i);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> where Exc1 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (5, 1);
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4, Inc5> where Exc1 : class, new () where Exc2 : class, new () {
            internal Exclude () {
                ExcludeMask.SetBit (EcsComponentPool<Exc1>.Instance.GetComponentTypeIndex (), true);
                ExcludeMask.SetBit (EcsComponentPool<Exc2>.Instance.GetComponentTypeIndex (), true);
                ValidateMasks (5, 2);
            }
        }
    }

    /// <summary>
    /// Container for filtered entities based on specified conditions.
    /// </summary>
    public abstract class EcsFilter {
        /// <summary>
        /// Components mask for filtering entities with required components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask IncludeMask = new EcsComponentMask ();

        /// <summary>
        /// Components mask for filtering entities with denied components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask ExcludeMask = new EcsComponentMask ();

        internal abstract void RaiseOnAddEvent (EcsWorld world, int entity);

        internal abstract void RaiseOnRemoveEvent (int entity);

        /// <summary>
        /// Storage of filtered entities.
        /// Important: Length of this storage can be larger than real amount of items,
        /// use EntitiesCount instead of Entities.Length!
        /// Do not change it manually!
        /// </summary>
        public int[] Entities = new int[32];

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