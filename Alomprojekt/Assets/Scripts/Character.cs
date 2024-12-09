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
        [Header("Base Stats")]
        [SerializeField]
        protected float maxHealth = 5f;    // Karakter maximális életereje
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
        protected float minMovementSpeedValue = 1.0f;
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
        protected Rigidbody2D rigidbody2d;  // Karakterhez kapcsolódó Rigidbody2D komponens

        private Sprite currentSprite;
        // -> SPRITE RENDERER!!!

        [System.Serializable]
        public class LevelSpritePair
        {
            [Range(1, 4)]
            public int level;
            public Sprite sprite;
        }

        [Header("Sprites")]
        [SerializeField]
        private List<LevelSpritePair> LevelSpritePairs;

        private Dictionary<int, Sprite> levelSpriteDictionary;



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



        /// <summary>
        /// Események
        /// </summary>
        protected event Action OnDeath;    // Karakter halála
        //public event Action<float> OnChangeHealth;  // Karakter életerejének megváltozása


        /// <summary>
        /// Inicializálja a GameObject változóit és komponenseit, valamint feliratkozik a szükséges eseményekre.
        /// Ez a metódus akkor kerül meghívásra, amikor a szkript elõször betöltõdik vagy példányosul, jellemzõen a játék elején.
        /// </summary>
        protected virtual void Awake()
        {
            levelSpriteDictionary = SpriteListToDictionary(LevelSpritePairs);

            /*
            rigidbody2d = GetComponent<Rigidbody2D>();
            CurrentHealth = maxHealth + additionalHealth;
            CurrentMovementSpeed = baseMovementSpeed + additionalMovementSpeed;
            CurrentDMG = baseDMG + addtionalDMG;
            CurrentAttackCooldown = baseAttackCooldown + additionalAttackCooldown;
            CurrentCriticalHitChance = baseCriticalHitChance + additionalCriticalHitChance;
            CurrentPercentageBasedDMG = basePercentageBasedDMG + additionalPercentageBasedDMG;
            */
            // event subscriptions
            OnDeath += Die;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="spriteList"></param>
        /// <returns></returns>
        private Dictionary<int, Sprite> SpriteListToDictionary(List<LevelSpritePair> spriteList)
        {
            Dictionary<int, Sprite> levelSpriteDictionary = new Dictionary<int, Sprite>();
            foreach (var pair in spriteList)
            {
                if (!levelSpriteDictionary.ContainsKey(pair.level))
                {
                    levelSpriteDictionary.Add(pair.level, pair.sprite);
                }
            }

            return levelSpriteDictionary;
        }


        /// <summary>
        /// 
        /// </summary>
        private void OnValidate()
        {
            ValidateUniqueLevels();
        }


        /// <summary>
        /// 
        /// </summary>
        private void ValidateUniqueLevels()
        {
            HashSet<int> levelSet = new HashSet<int>();
            foreach (var pair in LevelSpritePairs)
            {
                if (!levelSet.Add(pair.level))
                {
                    Debug.LogError($"Duplicate level {pair.level} found in LevelSpritePairs.");
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        protected void SetCurrentSpriteByLevel(int level)
        {
            if (levelSpriteDictionary.ContainsKey(level))
            {
                currentSprite = levelSpriteDictionary[level];
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
