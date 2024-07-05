using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Installers
{
    public abstract class GeneralInstaller : MonoBehaviour
    {
        [SerializeField] private Installer[] _installers;

        private void Awake()
        {
            InstallDependencies();
        }

        private void Start()
        {
            DoStart();
        }

        protected abstract void DoStart();

        private void InstallDependencies()
        {
            foreach (var installer in _installers)
            {
                installer.Install(ServiceLocator.Instance);
            }
            DoInstallDependencies();
        }

        protected abstract void DoInstallDependencies();
    }
}