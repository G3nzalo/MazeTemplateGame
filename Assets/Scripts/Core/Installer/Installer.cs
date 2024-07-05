using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Installers
{
    public abstract class Installer : MonoBehaviour
    {
        public abstract void Install(ServiceLocator serviceLocator);
    }
}
