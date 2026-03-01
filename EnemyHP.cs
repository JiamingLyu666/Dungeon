using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    public int hp = 20;

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}