using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class AbilityProjectile1 : MonoBehaviour
{
    private Vector3 _targetPosition;
    private float _animationProgress;
    private bool _alive = false;
    public GameObject ability1Prefab;

    public void Activate(Vector3 targetPosition)
    {
        _alive = true;
        _targetPosition = targetPosition;
    }

    void FixedUpdate()
    {
        if (_alive)
        {
            _animationProgress += Time.deltaTime;
            Debug.Log(_animationProgress);
            transform.position =
                MathParabola.Parabola(transform.position, _targetPosition, 0.5f, _animationProgress);
            if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
            {
                ability1Prefab.SetActive(true);
                Instantiate(ability1Prefab, _targetPosition, Quaternion.identity);
                Destroy((gameObject));
            }
        }
    }
}