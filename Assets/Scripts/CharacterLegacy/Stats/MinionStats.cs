using Character.StatsUI;

namespace Character.Stats
{
    public class MinionStats : IStats
    {

        public string name;
        public float health;
        public float damage;
        public float attackSpeed;
        public float rotationSpeed;
        public float attackRange;
        private MinionHealthBar _healthBar;

        // Start is called before the first frame update
        void Start()
        {
            _healthBar = GetComponentInChildren<MinionHealthBar>();
            _healthBar.SetName(name);
            _healthBar.SetHP(health);
        }

        public void OnAttacked(float dmg)
        {
            health -= dmg;
            _healthBar.SetHP(health);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
