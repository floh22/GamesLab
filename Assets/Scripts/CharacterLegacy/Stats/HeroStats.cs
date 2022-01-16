using Character.MainHero;
using Character.StatsUI;

namespace Character.Stats
{
    public class HeroStats : IStats
    {

        public string name;
        public float health;
        public float damage;
        public float attackSpeed;
        public float rotationSpeed;
        public float attackRange;
        public float movementSpeed;
        private MainHeroHealthBar _healthBar;

        void Start()
        {
            _healthBar = GetComponentInChildren<MainHeroHealthBar>();
            _healthBar.SetName(name);
            _healthBar.SetHP(health);
        }
        
        public void OnAttacked(float dmg)
        {
            health -= dmg;
            _healthBar.SetHP(health);
        }

        void Update()
        {
        
        }
    }
}
