using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.EnemyBehaviours
{
    // Az ellenség passzív viselkedését meghatározó absztrakt osztály, amely az 'EnemyBehaviour' osztálytól öröklődik.
    // A passzív viselkedés részleteit a leszármazott osztályok határozzák meg.
    public abstract class PassiveEnemyBehaviour : EnemyBehaviour
    {
        // Az 'ExecuteBehaviour' metódus felülírva az 'EnemyBehaviour' osztályból,
        // de itt absztrakt módon van deklarálva, tehát az öröklődő osztálynak kell megvalósítania a viselkedés végrehajtását.
        public override abstract void ExecuteBehaviour(EnemyController enemyController);

        // Az 'StartBehaviour' metódus felülírva az 'EnemyBehaviour' osztályból,
        // de itt absztrakt módon van deklarálva, tehát az öröklődő osztálynak kell megvalósítania a viselkedés indítását.
        public override abstract void StartBehaviour(EnemyController enemyController);

        // Az 'StopBehaviour' metódus felülírva az 'EnemyBehaviour' osztályból,
        // de itt absztrakt módon van deklarálva, tehát az öröklődő osztálynak kell megvalósítania a viselkedés leállítását.
        public override abstract void StopBehaviour(EnemyController enemyController);
    }
}
