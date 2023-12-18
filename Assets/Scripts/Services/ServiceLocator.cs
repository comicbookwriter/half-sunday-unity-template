using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    public class ServiceLocator
    {
        private static Dictionary<Type, IService> ServiceCache = new Dictionary<Type, IService>();
        
        public static T GetService<T>() where T : class, IService, new()
        {
            if (typeof(T) == typeof(MonoBehaviour))
            {
                throw new ArgumentException(
                    "You shouldn't be attempting to create services from MonoBehaviours this way! Register them in Awake!");
            }
            bool serviceExists = ServiceCache.TryGetValue(typeof(T), out IService service);
            if (!serviceExists)
            {
                service = new T();
                ServiceCache.Add(typeof(T), service);
            }
            return (T) service;
        }

        public static bool TryGetService<T>(out T service) where T : class, IService
        {
            bool serviceExists = ServiceCache.TryGetValue(typeof(T), out IService iService);
            if (iService != null)
                service = (T) iService;
            else
                service = null;
            return serviceExists;
        }
        
        public static void RegisterService<T>(T serviceObj) where T: class, IService
        {
            ServiceCache.Add(typeof(T), serviceObj);
        }
    }
}