using DI;
using SunnysideIsland.Audio;
using SunnysideIsland.Localization;
using SunnysideIsland.Pool;
using SunnysideIsland.Core;

namespace DI
{
    public class GameGlobalInstaller : GlobalInstaller
    {
        protected override void InstallGlobalBindings()
        {
            BindType<UpdateManager>();
        }
    }
}