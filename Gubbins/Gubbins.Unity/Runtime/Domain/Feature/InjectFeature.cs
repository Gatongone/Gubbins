// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/24-01:29:20
// Github: https://github.com/Gatongone

using Gubbins.Context;

namespace Gubbins.Unity
{
    internal class InjectFeature : IFeature
    {
        public bool Enable { get; set; } = true;
        public int Priority => int.MinValue + 1;

        public void Evaluate(ICategory category, ISystem system)
        {
            category.Context.Inject(system);
        }
    }
}