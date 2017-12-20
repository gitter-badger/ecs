using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsFilter {
        public readonly ComponentMask Mask;

        public readonly bool ForEvents;

        /// <summary>
        /// Do not change it manually.
        /// </summary>
        public readonly List<int> Entities = new List<int> (512);

        public EcsFilter (ComponentMask mask, bool forEvents) {
            Mask = mask;
            ForEvents = forEvents;
        }
    }
}