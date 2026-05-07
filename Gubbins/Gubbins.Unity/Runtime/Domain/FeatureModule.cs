using Gubbins.Collection;
using Gubbins.Domain;
using Gubbins.Event;

namespace Gubbins.Unity
{
    public interface IFeature
    {
        int Priority { get; }
        bool Enable { get; set; }
        void Evaluate(IDomain category, IModule module);
    }

    public class FeatureManager
    {
        internal const string DEPENDENCY_NAME = "DefaultFeatureManager";

        internal static readonly RedBlackTree<IFeature> Cache = new(new FeatureComparer());
        public static void AddFeature(IFeature feature) => Cache.Add(feature);
        public static bool RemoveFeature(IFeature feature) => Cache.Remove(feature);

        private class FeatureComparer : IComparer<IFeature>
        {
            public int Compare(IFeature x, IFeature y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }

    [Module(typeof(Domain.Domain))]
    public partial class FeatureModule : IModule
    {
        [Event(typeof(DomainEvents.AfterModuleAdded))]
        private void Initialize(IDomain domain, IModule module)
        {
            foreach (var feature in FeatureManager.Cache)
            {
                if (feature.Enable)
                {
                    feature.Evaluate(domain, module);
                }
            }
        }
    }
}