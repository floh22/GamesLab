using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GameManagement;
using Network;
using Photon.Pun;
using Photon.Realtime;
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
        public int OwnerID { get; }
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

        [field: SerializeField] public static int DistanceForExperienceOnDeath = 10;
        
        public bool IsAlive { get; set; }
        
        public bool IsVisible { get; set; }
        
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
        
        public void DoDamageVisual(IGameUnit unit, float damage);


        public static void SendDealDamageEvent(IGameUnit source, IGameUnit target, float damage)
        {
            Debug.Log(source.NetworkID + ", " + target.NetworkID);
            object[] content = { source.NetworkID, target.NetworkID, damage }; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All}; 
            var res = PhotonNetwork.RaiseEvent(GameStateController.DamageGameUnitEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void SendDealDamageEvent(IGameUnit target, float damage)
        {
            SendDealDamageEvent(this, target, damage);
        }

        public bool IsDestroyed();
        
    }
}
