// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Basic interface for filter events processing.
    /// </summary>
    public interface IEcsFilterListener {
        void OnFilterEntityAdded (int entity, object reason);
        void OnFilterEntityRemoved (int entity, object reason);
        void OnFilterEntityUpdated (int entity, object reason);
    }

    /// <summary>
    /// Container for filtered entities based on specified conditions.
    /// </summary>
    public sealed class EcsFilter {
        /// <summary>
        /// Components mask for filtering entities with required components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask IncludeMask;

        /// <summary>
        /// Components mask for filtering entities with denied components.
        /// Do not change it manually!
        /// </summary>
        internal readonly EcsComponentMask ExcludeMask;

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

        IEcsFilterListener[] _listeners = new IEcsFilterListener[4];

        int _listenersCount;

        /// <summary>
        /// Adds listener to events procesing.
        /// </summary>
        /// <param name="listener">External listener.</param>
        public void AddListener (IEcsFilterListener listener) {
#if DEBUG
            if (listener == null) {
                throw new System.ArgumentNullException ();
            }

            for (var i = 0; i < _listenersCount; i++) {
                if (_listeners[i] == listener) {
                    throw new System.Exception ("Listener already added");
                }
            }
#endif
            if (_listenersCount == _listeners.Length) {
                Array.Resize (ref _listeners, _listenersCount << 1);
            }
            _listeners[_listenersCount++] = listener;
        }

        /// <summary>
        /// Removes listener from events procesing.
        /// </summary>
        /// <param name="listener">External listener.</param>
        public void RemoveListener (IEcsFilterListener listener) {
            if (listener != null) {
                for (var i = _listenersCount - 1; i >= 0; i--) {
                    if (_listeners[i] == listener) {
                        _listenersCount--;
                        Array.Copy (_listeners, i + 1, _listeners, i, _listenersCount - i);
                        break;
                    }
                }
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal void RaiseOnAddEvent (int entity, object reason) {
            if (Entities.Length == EntitiesCount) {
                Array.Resize (ref Entities, EntitiesCount << 1);
            }
            Entities[EntitiesCount++] = entity;
            for (var i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityAdded (entity, reason);
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal void RaiseOnRemoveEvent (int entity, object reason) {
            var i = EntitiesCount - 1;
            for (; i >= 0; i--) {
                if (Entities[i] == entity) {
                    break;
                }
            }
            if (i != -1) {
                EntitiesCount--;
                Array.Copy (Entities, i + 1, Entities, i, EntitiesCount - i);
            }
            for (i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityRemoved (entity, reason);
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal void RaiseOnUpdateEvent (int entity, object reason) {
            for (var i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityUpdated (entity, reason);
            }
        }

        internal EcsFilter (EcsComponentMask include, EcsComponentMask exclude) {
            IncludeMask = include;
            ExcludeMask = exclude;
        }
#if DEBUG
        public override string ToString () {
            return string.Format ("Filter(+{0} -{1})", IncludeMask, ExcludeMask);
        }
#endif
    }
}