using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int maxHP = 100;
    public int hp = 100;
    public int coins = 0;

    void Start()
    {
        hp = maxHP;
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0)
        {
            hp = maxHP;
            transform.position = Vector3.zero;
            Debug.Log("Player died, reset position");
        }
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHP, hp + amount);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }
}