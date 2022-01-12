using Character;
using GameManagement;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    public GameObject abilityPrefab;
    public IGameUnit castFrom;

    protected Vector3 _targetPosition;
    protected float _animationProgress;
    protected bool _alive = false;


    public void Activate(Vector3 targetPosition, IGameUnit castFrom)
    {
        _alive = true;
        _targetPosition = targetPosition;
        this.castFrom = castFrom;
    }

    public void TerminateParticle()
    {
    }
}