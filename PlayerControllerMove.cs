using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerControllerMove : MonoBehaviour
{
    [Header("Character Controller")]
    public float controllerCenterY = 0.7461587f;
    public float controllerRadius = 0.5f;
    public float controllerHeight = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public Transform cameraTransform;

    [Header("Animation")]
    public Animator animator;
    public string idleStateName = "WK_heavy_infantry_05_combat_idle";
    public string runStateName = "WK_heavy_infantry_04_charge";
    public string attackStateName = "WK_heavy_infantry_08_attack_B";

    [Header("Combat")]
    public string enemyTag = "Enemy";
    public float attackRange = 2f;
    public bool lockMovementDuringAttack = true;

    [Header("Gate")]
    public string gateObjectName = "Arch_Gate";
    public bool removeGateWhenNoEnemyAlive = true;

    [Header("Victory")]
    public string victoryTriggerName = "VictoryTrigger";
    public GameObject victoryPanel;
    public bool autoCreateVictoryPanelIfMissing = true;
    public bool pauseOnVictory = true;

    private readonly string[] victoryKeywords = { "victory", "win" };

    private CharacterController characterController;
    private bool isAttacking;
    private float attackEndTime;
    private float cachedAttackDuration = -1f;
    private int currentAnimationHash = int.MinValue;
    private bool hasSeenEnemy;
    private bool gateRemoved;
    private float enemyCheckTimer;
    private bool hasWon;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.LogWarning("CharacterController was missing and has been added at runtime.");
        }

        ApplyCharacterControllerProfile();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        PlayerAnimDriver oldAnimDriver = GetComponent<PlayerAnimDriver>();
        if (oldAnimDriver != null)
        {
            oldAnimDriver.enabled = false;
        }

        if (victoryPanel == null)
        {
            victoryPanel = FindVictoryPanelInScene();
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        EnsureStatePlaying(idleStateName, true);
        CheckEnemyAndGateState();
    }

    void Update()
    {
        if (hasWon) return;
        if (Keyboard.current == null) return;

        bool shiftHeld =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isAttacking)
        {
            StartAttack();
        }

        Vector3 moveInput = GetMoveInput();
        bool canMove = !shiftHeld && (!lockMovementDuringAttack || !isAttacking);
        Vector3 moveWorld = canMove ? ConvertInputToWorld(moveInput) : Vector3.zero;

        MoveCharacter(moveWorld);
        UpdateAnimation(moveWorld);

        enemyCheckTimer -= Time.deltaTime;
        if (enemyCheckTimer <= 0f)
        {
            enemyCheckTimer = 0.25f;
            CheckEnemyAndGateState();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasWon) return;
        if (other == null) return;

        if (!IsVictoryTrigger(other.transform)) return;

        ShowVictoryUI();
    }

    private void ApplyCharacterControllerProfile()
    {
        if (characterController == null) return;

        characterController.radius = controllerRadius;
        characterController.height = controllerHeight;
        characterController.center = new Vector3(0f, controllerCenterY, 0f);

        characterController.stepOffset = Mathf.Clamp(0.3f, 0f, Mathf.Max(0f, controllerHeight - 0.01f));
        characterController.slopeLimit = 45f;
        characterController.skinWidth = 0.08f;
        characterController.minMoveDistance = 0.001f;

        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            capsuleCollider.radius = controllerRadius;
            capsuleCollider.height = controllerHeight;
            capsuleCollider.center = new Vector3(0f, controllerCenterY, 0f);
            capsuleCollider.direction = 1;
        }
    }

    private Vector3 GetMoveInput()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;

        return new Vector3(x, 0f, z).normalized;
    }

    private Vector3 ConvertInputToWorld(Vector3 input)
    {
        if (cameraTransform == null) return input;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.z + cameraRight * input.x).normalized;
    }

    private void MoveCharacter(Vector3 moveWorld)
    {
        if (characterController != null)
        {
            characterController.SimpleMove(moveWorld * moveSpeed);
        }
        else
        {
            transform.position += moveWorld * (moveSpeed * Time.deltaTime);
        }

        if (moveWorld.sqrMagnitude > 0.001f)
        {
            transform.forward = moveWorld.normalized;
        }
    }

    private void UpdateAnimation(Vector3 moveWorld)
    {
        if (isAttacking)
        {
            if (Time.time >= attackEndTime)
            {
                isAttacking = false;
            }
            else
            {
                EnsureStatePlaying(attackStateName);
                return;
            }
        }

        if (moveWorld.sqrMagnitude > 0.0001f)
        {
            EnsureStatePlaying(runStateName);
        }
        else
        {
            EnsureStatePlaying(idleStateName);
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackEndTime = Time.time + GetAttackDuration();

        EnsureStatePlaying(attackStateName, true);
        DestroyClosestEnemyInRange();
        CheckEnemyAndGateState();
    }

    private float GetAttackDuration()
    {
        if (cachedAttackDuration > 0f) return cachedAttackDuration;

        cachedAttackDuration = 0.8f;

        if (animator == null) return cachedAttackDuration;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null) return cachedAttackDuration;

        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) continue;

            if (string.Equals(clip.name, attackStateName, StringComparison.Ordinal))
            {
                cachedAttackDuration = Mathf.Max(0.1f, clip.length);
                return cachedAttackDuration;
            }
        }

        return cachedAttackDuration;
    }

    private void EnsureStatePlaying(string stateName, bool forceRestart = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash)) return;

        if (!forceRestart && currentAnimationHash == hash) return;

        if (forceRestart)
        {
            animator.Play(hash, 0, 0f);
        }
        else
        {
            animator.CrossFadeInFixedTime(hash, 0.08f, 0);
        }

        currentAnimationHash = hash;
    }

    private void DestroyClosestEnemyInRange()
    {
        List<GameObject> enemies = new List<GameObject>(32);
        int aliveCount = CollectAliveEnemies(enemies);
        if (aliveCount <= 0) return;

        float rangeSquared = attackRange * attackRange;
        float bestSquaredDistance = float.MaxValue;
        GameObject closestEnemy = null;

        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            float squaredDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (squaredDistance > rangeSquared) continue;
            if (squaredDistance >= bestSquaredDistance) continue;

            bestSquaredDistance = squaredDistance;
            closestEnemy = enemy;
        }

        if (closestEnemy == null) return;

        EnemyHP enemyHealth = closestEnemy.GetComponent<EnemyHP>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(int.MaxValue);
        }

        if (closestEnemy != null)
        {
            Destroy(closestEnemy);
        }
    }

    private void CheckEnemyAndGateState()
    {
        int enemyCount = CountEnemiesAlive();

        if (enemyCount > 0)
        {
            hasSeenEnemy = true;
        }

        bool enemyCleared = enemyCount <= 0 && (hasSeenEnemy || removeGateWhenNoEnemyAlive);

        if (gateRemoved) return;
        if (!enemyCleared) return;

        GameObject gateObject = FindSceneObjectByName(gateObjectName);
        if (gateObject != null)
        {
            gateObject.SetActive(false);
        }

        gateRemoved = true;
    }

    private int CountEnemiesAlive()
    {
        return CollectAliveEnemies(null);
    }

    private int CollectAliveEnemies(List<GameObject> results)
    {
        if (results != null)
        {
            results.Clear();
        }

        HashSet<int> uniqueIds = new HashSet<int>();

        void TryAdd(GameObject gameObject)
        {
            if (gameObject == null || !gameObject.activeInHierarchy) return;

            int instanceId = gameObject.GetInstanceID();
            if (!uniqueIds.Add(instanceId)) return;

            if (results != null)
            {
                results.Add(gameObject);
            }
        }

        try
        {
            GameObject[] taggedEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int i = 0; i < taggedEnemies.Length; i++)
            {
                TryAdd(taggedEnemies[i]);
            }
        }
        catch (UnityException)
        {
        }

        EnemyHP[] healthEnemies = UnityEngine.Object.FindObjectsByType<EnemyHP>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < healthEnemies.Length; i++)
        {
            if (healthEnemies[i] == null) continue;
            TryAdd(healthEnemies[i].gameObject);
        }

        EnemyChasePlayer[] chaseEnemies = UnityEngine.Object.FindObjectsByType<EnemyChasePlayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < chaseEnemies.Length; i++)
        {
            if (chaseEnemies[i] == null) continue;
            TryAdd(chaseEnemies[i].gameObject);
        }

        return uniqueIds.Count;
    }

    private bool IsVictoryTrigger(Transform hitTransform)
    {
        Transform current = hitTransform;

        while (current != null)
        {
            if (string.Equals(current.name, victoryTriggerName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ShowVictoryUI()
    {
        hasWon = true;

        if (victoryPanel == null)
        {
            victoryPanel = FindVictoryPanelInScene();
        }

        if (victoryPanel == null && autoCreateVictoryPanelIfMissing)
        {
            victoryPanel = CreateVictoryPanel();
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (pauseOnVictory)
        {
            Time.timeScale = 0f;
        }
    }

    private GameObject FindVictoryPanelInScene()
    {
        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current == null) continue;

            string lowerName = current.name.ToLowerInvariant();
            for (int j = 0; j < victoryKeywords.Length; j++)
            {
                if (!lowerName.Contains(victoryKeywords[j])) continue;
                return current.gameObject;
            }
        }

        return null;
    }

    private GameObject CreateVictoryPanel()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "Canvas_Auto",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject panelObject = new GameObject(
            "VictoryPanel_Auto",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject textObject = new GameObject(
            "VictoryText_Auto",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(700f, 160f);
        textRect.anchoredPosition = Vector2.zero;

        Text textComponent = textObject.GetComponent<Text>();
        textComponent.text = "Victory!";
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 96;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = new Color(1f, 0.85f, 0.2f, 1f);

        panelObject.SetActive(false);
        return panelObject;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current == null) continue;

            if (string.Equals(current.name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return current.gameObject;
            }
        }

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current == null) continue;

            if (current.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return current.gameObject;
            }
        }

        return null;
    }
}