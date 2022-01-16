using System;
using System.Collections.Generic;
using GameManagement;
using Photon.Pun;
using UnityEngine;

namespace Character
{
    public enum GameUnitType
    {
        None,
        Structure,
        Player,
        Minion,
        Monster
    }
    public interface IGameUnit : IPunObservable
    {
        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        
        public GameUnitType Type { get; }
        
        public Vector3 Position { get; set; }
        public GameObject AttachtedObjectInstance { get; set; }
    
        public float MaxHealth { get; set; }
        public float Health { get; set; }
        public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; }
    
        public float AttackDamage { get; set; }
        public float AttackSpeed { get; set; }
    
        public float AttackRange { get; set; }

        [field: SerializeField] public static int DistanceForExperienceOnDeath = 5;
        
        public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }

        public void AddAttacker(IGameUnit attacker)
        {
            CurrentlyAttackedBy.Add(attacker);
        }

        public void RemoveAttacker(IGameUnit attacker)
        {
            CurrentlyAttackedBy.Remove(attacker);
        }
        
        public void TargetDied(IGameUnit target)
        {
            RemoveAttacker(target);
            if (target == CurrentAttackTarget)
            {
                CurrentAttackTarget = null;
            }
        }
        
        public void Damage(IGameUnit unit, float damage);

        public bool IsDestroyed();
    }
}
