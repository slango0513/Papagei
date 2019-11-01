//using Papagei;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using Microsoft.Extensions.ObjectPool;
//using System;

//namespace Papagei
//{
//    public class PooledEntityPolicy<T> : IPooledObjectPolicy<T> where T : Entity
//    {
//        private readonly IServiceProvider _serviceProvider;

//        public PooledEntityPolicy(IServiceProvider serviceProvider)
//        {
//            _serviceProvider = serviceProvider;
//        }

//        public T Create()
//        {
//            var obj = _serviceProvider.GetService<T>();
//            return obj;
//        }

//        public bool Return(T obj)
//        {
//            obj.Reset();
//            return true;
//        }
//    }
//}

//namespace Microsoft.Extensions.DependencyInjection
//{
//    public static class ServiceCollectionExtensions
//    {
//        public static IServiceCollection AddEntity<T>(this IServiceCollection services) where T : Entity
//        {
//            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
//            services.TryAddTransient<T>();
//            services.TryAddSingleton<IPooledObjectPolicy<T>>(provider =>
//            {
//                return new PooledEntityPolicy<T>(provider);
//            });
//            services.TryAddSingleton(provider =>
//            {
//                var objectPoolProvider = provider.GetRequiredService<ObjectPoolProvider>();
//                var policy = new PooledEntityPolicy<T>(provider);
//                return objectPoolProvider.Create(policy);
//            });
//            return services;
//        }

//        public static IServiceCollection AddEntity<T1, T2>(this IServiceCollection services) where T1 : Entity where T2 : Entity
//        {
//            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
//            services.AddEntity<T1>();
//            services.AddEntity<T2>();
//            return services;
//        }

//        public static IServiceCollection AddEntity<T1, T2, T3>(this IServiceCollection services) where T1 : Entity where T2 : Entity where T3 : Entity
//        {
//            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
//            services.AddEntity<T1, T2>();
//            services.AddEntity<T3>();
//            return services;
//        }
//    }
//}
