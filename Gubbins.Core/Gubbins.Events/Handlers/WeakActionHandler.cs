using System.Reflection;
using Gubbins.Enhance;

namespace Gubbins.Events;

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler : IEventHandler, IWeakEventHandler, IEquatable<WeakActionEventHandler>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(Unit notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke();

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle() => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke();

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(Unit notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle() { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler other => Equals(other),
            Action action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke();
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider> m_Invocation;

            public ActionInvoker(TProvider provider, Action action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider>)) as Action<TProvider> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke()
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T> : IEventHandler<T>, IWeakEventHandler<T>, IEquatable<WeakActionEventHandler<T>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }



        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T param) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param);



        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T param) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T> other => Equals(other),
            Action<T> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T param);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T>)) as Action<TProvider, T> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T param)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2> : IEventHandler<(T1, T2)>, IWeakEventHandler<(T1, T2)>, IEquatable<WeakActionEventHandler<T1, T2>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2> other => Equals(other),
            Action<T1, T2> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2>)) as Action<TProvider, T1, T2> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3> : IEventHandler<(T1, T2, T3)>, IWeakEventHandler<(T1, T2, T3)>, IEquatable<WeakActionEventHandler<T1, T2, T3>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3> other => Equals(other),
            Action<T1, T2, T3> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3>)) as Action<TProvider, T1, T2, T3> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4> : IEventHandler<(T1, T2, T3, T4)>, IWeakEventHandler<(T1, T2, T3, T4)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4> other => Equals(other),
            Action<T1, T2, T3, T4> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4>)) as Action<TProvider, T1, T2, T3, T4> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5> : IEventHandler<(T1, T2, T3, T4, T5)>, IWeakEventHandler<(T1, T2, T3, T4, T5)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5> other => Equals(other),
            Action<T1, T2, T3, T4, T5> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5>)) as Action<TProvider, T1, T2, T3, T4, T5> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6> : IEventHandler<(T1, T2, T3, T4, T5, T6)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6>)) as Action<TProvider, T1, T2, T3, T4, T5, T6> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13, notification.Item14);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13, notification.Item14); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14);
                return true;
            }
        }
    }

    /// <summary>
    /// Event handler with System.Action which would reference the event provider by weak.
    /// </summary>
    public sealed class WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>, IWeakEventHandler<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>, IEquatable<WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
    {
        /// <summary>
        /// Delegate with instance method provider.
        /// </summary>
        private readonly IInvocation m_Invocation;

        /// <summary>
        /// The owner that control this weak event's lifecycle.
        /// </summary>
        private readonly WeakReference<IEquatable<DBNull>> m_Owner;

        /// <summary>
        /// Raw method of the event provider.
        /// </summary>
        private readonly MethodInfo m_Method;

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.IsAlive"/>
        public bool IsAlive => m_Owner.TryGetTarget(out _);

        /// <summary>
        /// Create a weak event handler from a System.Action.
        /// </summary>
        /// <param name="delegation">Delegation for creating EventHandler.</param>
        /// <param name="owner">The owner that control this weak event's lifecycle.</param>
        public WeakActionEventHandler(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> delegation, IEquatable<DBNull> owner)
        {
            m_Owner = new WeakReference<IEquatable<DBNull>>(owner);
            m_Method = delegation.Method;
            var invokerType = typeof(ActionInvoker<>).MakeGenericType(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), delegation.Target.GetType());
            m_Invocation = Activator.CreateInstance(invokerType, delegation.Target, delegation) as IInvocation ?? throw new InvalidOperationException($"Failed to create ActionInvoker.");
        }

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) notification) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13, notification.Item14, notification.Item15);

        /// <inheritdoc cref="IWeakEventHandler{TNotification}.TryHandle"/>
        public bool TryHandle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15) => m_Owner.TryGetTarget(out var owner) && owner != null && m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14, param15);

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) notification) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(notification.Item1, notification.Item2, notification.Item3, notification.Item4, notification.Item5, notification.Item6, notification.Item7, notification.Item8, notification.Item9, notification.Item10, notification.Item11, notification.Item12, notification.Item13, notification.Item14, notification.Item15); }

        /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
        public void Handle(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15) { if (m_Owner.TryGetTarget(out var owner) && owner != null) m_Invocation.Invoke(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14, param15); }

        /// <inheritdoc cref="System.Object.GetHashCode()"/>
        public override int GetHashCode() => m_Invocation != null! ? m_Invocation.GetHashCode() : 0;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public static bool operator ==(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> left, object right) => !Equals(left, null) && left.Equals(right);

        /// <summary>
        /// Verify if the target not equals to event handler.
        /// </summary>
        public static bool operator !=(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> left, object right) => !(left == right);

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public bool Equals(WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> other) => m_Method == other.m_Method;

        /// <summary>
        /// Verify if the target equals to event handler.
        /// </summary>
        public override bool Equals(object? obj) => obj switch
        {
            WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> other => Equals(other),
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action => action.Method == m_Method,
            _ => false
        };

        private interface IInvocation
        {
            bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15);
        }

        private sealed class ActionInvoker<TProvider> : IInvocation where TProvider : class
        {
            /// <summary>
            /// The event provider.
            /// </summary>
            private readonly WeakReference<TProvider> m_Provider;

            /// <summary>
            /// Wrapped action.
            /// </summary>
            private readonly Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> m_Invocation;

            public ActionInvoker(TProvider provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
            {
                m_Provider = new WeakReference<TProvider>(provider);
                m_Invocation = action.Method.CreateDelegate(typeof(Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>)) as Action<TProvider, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> ?? throw new InvalidOperationException($"Failed to create Action from method '{action.Method.Name}'.");
            }

            public bool Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15)
            {
                if (!m_Provider.TryGetTarget(out var provider))
                {
                    return false;
                }
                m_Invocation.Invoke(provider, param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14, param15);
                return true;
            }
        }
    }
