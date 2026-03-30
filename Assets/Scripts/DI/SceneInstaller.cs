using UnityEngine;

namespace DI
{
    public class SceneInstaller : Installer
    {
        protected sealed override void InstallBindings()
        {
            InstallSceneBindings();
        }
        
        protected virtual void InstallSceneBindings()
        {
            // Override this in your scene installer
        }
    }
}
