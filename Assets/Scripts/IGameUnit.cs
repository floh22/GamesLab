using System.Collections;
using System.Collections.Generic;
using GameManagement;
using UnityEngine;

public interface IGameUnit
{
    public int NetworkID { get; set; }
    public GameData.Team Team { get; set; }
    public float Health { get; set; }
    public float MoveSpeed { get; set; }
    
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    
}
