namespace VoxelEngine.Graphics
{
    using VoxelEngine.Scenes;

    public readonly struct RenderComponentComparer : IComparer<IRenderComponent>
    {
        public static readonly RenderComponentComparer Instance = new();

        public int Compare(IRenderComponent? x, IRenderComponent? y)
        {
            if (x == null || y == null) return 0;
            return x.QueueIndex.CompareTo(y.QueueIndex);
        }
    }

    public class RenderQueues
    {
        private static readonly RenderQueueIndex[] indices = Enum.GetValues<RenderQueueIndex>();
        private readonly Dictionary<RenderQueueIndex, List<IRenderComponent>> stageComponents = [];

        public RenderQueues()
        {
            foreach (RenderQueueIndex index in Enum.GetValues<RenderQueueIndex>())
            {
                stageComponents[index] = [];
            }
        }

        public List<IRenderComponent> this[RenderQueueIndex index] => stageComponents[index];

        public void Add(IRenderComponent component)
        {
            foreach (var index in indices)
            {
                if (component.QueueIndex <= (int)index)
                {
                    var stage = stageComponents[index];
                    int insertIndex = stage.BinarySearch(component, RenderComponentComparer.Instance);
                    if (insertIndex < 0) insertIndex = ~insertIndex;
                    stage.Insert(insertIndex, component);
                    break;
                }
            }
        }

        public void Remove(IRenderComponent component)
        {
            foreach (var list in stageComponents.Values)
            {
                list.Remove(component);
            }
        }
    }
}