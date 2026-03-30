using UnityEngine;
using UnityEngine.SceneManagement;

namespace DI
{
    public abstract class Installer : MonoBehaviour
    {
        protected DIContainer Container { get; private set; }
        
        private void Awake()
        {
            Container = DIContainer.CreateSceneContainer();
            InstallBindings();
            InjectSceneObjects();
        }
        
        private void OnDestroy()
        {
            if (Container != null)
            {
                DIContainer.RemoveContainer(Container);
            }
        }
        
        protected abstract void InstallBindings();
        
        private void InjectSceneObjects()
        {
            var scene = gameObject.scene;
            var rootObjects = scene.GetRootGameObjects();
            
            foreach (var root in rootObjects)
            {
                var components = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var component in components)
                {
                    if (component != null && !(component is Installer))
                    {
                        DIContainer.Inject(component);
                    }
                }
            }
        }
        
        protected void Bind<TInterface, TImplementation>(string key = "") where TImplementation : TInterface
        {
            Container.Register<TInterface, TImplementation>(key);
        }
        
        protected void BindInstance<TInterface>(TInterface instance, string key = "")
        {
            Container.RegisterInstance(instance, key);
        }
        
        protected void BindType<TImplementation>(string key = "")
        {
            Container.RegisterType<TImplementation>(key);
        }
        
        protected T Resolve<T>(string key = "")
        {
            return DIContainer.Resolve<T>(key);
        }
    }
}
