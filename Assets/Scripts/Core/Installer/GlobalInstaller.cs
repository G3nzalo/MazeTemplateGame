using System.Collections;
using System.Collections.Generic;
using Maze.Runtime.Commands;
using Core.DataStorage;
using Core.Serializers;
using Patterns.Behaviour.Command;
using UnityEngine;

namespace Core.Installers
{
    public class GlobalInstaller : GeneralInstaller
    {
        protected override void DoStart()
        {
            ServiceLocator.Instance.GetService<CommandQueue>()
                          .AddCommand(new LoadSceneCommand("Menu"));
        }

        protected override void DoInstallDependencies()
        {
            ServiceLocator.Instance.RegisterService(CommandQueue.Instance);

            var serializer = new JsonUtilityAdapter();
            var dataStore = new PlayerPrefsDataStoreAdapter(serializer);
            //var scoreSystemImpl = new ScoreSystemImpl(dataStore);
            //ServiceLocator.Instance.RegisterService<ScoreSystem>(scoreSystemImpl);
        }
    }
}
