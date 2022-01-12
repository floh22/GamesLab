using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using GameManagement;
using GameUnit;
using NetworkedPlayer;
using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Range(1, 100)] public float damage = 10;
    public float damageMultiplier = 1;
    public float delayBetweenAoEDamage = 1f;

    delegate void EffectOnDagmage(float damage);

    private EffectOnDagmage damageEffect;
    private IGameUnit _castFrom;
    private bool _isAoE = true;
    private float timestamp = 0f;


    public void Activate(IGameUnit castFrom, bool isAoE)
    {
        _castFrom = castFrom;
        _isAoE = isAoE;
    }

    private void OnTriggerStay(Collider collider)
    {
        if (!_isAoE)
        {
            return;
        }

        if (Time.time > timestamp + delayBetweenAoEDamage)
        {
            timestamp = Time.time;
            DealDamage(collider);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (_isAoE)
        {
            return;
        }

        DealDamage(collider);
        gameObject.GetComponent<AbilityProjectile>().TerminateParticle();
    }

    private void DealDamage(Collider collider)
    {
        if (collider == null)
        {
            return;
        }

        if (collider.gameObject.tag == "Minion")
        {
            collider.gameObject.GetComponent<Minion>().Damage(_castFrom, damage * damageMultiplier);
        }
        
        if (collider.gameObject.tag == "Player")
        {
            if (_castFrom == null)
            {
                return;
            }
            if (_castFrom.Team != collider.gameObject.GetComponent<PlayerController>().Team)
            {
                collider.gameObject.GetComponent<PlayerController>().Damage(_castFrom, damage * damageMultiplier);
            }
        }
    }
}