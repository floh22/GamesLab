using Character;
using GameManagement;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    public GameObject abilityPrefab;
    [Range(1, 100)] public float damage = 10;
    public float damageMultiplier = 1;

    protected Vector3 _targetPosition;
    protected GameData.Team? _castFrom = null;
    protected float _animationProgress;
    protected bool _alive = false;
    

    public void Activate(Vector3 targetPosition, System.Nullable<GameManagement.GameData.Team> castFrom)
    {
        _alive = true;
        _targetPosition = targetPosition;
        _castFrom = castFrom;
    }
    
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            collider.gameObject.GetComponent<IGameUnit>().Damage(null, damage * damageMultiplier);
        }
    }

    public void TerminateParticle()
    {
    }
}