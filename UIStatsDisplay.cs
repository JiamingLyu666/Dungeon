using UnityEngine;
using TMPro;

public class UIStatsDisplay : MonoBehaviour
{
    public PlayerStats playerStats;
    public string playerTag = "Player";

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;

    void Start()
    {
        FindPlayerStatsIfNeeded();
        RefreshUI();
    }

    void Update()
    {
        FindPlayerStatsIfNeeded();
        RefreshUI();
    }

    void FindPlayerStatsIfNeeded()
    {
        if (playerStats != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null) return;

        playerStats = playerObject.GetComponent<PlayerStats>();
    }

    void RefreshUI()
    {
        if (playerStats == null) return;

        if (hpText != null)
        {
            hpText.text = "HP " + playerStats.hp + "/" + playerStats.maxHP;
        }

        if (atkText != null)
        {
            atkText.text = "ATK " + playerStats.attack;
        }

        if (defText != null)
        {
            defText.text = "DEF " + playerStats.defense;
        }
    }
}