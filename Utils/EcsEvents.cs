using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Events processing helper. For internal use.
    /// </summary>
    sealed class EcsEvents {
        const int MaxCallDepth = 2;

        readonly Dictionary<int, Delegate> _events = new Dictionary<int, Delegate> (64);

        int _eventsInCall;

        /// <summary>
        /// Subscribe callback to be raised on specific event.
        /// </summary>
        /// <param name="eventAction">Callback.</param>
        public void Subscribe<T> (Action<T> eventAction) where T : struct {
            if (eventAction != null) {
                var eventType = typeof (T).GetHashCode ();
                Delegate rawList;
                _events.TryGetValue (eventType, out rawList);
                _events[eventType] = (rawList as Action<T>) + eventAction;
            }
        }

        /// <summary>
        /// Unsubscribe callback.
        /// </summary>
        /// <param name="eventAction">Event action.</param>
        /// <param name="keepEvent">GC optimization - clear only callback list and keep event for future use.</param>
        public void Unsubscribe<T> (Action<T> eventAction, bool keepEvent = false) where T : struct {
            if (eventAction != null) {
                var eventType = typeof (T).GetHashCode ();
                Delegate rawList;
                if (_events.TryGetValue (eventType, out rawList)) {
                    var list = (rawList as Action<T>) - eventAction;
                    if (list == null && !keepEvent) {
                        _events.Remove (eventType);
                    } else {
                        _events[eventType] = list;
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribe all listeneres and clear all events.
        /// </summary>
        public void UnsubscribeAndClearAllEvents () {
            _events.Clear ();
        }

        /// <summary>
        /// Publish event.
        /// </summary>
        /// <param name="eventMessage">Event message.</param>
        public void Publish<T> (T eventMessage) where T : struct {
            if (_eventsInCall >= MaxCallDepth) {
                throw new Exception ("Max call depth reached");
            }
            var eventType = typeof (T).GetHashCode ();
            Delegate rawList;
            _events.TryGetValue (eventType, out rawList);
            var list = rawList as Action<T>;
            if (list != null) {
                _eventsInCall++;
                list (eventMessage);
                _eventsInCall--;
            }
        }
    }
}