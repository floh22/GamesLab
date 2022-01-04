using System;
using Character;
using GameManagement;
using UnityEngine;

namespace GameUnit
{
    public class BaseBehavior : MonoBehaviour, IGameUnit
    {
        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        public float MaxHealth { get; set; } = 1000;
        public float Health { get; set; }
        public float MoveSpeed { get; set; } = 0;
        public float RotationSpeed { get; set; }  = 0;
        public float AttackDamage { get; set; } = 0;
        public float AttackSpeed { get; set; } = 0;
        public float AttackRange { get; set; } = 0;
        
        public int Pages { get; set; }
        
        // Start is called before the first frame update
        void Start()
        {
            GameObject o = gameObject;
            NetworkID = o.GetInstanceID();
            bool res = Enum.TryParse(o.name, out GameData.Team parsedTeam);
            if (res)
            {
                Team = parsedTeam;
            }
            else
            {
                Debug.LogError($"Could not init base {o.name}");
            }

            Health = MaxHealth;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

    }
}
