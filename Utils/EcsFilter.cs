// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LeopotamGroup.Ecs.Internals;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Basic interface for filter events processing.
    /// </summary>
    public interface IEcsFilterListener {
        void OnFilterEntityAdded (int entity);
        void OnFilterEntityRemoved (int entity);
        void OnFilterEntityUpdated (int entity);
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
        /// List of filtered entities.
        /// Do not change it manually!
        /// </summary>
        public readonly List<int> Entities = new List<int> (64);

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
                var newListeners = new IEcsFilterListener[_listenersCount << 1];
                Array.Copy (_listeners, 0, newListeners, 0, _listenersCount);
                _listeners = newListeners;
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
        internal void RaiseOnEntityAdded (int entity) {
            for (var i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityAdded (entity);
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal void RaiseOnEntityRemoved (int entity) {
            for (var i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityRemoved (entity);
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal void RaiseOnEntityUpdated (int entity) {
            for (var i = 0; i < _listenersCount; i++) {
                _listeners[i].OnFilterEntityUpdated (entity);
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