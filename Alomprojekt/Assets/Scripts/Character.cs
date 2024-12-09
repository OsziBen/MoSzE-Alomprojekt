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
    // TODO: Mozg�s �s mozg�si sebess�g kezel�se
    // TODO: v�ltoz�k/met�dusok hozz�f�r�si szintj�nek fel�lvizsg�lata tesztel�seket k�vet�en
    // TODO: Player �s Enemy t�mad�s �sszef�s�l�se
    public abstract class Character : MonoBehaviour
    {
        /// <summary>
        /// V�ltoz�k
        /// </summary>
        [Header("Base Stats")]
        [SerializeField]
        protected float maxHealth = 5f;    // Karakter maxim�lis �letereje
        protected float additionalHealth = 0f; // Karakter hozz�adott �letereje
        protected float _currentHealth;   // Karakter aktu�lis �letereje
        [SerializeField]
        protected float baseMovementSpeed = 3.0f;  // Karakter alap mozg�si sebess�ge
        protected float additionalMovementSpeed = 0f;  // Karakter hoz�adott mozg�si sebess�ge
        protected float _currentMovementSpeed;  // Karakter aktu�lis mozg�si sebess�ge
        [SerializeField]
        protected float baseDMG = 1f;  // Karakter alap sebz�s�rt�ke
        protected float additionalDMG = 0f; // Karakter hozz�adott sebz�s�rt�ke
        protected float _currentDMG;    // Karakter aktu�lis sebz�s�rt�ke
        [SerializeField]
        protected float baseAttackCooldown = 0.5f; // Karakter alap t�mad�s-visszat�lt�d�si ideje
        protected float attackCooldownReduction = 0f;  // Karakter hozz�adott t�mad�s-visszat�lt�d�si ideje
        protected float _currentAttackCooldown; // Karakter aktu�lis t�mad�s-visszat�lt�d�si ideje
        [SerializeField]
        protected float baseCriticalHitChance = 0.05f;  // Karakter alap kritikussebz�s es�lye (%)
        protected float additionalCriticalHitChance = 0.2f; // Karakter hozz�adott kritikussebz�s es�lye (%)
        protected float _currentCriticalHitChance;  // Karakter aktu�lis kritikussebz�s es�lye (%)
        [SerializeField]
        protected float basePercentageBasedDMG = 0f;    // Karakter alap sz�zal�kos sebz�s�rt�ke (%)
        protected float additionalPercentageBasedDMG = 0.07f;   // Karakter hozz�adott sz�zal�kos sebz�s�rt�ke (%)
        protected float _currentPercentageBasedDMG; // Karakter aktu�lis sz�zal�kos sebz�s�rt�ke (%)

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
        protected float maxCriticalHitChanceValue = 0.5f; // Kritikus tal�lat es�lye sz�zal�kban (0% - 100%)

        protected float minPercentageBasedDMGValue = 0.0f;
        protected float maxPercentageBasedDMGValue = 0.5f;

        /// <summary>
        /// Komponenesek
        /// </summary>
        protected Rigidbody2D rigidbody2d;  // Karakterhez kapcsol�d� Rigidbody2D komponens

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
        /// Getterek �s Setterek
        /// </summary>
        public float CurrentHealth  // Aktu�lis �leter�
        {
            get { return _currentHealth; }
            set { _currentHealth = value; }
        }

        public float MaxHealth  // Maxim�lis �leter�
        {
            get { return maxHealth; }
            set { maxHealth = value; }
        }

        public float CurrentMovementSpeed   // Aktu�lis mozg�si sebess�g
        {
            get { return _currentMovementSpeed; }
            set { _currentMovementSpeed = value; }
        }

        public float CurrentDMG // Aktu�lis sebz�s�rt�k
        {
            get { return _currentDMG; }
            set { _currentDMG = value; }
        }

        public float BaseDMG    // Alap sebz�s�rt�k
        {
            get { return baseDMG; }
            set { baseDMG = value; }
        }

        public float CurrentAttackCooldown  // Aktu�lis sez�s-visszat�lt�d�si id�
        {
            get { return _currentAttackCooldown; }
            set { _currentAttackCooldown = value; }
        }

        public float CurrentCriticalHitChance   // Aktu�lis kritikussebz�s es�ly (5)
        {
            get { return _currentCriticalHitChance; }
            set { _currentCriticalHitChance = value; }
        }

        public float CurrentPercentageBasedDMG  // Aktu�lis sz�zal�kos sebz�s�rt�k (%)
        {
            get { return _currentPercentageBasedDMG; }
            set { _currentPercentageBasedDMG = value; }
        }



        /// <summary>
        /// Esem�nyek
        /// </summary>
        protected event Action OnDeath;    // Karakter hal�la
        //public event Action<float> OnChangeHealth;  // Karakter �leterej�nek megv�ltoz�sa


        /// <summary>
        /// Inicializ�lja a GameObject v�ltoz�it �s komponenseit, valamint feliratkozik a sz�ks�ges esem�nyekre.
        /// Ez a met�dus akkor ker�l megh�v�sra, amikor a szkript el�sz�r bet�lt�dik vagy p�ld�nyosul, jellemz�en a j�t�k elej�n.
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
        /// Kezeli a karakter �leter�-v�ltoz�s�t, figyeli a kapcsol�d� esem�nyeket,
        /// �s ellen�rzi, hogy a karakter meghalt-e.
        /// </summary>
        /// <param name="amount">Az �leter� v�ltoz�sa; lehet pozit�v (gy�gyul�s), vagy negat�v (sebz�s)</param>
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
        /// Kezeli a karakter hal�l�t, �s t�rli a GameObject-et.
        /// </summary>
        protected virtual void Die()
        {
            Debug.Log("Entity " + gameObject.name + " has died");
            Destroy(gameObject);
        }


        /// <summary>
        /// A karakterhez kapcsol�d� GameObject t�rl�s�t megel�z�en leiratkoz�s a kapcsol�d�
        /// OnDeath esem�nyr�l, hogy elker�lj�k az esetleges mem�ria sziv�rg�st vagy hib�kat.
        /// </summary>
        protected virtual void OnDestroy()
        {
            OnDeath -= Die;
        }
    }
}
