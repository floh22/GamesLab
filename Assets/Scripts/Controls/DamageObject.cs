using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Range(1, 100)] public float damage = 10;
    public float damageMultiplier = 1;



    private void OnTriggerEnter(Collider collider)
    {
        // if (collider.gameObject.tag == "Player")
        // {
        //     collider.gameObject.GetComponent<IGameUnit>().Damage(null, damage * damageMultiplier);
        //     gameObject.GetComponent<AbilityProjectile>().TerminateParticle();
        // }
    }
}
