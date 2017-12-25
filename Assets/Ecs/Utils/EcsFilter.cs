using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsFilter {
        /// <summary>
        /// Components mask for filtering.
        /// Do not change it manually!
        /// </summary>
        public readonly EcsComponentMask Mask;

        /// <summary>
        /// List of filtered entities.
        /// Do not change it manually!
        /// </summary>
        public readonly List<int> Entities = new List<int> (512);

        public EcsFilter (EcsComponentMask mask) {
            Mask = mask;
        }

        public override string ToString () {
            return string.Format ("Filter({0})", Mask);
        }
    }
}