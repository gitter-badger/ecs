using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsWorld {
        /// <summary>
        /// All registered systems.
        /// </summary>
        readonly List<EcsSystem> _allSystems = new List<EcsSystem> (64);

        /// <summary>
        /// Dictionary for fast search component <-> bitmask for component type.
        /// </summary>
        readonly Dictionary<Type, ComponentMask> _componentMasks = new Dictionary<Type, ComponentMask> (64);

        /// <summary>
        /// List of all entities (their components).
        /// </summary>
        readonly List<IEcsComponent[]> _entities = new List<IEcsComponent[]> (1024);

        /// <summary>
        /// List of removed entities - they can be reused later.
        /// </summary>
        readonly List<int> _reservedEntities = new List<int> (256);

        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld AddSystem (EcsSystem system) {
            if (_inited) {
                throw new Exception ("Already initialized, cant add new system.");
            }
            system.SetWorld (this);
            _allSystems.Add (system);
            return this;
        }

        /// <summary>
        /// Closes registration for new external data, initialize all registered systems.
        /// </summary>
        public void Initialize () {
            _inited = true;
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                _allSystems[i].Initialize ();
            }
        }

        /// <summary>
        /// Destroys all registered external data, full cleanup for internal data.
        /// </summary>
        public void Destroy () {
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                _allSystems[i].Destroy ();
            }
            _allSystems.Clear ();
            _componentMasks.Clear ();
            _entities.Clear ();
            _reservedEntities.Clear ();
        }

        /// <summary>
        /// Processing for IEcsUpdateSystem systems.
        /// </summary>
        public void Update () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.Update ();
                }
            }
        }

        /// <summary>
        /// Processing for IEcsFixedUpdateSystem systems.
        /// </summary>
        public void FixedUpdate () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsFixedUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.FixedUpdate ();
                }
            }
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        public int CreateEntity () {
            int entity;
            if (_reservedEntities.Count > 0) {
                var id = _reservedEntities.Count - 1;
                entity = _reservedEntities[id];
                _reservedEntities.RemoveAt (id);
            } else {
                entity = _entities.Count;
                _entities.Add (new IEcsComponent[_componentMasks.Count]);
            }
            return entity;
        }

        /// <summary>
        /// Removes exists entity or throws exception on invalid one.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveEntity (int entity) {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            if (_reservedEntities.IndexOf (entity) != -1) {
                throw new Exception ("Entity already removed");
            }
            _reservedEntities.Add (entity);
        }

        /// <summary>
        /// Adds component to entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public T AddComponent<T> (int entity) where T : IEcsComponent {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            throw new NotImplementedException ();
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveComponent<T> (int entity) where T : IEcsComponent {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            throw new NotImplementedException ();
        }

        /// <summary>
        /// For internal use only, dont call it directly.
        /// </summary>
        public ComponentMask GetComponentsMask (Type[] types) {
            if (_inited) {
                throw new Exception ("Initialize method already called.");
            }
            var mask = new ComponentMask ();
            if (types != null) {
                for (var i = 0; i < types.Length; i++) {
                    mask |= GetComponentMask (types[i]);
                }
            }
            return mask;
        }

        /// <summary>
        /// For internal use only, dont call it directly.
        /// </summary>
        public string GetDebugStats () {
            return string.Format ("Components: {0}\nEntities: {1}", _componentMasks.Count, _entities.Count);
        }

        ComponentMask GetComponentMask (Type type) {
            if (!typeof (IEcsComponent).IsAssignableFrom (type)) {
                throw new Exception ("Invalid component");
            }
            ComponentMask retVal;
            if (!_componentMasks.TryGetValue (type, out retVal)) {
                retVal = ComponentMask.Create (_componentMasks.Count);
                _componentMasks[type] = retVal;
            }
            return retVal;
        }
    }
}