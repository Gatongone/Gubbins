using Gubbins.Context;

namespace Gubbins.Event;

public interface IEventListener
{
    void Listen(IDependenciesResolver resolver, IDependenciesRegistry registry);
    void Clear(IDependenciesResolver resolver);
}
