using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageIndicator : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float lifetime = 0.8f;
    public float minDist = 2.5f;
    public float maxDist = 3.5f;

    private Vector3 initPos;
    private Vector3 targetPos;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        var position = transform.position;
        transform.LookAt(position - Camera.main.transform.position);

        float direction = Random.rotation.eulerAngles.z;
        initPos = position;
        initPos.y += 1;
        float dist = Random.Range(minDist, maxDist);
        targetPos = initPos + (Quaternion.Euler(0, 0, direction) * new Vector3(dist, dist, 0f));
        targetPos.y = Mathf.Abs(targetPos.y);
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        float fraction = lifetime / 2f;

        if (timer > lifetime) Destroy(gameObject);
        else if (timer > fraction)
            text.color = Color.Lerp(text.color, Color.clear, (timer - fraction / (lifetime - fraction)));

        transform.position = Vector3.Lerp(initPos, targetPos, Mathf.Sin(timer / lifetime));
        transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, Mathf.Sin(timer / lifetime));
        transform.LookAt(transform.position - Camera.main.transform.position);
    }

    public void SetDamageText(float damage)
    {
        text.text = damage.ToString();
    }
}