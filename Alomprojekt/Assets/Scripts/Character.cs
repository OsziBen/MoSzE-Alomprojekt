using System;
using System.Collections;
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
        [Header("Prefab ID")]
        [SerializeField]
        protected string prefabID;

        [ContextMenu("Generate guid for ID")]
        private void GenerateGuid()
        {
            prefabID = System.Guid.NewGuid().ToString();
        }


        [Header("Base Stats")]
        [SerializeField]
        protected float maxHealth;    // Karakter maximális életereje
        protected float additionalHealth = 0f; // Karakter hozzáadott életereje
        protected float _currentHealth;   // Karakter aktuális életereje
        [SerializeField]
        protected float baseMovementSpeed = 3.0f;  // Karakter alap mozgási sebessége
        protected float additionalMovementSpeed = 0f;  // Karakter hozáadott mozgási sebessége
        protected float _currentMovementSpeed;  // Karakter aktuális mozgási sebessége
        [SerializeField]
        protected float baseDMG = 1f;  // Karakter alap sebzésértéke
        protected float additionalDMG = 0f; // Karakter hozzáadott sebzésértéke
        protected float _currentDMG;    // Karakter aktuális sebzésértéke
        [SerializeField]
        protected float baseAttackCooldown = 0.5f; // Karakter alap támadás-visszatöltõdési ideje
        protected float attackCooldownReduction = 0f;  // Karakter hozzáadott támadás-visszatöltõdési ideje
        protected float _currentAttackCooldown; // Karakter aktuális támadás-visszatöltõdési ideje
        [SerializeField]
        protected float baseCriticalHitChance = 0.05f;  // Karakter alap kritikussebzés esélye (%)
        protected float additionalCriticalHitChance = 0.2f; // Karakter hozzáadott kritikussebzés esélye (%)
        protected float _currentCriticalHitChance;  // Karakter aktuális kritikussebzés esélye (%)
        [SerializeField]
        protected float basePercentageBasedDMG = 0f;    // Karakter alap százalékos sebzésértéke (%)
        protected float additionalPercentageBasedDMG = 0.07f;   // Karakter hozzáadott százalékos sebzésértéke (%)
        protected float _currentPercentageBasedDMG; // Karakter aktuális százalékos sebzésértéke (%)

        [Header("Max Values (temp)")]
        [SerializeField]
        protected float minHealthValue = 5f;
        [SerializeField]
        protected float maxHealthValue = 10f;
        [SerializeField]
        protected float minMovementSpeedValue = 3.0f;
        [SerializeField]
        protected float maxMovementSpeedValue = 10.0f;
        [SerializeField]
        protected float minDMGValue = 1f;
        [SerializeField]
        protected float maxDMGValue = 10f;
        [SerializeField]
        protected float minAttackCooldownValue = 0.5f;
        [SerializeField]
        protected float maxAttackCooldownValue = 5.0f;

        protected float minCriticalHitChanceValue = 0.0f;
        protected float maxCriticalHitChanceValue = 0.5f; // Kritikus találat esélye százalékban (0% - 100%)

        protected float minPercentageBasedDMGValue = 0.0f;
        protected float maxPercentageBasedDMGValue = 0.5f;

        /// <summary>
        /// Komponenesek
        /// </summary>
        [SerializeField]
        protected Rigidbody2D _rigidbody2d;  // Karakterhez kapcsolódó Rigidbody2D komponens


        /// <summary>
        /// Getterek és Setterek
        /// </summary>
        /// 
        public string ID
        {
            get { return prefabID; }
        }

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

        public float CurrentCriticalHitChance   // Aktuális kritikussebzés esély (5)
        {
            get { return _currentCriticalHitChance; }
            set { _currentCriticalHitChance = value; }
        }

        public float CurrentPercentageBasedDMG  // Aktuális százalékos sebzésérték (%)
        {
            get { return _currentPercentageBasedDMG; }
            set { _currentPercentageBasedDMG = value; }
        }

        public Rigidbody2D rigidbody2d
        {
            get { return _rigidbody2d; }
            protected set { _rigidbody2d = value; }
        }


        /// <summary>
        /// Események
        /// </summary>
        protected event Action OnDeath;    // Karakter halála
        public event Action<float> OnHealthChanged;  // Karakter életerejének megváltozása


        /// <summary>
        /// Inicializálja a GameObject változóit és komponenseit, valamint feliratkozik a szükséges eseményekre.
        /// Ez a metódus akkor kerül meghívásra, amikor a szkript elõször betöltõdik vagy példányosul, jellemzõen a játék elején.
        /// </summary>
        protected virtual void Awake()
        {
            //levelSpriteDictionary = SpriteListToDictionary(LevelSpritePairs);

            /*
            CurrentHealth = maxHealth + additionalHealth;
            CurrentMovementSpeed = baseMovementSpeed + additionalMovementSpeed;
            CurrentDMG = baseDMG + addtionalDMG;
            CurrentAttackCooldown = baseAttackCooldown + additionalAttackCooldown;
            CurrentCriticalHitChance = baseCriticalHitChance + additionalCriticalHitChance;
            CurrentPercentageBasedDMG = basePercentageBasedDMG + additionalPercentageBasedDMG;
            */
            rigidbody2d = GetComponent<Rigidbody2D>();
            // event subscriptions
            OnDeath += Die;
        }


        /// <summary>
        /// 
        /// </summary>
        private void OnValidate()
        {
            //ValidateUniqueID();
        }


        private void ValidateUniqueID()
        {
            if (string.IsNullOrEmpty(prefabID))
            {
                Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
            }
        }


        /// <summary>
        /// Kezeli a karakter életerõ-változását, figyeli a kapcsolódó eseményeket,
        /// és ellenõrzi, hogy a karakter meghalt-e.
        /// </summary>
        /// <param name="amount">Az életerõ változása; lehet pozitív (gyógyulás), vagy negatív (sebzés)</param>
        public virtual void ChangeHealth(float amount)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);

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
