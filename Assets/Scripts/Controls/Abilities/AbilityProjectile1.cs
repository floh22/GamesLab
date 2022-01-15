using System.Collections;
using System.Collections.Generic;
using GameManagement;
using TreeEditor;
using UnityEngine;

public class AbilityProjectile1 : AbilityProjectile
{
    void FixedUpdate()
    {
        if (_alive)
        {
            _animationProgress += Time.deltaTime;
            transform.position =
                MathParabola.Parabola(transform.position, _targetPosition, 0.5f, _animationProgress);
            if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
            {
                TerminateParticle();
            }
        }
    }

    public void TerminateParticle()
    {
        abilityPrefab.SetActive(true);
        GameObject explosion = Instantiate(abilityPrefab, _targetPosition, Quaternion.identity) as GameObject;
        explosion.GetComponent<DamageObject>().Activate(castFrom, true, _damageMultiplier);
        Destroy(gameObject);
    }
}