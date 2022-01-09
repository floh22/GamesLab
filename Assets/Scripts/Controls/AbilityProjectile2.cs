using UnityEngine;

public class AbilityProjectile2 : MonoBehaviour
{
    public Vector3 _targetPosition;
    private float _animationProgress;
    public bool _alive = false;
    public GameObject ability1Prefab;

    public void Activate(Vector3 targetPosition)
    {
        _alive = true;
        _targetPosition = targetPosition;
        transform.LookAt(targetPosition);
    }

    void FixedUpdate()
    {
        if (_alive)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, Time.deltaTime * 6);
            if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
            {
                // Instantiate(ability1Prefab, _targetPosition, Quaternion.identity);
                Destroy((gameObject));
            }
        }
    }
}