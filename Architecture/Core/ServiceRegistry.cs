using System.Collections.Concurrent;

namespace LibmpvIptvClient.Architecture.Core;

public sealed class ServiceRegistry
{
    private readonly ConcurrentDictionary<Type, Func<ServiceRegistry, object>> _factories = new();
    private readonly ConcurrentDictionary<Type, object> _singletons = new();
    private readonly ConcurrentDictionary<Type, byte> _singletonTypes = new();

    public void RegisterSingleton<TService>(TService instance) where TService : class
    {
        _singletons[typeof(TService)] = instance;
        _singletonTypes[typeof(TService)] = 1;
    }

    public void RegisterSingleton<TService>(Func<ServiceRegistry, TService> factory) where TService : class
    {
        _factories[typeof(TService)] = scope => factory(scope);
        _singletonTypes[typeof(TService)] = 1;
    }

    public void RegisterTransient<TService>(Func<ServiceRegistry, TService> factory) where TService : class
    {
        _factories[typeof(TService)] = scope => factory(scope);
        _singletons.TryRemove(typeof(TService), out _);
        _singletonTypes.TryRemove(typeof(TService), out _);
    }

    public TService Resolve<TService>() where TService : class
    {
        var service = Resolve(typeof(TService));
        return (TService)service;
    }

    public object Resolve(Type serviceType)
    {
        if (_singletons.TryGetValue(serviceType, out var existing))
        {
            return existing;
        }

        if (_factories.TryGetValue(serviceType, out var factory))
        {
            var created = factory(this);
            if (created is null)
            {
                throw new InvalidOperationException($"服务构造失败: {serviceType.FullName}");
            }

            if (!_singletonTypes.ContainsKey(serviceType))
            {
                return created;
            }

            _singletons[serviceType] = created;
            return created;
        }

        throw new InvalidOperationException($"未注册服务: {serviceType.FullName}");
    }
}
