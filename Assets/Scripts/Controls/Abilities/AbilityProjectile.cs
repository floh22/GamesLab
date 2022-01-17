using Character;
using GameManagement;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    public GameObject abilityPrefab;
    public IGameUnit castFrom;

    public GameObject DamageObjectHolder;
    public bool dealsDamage;

    protected Vector3 _targetPosition;
    protected float _animationProgress;
    protected bool _alive = false;
    protected float _damageMultiplier;


    public void Activate(Vector3 targetPosition, IGameUnit castFrom, float damageMultiplier)
    {
        _alive = true;
        _targetPosition = targetPosition;
        this.castFrom = castFrom;
        _damageMultiplier = damageMultiplier;
        DamageObjectHolder.AddComponent<DamageObject>();
        dealsDamage = true;
    }

    public void ActivateNoDamage(Vector3 targetPosition, IGameUnit castFrom)
    {
        _alive = true;
        _targetPosition = targetPosition;
        this.castFrom = castFrom;
    }

    public void TerminateParticle()
    {
    }
}