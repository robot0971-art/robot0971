using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DI
{
    public abstract class GlobalInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAll()
        {
            if (DIContainer.Global == null)
            {
                DIContainer.InitializeGlobal();
            }
            
            // 모든 GlobalInstaller 구현체 찾아서 실행
            var installerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GlobalInstaller)));
            
            foreach (var type in installerTypes)
            {
                try
                {
                    var installer = (GlobalInstaller)Activator.CreateInstance(type);
                    installer.InstallGlobalBindings();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to initialize GlobalInstaller {type.Name}: {e}");
                }
            }
        }
        
        protected void Bind<TInterface, TImplementation>(string key = "") where TImplementation : TInterface
        {
            DIContainer.Global.Register<TInterface, TImplementation>(key);
        }
        
        protected void BindInstance<TInterface>(TInterface instance, string key = "")
        {
            DIContainer.Global.RegisterInstance(instance, key);
        }
        
        protected void BindType<TImplementation>(string key = "")
        {
            DIContainer.Global.RegisterType<TImplementation>(key);
        }
        
        protected T Resolve<T>(string key = "")
        {
            return DIContainer.Resolve<T>(key);
        }
        
        protected abstract void InstallGlobalBindings();
    }
}
