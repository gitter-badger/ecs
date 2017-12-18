using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsFilter {
        /// <summary>
        /// Do not change it manually.
        /// </summary>
        public ComponentMask Mask;

        public readonly List<int> Entities = new List<int> (512);

        public EcsFilter (ComponentMask mask) {
            Mask = mask;
        }
    }
}