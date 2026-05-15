// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/05/22-22:12:22
// Github: https://github.com/Gatongone

using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Serialized;
using UnityEngine;

namespace Gubbins.Unity
{
    [Serializable]
    public sealed class EntityAdapter : MonoBehaviour
    {
        [SerializeField, TypeFrom(typeof(ICategory), TypeKind.Implementation | TypeKind.Class | TypeKind.Newable)]
        private SerializedType m_Category = typeof(Category);
        [SerializeField]
        private UnityEngine.Object[] m_Components = Array.Empty<UnityEngine.Object>();
        private EntityManager m_Allocator;
        // public Entity Entity => m_Entity.Value;

        private void Awake()
        {
            m_Allocator = Category.GetCategory(m_Category).Context.Resolve<IEntityAllocator>() as EntityManager;
            if (m_Allocator == null) return;
        }

        private void Reset()
        {
            var list = new List<UnityEngine.Object> {gameObject};
            list.AddRange(GetComponents<Component>());
            list.Remove(this);
            m_Components = list.ToArray();
        }
    }
}