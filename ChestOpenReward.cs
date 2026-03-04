using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ChestOpenReward : MonoBehaviour
{
    [Header("Lid")]
    public Transform lid;
    public Vector3 openEulerOffset = new Vector3(-35f, 0f, 0f);
    public float openDuration = 0.4f;

    [Header("Interaction")]
    public float frontDotThreshold = 0.6f;
    public bool invertFrontDirection = true;

    [Header("Rewards")]
    public int healAmount = 20;
    public int maxHpAmount = 20;
    public int attackAmount = 5;
    public int defenseAmount = 3;

    private bool opened = false;
    private bool playerInRange = false;
    private PlayerStats currentPlayerStats;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    void Start()
    {
        if (lid == null)
        {
            Debug.LogError("Chest lid is not assigned.");
            enabled = false;
            return;
        }

        closedRotation = lid.localRotation;
        openedRotation = closedRotation * Quaternion.Euler(openEulerOffset);
    }

    void Update()
    {
        if (opened) return;
        if (!playerInRange) return;
        if (currentPlayerStats == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E key detected near chest.");

            if (!IsPlayerInFront())
            {
                Debug.Log("Player is not in front of the chest.");
                return;
            }

            opened = true;
            StartCoroutine(OpenChest(currentPlayerStats));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null) return;

        playerInRange = true;
        currentPlayerStats = stats;

        Debug.Log("Player entered chest trigger.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null) return;

        if (currentPlayerStats == stats)
        {
            playerInRange = false;
            currentPlayerStats = null;
            Debug.Log("Player exited chest trigger.");
        }
    }

    bool IsPlayerInFront()
    {
        if (currentPlayerStats == null) return false;

        Vector3 toPlayer = currentPlayerStats.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f) return false;
        toPlayer.Normalize();

        Vector3 chestForward = transform.forward;
        chestForward.y = 0f;
        chestForward.Normalize();

        if (invertFrontDirection)
        {
            chestForward = -chestForward;
        }

        float dot = Vector3.Dot(chestForward, toPlayer);
        Debug.Log("Chest front dot value: " + dot);

        return dot >= frontDotThreshold;
    }

    IEnumerator OpenChest(PlayerStats stats)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / openDuration;
            lid.localRotation = Quaternion.Slerp(closedRotation, openedRotation, t);
            yield return null;
        }

        lid.localRotation = openedRotation;
        GiveRandomReward(stats);

        Debug.Log("Chest opened and will remain open.");
    }

    void GiveRandomReward(PlayerStats stats)
    {
        int rewardType = Random.Range(0, 4);

        if (rewardType == 0)
        {
            stats.Heal(healAmount);
            Debug.Log("Chest reward: Heal +" + healAmount);
        }
        else if (rewardType == 1)
        {
            stats.IncreaseMaxHP(maxHpAmount);
            Debug.Log("Chest reward: Max HP +" + maxHpAmount);
        }
        else if (rewardType == 2)
        {
            stats.IncreaseAttack(attackAmount);
            Debug.Log("Chest reward: Attack +" + attackAmount);
        }
        else
        {
            stats.IncreaseDefense(defenseAmount);
            Debug.Log("Chest reward: Defense +" + defenseAmount);
        }
    }
}