using Character;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    public GameObject abilityPrefab;
    public IGameUnit castFrom;

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
    }

    public void TerminateParticle()
    {
    }
}