using Assets.Scripts;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.EnemyBehaviours
{
    // Az ellenség viselkedését meghatározó absztrakt osztály.
    public abstract class EnemyBehaviour : MonoBehaviour, IEnemyBehaviour
    {
        // Az absztrakt metódus, amely meghatározza az ellenség viselkedésének végrehajtását.
        // Az ellenség vezérlőjét kapja paraméterül.
        public abstract void ExecuteBehaviour(EnemyController enemyController);

        // Az absztrakt metódus, amely az ellenség viselkedésének elindítását végzi.
        // Az ellenség vezérlőjét kapja paraméterül.
        public abstract void StartBehaviour(EnemyController enemyController);

        // Az absztrakt metódus, amely az ellenség viselkedésének leállítását végzi.
        // Az ellenség vezérlőjét kapja paraméterül.
        public abstract void StopBehaviour(EnemyController enemyController);
    }
}
