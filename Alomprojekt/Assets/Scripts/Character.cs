using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts
{
    // TODO: Mozgás és mozgási sebesség kezelése
    // TODO: változók/metódusok hozzáférési szintjének felülvizsgálata teszteléseket követõen
    // TODO: Player és Enemy támadás összefésülése
    public abstract class Character : MonoBehaviour
    {
        /// <summary>
        /// Változók
        /// </summary>
        protected float maxHealth = 5f;    // Karakter maximális életereje
        protected float additionalHealth = 0f; // Karakter hozzáadott életereje
        protected float _currentHealth;   // Karakter aktuális életereje

        protected float baseMovementSpeed = 3.0f;  // Karakter alap mozgási sebessége
        protected float additionalMovementSpeed = 0f;  // Karakter hozáadott mozgási sebessége
        protected float _currentMovementSpeed;  // Karakter aktuális mozgási sebessége

        protected float baseDMG = 1f;  // Karakter alap sebzésértéke
        protected float addtionalDMG = 0f; // Karakter hozzáadott sebzésértéke
        protected float _currentDMG;    // Karakter aktuális sebzésértéke

        protected float baseAttackCooldown = 0.5f; // Karakter alap támadás-visszatöltõdési ideje
        protected float additionalAttackCooldown = 0f;  // Karakter hozzáadott támadás-visszatöltõdési ideje
        protected float _currentAttackCooldown; // Karakter aktuális támadás-visszatöltõdési ideje


        /// <summary>
        /// Komponenesek
        /// </summary>
        protected Rigidbody2D rigidbody2d;  // Karakterhez kapcsolódó Rigidbody2D komponens


        /// <summary>
        /// Getterek és Setterek
        /// </summary>
        public float CurrentHealth  // Aktuális életerõ
        {
            get { return _currentHealth; }
            set { _currentHealth = value; }
        }

        public float MaxHealth  // Maximális életerõ
        {
            get { return maxHealth; }
            set { maxHealth = value; }
        }

        public float CurrentMovementSpeed   // Aktuális mozgási sebesség
        {
            get { return _currentMovementSpeed; }
            set { _currentMovementSpeed = value; }
        }

        public float CurrentDMG // Aktuális sebzésérték
        {
            get { return _currentDMG; }
            set { _currentDMG = value; }
        }

        public float BaseDMG    // Alap sebzésérték
        {
            get { return baseDMG; }
            set { baseDMG = value; }
        }

        public float CurrentAttackCooldown  // Aktuális sezés-visszatöltõdési idõ
        {
            get { return _currentAttackCooldown; }
            set { _currentAttackCooldown = value; }
        }


        /// <summary>
        /// Események
        /// </summary>
        public event Action OnDeath;    // Karakter halála
        //public event Action<float> OnChangeHealth;  // Karakter életerejének megváltozása


        /// <summary>
        /// Inicializálja a GameObject változóit és komponenseit, valamint feliratkozik a szükséges eseményekre.
        /// Ez a metódus akkor kerül meghívásra, amikor a szkript elõször betöltõdik vagy példányosul, jellemzõen a játék elején.
        /// </summary>
        void Awake()
        {
            rigidbody2d = GetComponent<Rigidbody2D>();
            CurrentHealth = maxHealth + additionalHealth;
            CurrentMovementSpeed = baseMovementSpeed + additionalMovementSpeed;
            CurrentDMG = baseDMG + addtionalDMG;
            CurrentAttackCooldown = baseAttackCooldown + additionalAttackCooldown;

            // esemény feliratkozások
            OnDeath += Die;
        }


        /// <summary>
        /// Kezeli a karakter életerõ-változását, figyeli a kapcsolódó eseményeket,
        /// és ellenõrzi, hogy a karakter meghalt-e.
        /// </summary>
        /// <param name="amount">Az életerõ változása; lehet pozitív (gyógyulás), vagy negatív (sebzés)</param>
        public virtual void ChangeHealth(float amount)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
            //OnChangeHealth?.Invoke(CurrentHealth);

            Debug.Log(CurrentHealth + " / " + MaxHealth);
            if (CurrentHealth == 0f)
            {
                OnDeath?.Invoke();
            }
        }


        /// <summary>
        /// Kezeli a karakter halálát, és törli a GameObject-et.
        /// </summary>
        protected virtual void Die()
        {
            Debug.Log("Entity " + gameObject.name + " has died");
            Destroy(gameObject);
        }


        /// <summary>
        /// A karakterhez kapcsolódó GameObject törlését megelõzõen leiratkozás a kapcsolódó
        /// OnDeath eseményrõl, hogy elkerüljük az esetleges memória szivárgást vagy hibákat.
        /// </summary>
        protected virtual void OnDestroy()
        {
            OnDeath -= Die;
        }
    }
}
