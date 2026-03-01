using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 2f;
    public float chaseRange = 6f;
    public int touchDamage = 5;
    public float hitCooldown = 1f;

    private Transform player;
    private float cd = 0f;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        cd -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= chaseRange)
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            transform.position += dir.normalized * speed * Time.deltaTime;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (cd > 0f) return;

        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(touchDamage);
                cd = hitCooldown;
            }
        }
    }
}