using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 100;
    public int hp = 100;
    public int attack = 10;
    public int defense = 0;

    void Start()
    {
        hp = maxHP;
        Debug.Log("PlayerStats initialized => HP: " + hp + "/" + maxHP + ", ATK: " + attack + ", DEF: " + defense);
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHP, hp + amount);
        Debug.Log("Heal applied => HP: " + hp + "/" + maxHP);
    }

    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        hp += amount;
        Debug.Log("Max HP increased => HP: " + hp + "/" + maxHP);
    }

    public void IncreaseAttack(int amount)
    {
        attack += amount;
        Debug.Log("Attack increased => ATK: " + attack);
    }

    public void IncreaseDefense(int amount)
    {
        defense += amount;
        Debug.Log("Defense increased => DEF: " + defense);
    }

    public void TakeDamage(int enemyAttack)
    {
        int damage = Mathf.Max(0, enemyAttack - defense);
        hp -= damage;

        if (hp <= 0)
        {
            hp = maxHP;
            transform.position = Vector3.zero;
        }

        Debug.Log("Damage taken => HP: " + hp + "/" + maxHP + ", ATK: " + attack + ", DEF: " + defense);
    }
}