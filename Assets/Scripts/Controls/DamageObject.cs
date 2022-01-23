using Character;
using NetworkedPlayer;
using Photon.Pun;
using UnityEngine;
using Minion = GameUnit.Minion;

public class DamageObject : MonoBehaviour
{
    [Range(1, 100)] public float damage = 10;
    public float delayBetweenAoEDamage = 1f;

    delegate void EffectOnDagmage(float damage);

    private EffectOnDagmage damageEffect;
    private IGameUnit _castFrom;
    private bool _isAoE = true;
    private float _damageMultiplier = 1;
    private float timestamp = 0f;


    public void Activate(IGameUnit castFrom, float damage, float delayBetweenAoEDamage, bool isAoE, float damageMultiplier)
    {
        this.damage = damage;
        this.delayBetweenAoEDamage = delayBetweenAoEDamage;
        _castFrom = castFrom;
        _isAoE = isAoE;
        _damageMultiplier = damageMultiplier;
    }

    private void OnTriggerStay(Collider collider)
    {
        if (!_isAoE )
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
        if (_isAoE )
        {
            return;
        }

        DealDamage(collider);
        gameObject.GetComponent<AbilityProjectile>().TerminateParticle();
    }

    private void DealDamage(Collider collider)
    {
        if (collider == null || _castFrom == null )
        {
            return;
        }
        
        IGameUnit unit = collider.GetComponent<IGameUnit>();

        //Ignore units without GameUnit component 
        if (unit == null || unit.Equals(null) || _castFrom.Team == unit.Team)
        {
            return;
        }

        float actualDamage = damage * _damageMultiplier;
        IGameUnit.SendDealDamageEvent(_castFrom, unit, actualDamage);
    }
}