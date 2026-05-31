using System;
using UnityEngine;

namespace Gubbins.Entities
{
    [Serializable]
    public struct ComponentSet
    {
        [SerializeReference]
        public IComponent[] Components;
    }
}