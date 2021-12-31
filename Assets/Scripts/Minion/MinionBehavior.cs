using GameManagement;
using UnityEngine;

namespace Minion
{
    public class MinionBehavior : MonoBehaviour, IGameUnit
    {
        [SerializeField] public static GameObject TargetPositions;
        
        [SerializeField] private static MinionValues values;
        
        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        public float Health { get; set; }
        public float MoveSpeed { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackDamage { get; set; }
        
        private GameData.Team target;
        
        private Transform currentDestination;
        private GameObject currentTarget;


        private float health;
        private float attackCooldown;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        public void Init(int networkID, GameData.Team targetTeam)
        {
            this.NetworkID = networkID;
            this.target = targetTeam;
            Health = values.MinionHealth;
            MoveSpeed = values.MinionMoveSpeed;
            AttackSpeed = values.MinionAttacKSpeed;
            AttackDamage = values.MinionAttackDamage;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void SetTarget(GameData.Team targetTeam)
        {
            this.target = targetTeam;
        }
    }
}
