namespace Gubbins.Context;

public interface IEventListener
{
    void Listen(IDependenciesResolver resolver, IDependenciesRegistry registry);
    void Clear(IDependenciesResolver resolver);
}