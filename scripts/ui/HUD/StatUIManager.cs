using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using StarterAssets;
using System.Collections.Generic;

public class StatUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject statWindow;
    [SerializeField] private Transform statsContainer; // Scroll View의 'Content'를 연결하세요!
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private GameObject categoryHeaderPrefab; // (선택) 카테고리 제목 프리팹

    [Header("Input")]
    public InputAction toggleKey;

    [Header("Controller Reference")]
    [SerializeField] private StarterAssetsInputs _input; // 인스펙터에서 플레이어 연결

    private bool _isOpen = false;
    private List<GameObject> _spawnedRows = new List<GameObject>();

    private void OnEnable()
    {
        toggleKey.Enable();
        toggleKey.performed += OnToggleKey;
    }

    private void OnDisable()
    {
        toggleKey.Disable();
        toggleKey.performed -= OnToggleKey;
    }

    private void OnToggleKey(InputAction.CallbackContext context)
    {
        ToggleWindow();
    }

    public void ToggleWindow()
    {
        _isOpen = !_isOpen;
        statWindow.SetActive(_isOpen);

        if (_isOpen)
        {
            UpdateStatUI();

            // [중요] 마우스 커서 잠금 해제 & 보이게 하기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 플레이어의 카메라 회전 입력 막기 (선택사항)
            if (_input != null) _input.cursorInputForLook = false;
        }
        else
        {
            // [중요] 다시 게임으로 돌아갈 때 마우스 잠그기
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 플레이어 카메라 회전 다시 허용
            if (_input != null) _input.cursorInputForLook = true;
        }
    }

    private void UpdateStatUI()
    {
        // 1. 기존 리스트 초기화
        foreach (var row in _spawnedRows)
        {
            Destroy(row);
        }
        _spawnedRows.Clear();

        PlayerStatusManager stats = PlayerStatusManager.Instance;
        if (stats == null) return;

        // 2. 모든 스탯 생성 (카테고리별로 정리)

        // --- 1. Survival Stats ---
        CreateCategory("Survival");
        CreateRow("Max Health", stats.maxHealth.Value);
        CreateRow("HP Regen", stats.hpRegen.Value);
        CreateRow("Armor", stats.armor.Value);
        CreateRow("Evasion", stats.evasion.Value * 100f, "%");
        CreateRow("Lifesteal", stats.lifesteal.Value * 100f, "%");
        CreateRow("Thorns", stats.thorns.Value);
        // bool 타입인 Can Overheal은 텍스트로 표현
        CreateTextRow("Can Overheal", stats.canOverheal ? "Yes" : "No");

        // --- 2. Offense Stats ---
        CreateCategory("Offense");
        CreateRow("Flat Damage", stats.flatDamage.Value);
        CreateRow("Damage Mult", stats.damageMult.Value * 100f, "%");
        CreateRow("Attack Speed", stats.attackSpeed.Value);
        CreateRow("Crit Chance", stats.critChance.Value * 100f, "%");
        CreateRow("Crit Damage", stats.critDamage.Value * 100f, "%");
        CreateRow("Elite Damage", stats.eliteDamage.Value * 100f, "%");
        CreateRow("Knockback", stats.knockback.Value);

        // --- 3. Projectile & Range ---
        CreateCategory("Projectile");
        CreateRow("Projectile Count", stats.projectileCount.Value);
        CreateRow("Projectile Pierce", stats.projectilePierce.Value);
        CreateRow("Projectile Bounces", stats.projectileBounces.Value);
        CreateRow("Projectile Chain", stats.projectileChain.Value);
        CreateRow("Projectile Speed", stats.projectileSpeed.Value);
        CreateRow("Area Size", stats.areaSize.Value * 100f, "%");
        CreateRow("Duration", stats.duration.Value, "s");

        // --- 4. Mobility ---
        CreateCategory("Mobility");
        CreateRow("Move Speed", stats.moveSpeed.Value);
        CreateRow("Jump Count", stats.jumpCount.Value);
        CreateRow("Jump Height", stats.jumpHeight.Value);

        // --- 5. Utility & Economy ---
        CreateCategory("Utility");
        CreateRow("Luck", stats.luck.Value);
        CreateRow("Pickup Range", stats.pickupRange.Value);
        CreateRow("XP Gain", stats.xpGain.Value * 100f, "%");
        CreateRow("Gold Gain", stats.goldGain.Value * 100f, "%");
    }

    // 숫자형 스탯 표시
    private void CreateRow(string label, float value, string suffix = "")
    {
        GameObject newRow = Instantiate(statRowPrefab, statsContainer);
        _spawnedRows.Add(newRow);

        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = label;
            texts[1].text = $"{value:N1}{suffix}";
        }
    }

    // 텍스트형 스탯 표시 (예: Yes/No)
    private void CreateTextRow(string label, string valueText)
    {
        GameObject newRow = Instantiate(statRowPrefab, statsContainer);
        _spawnedRows.Add(newRow);

        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = label;
            texts[1].text = valueText;
        }
    }

    // 카테고리 헤더 표시 (옵션: 헤더 프리팹 없으면 일반 텍스트로 표시해도 됨)
    private void CreateCategory(string title)
    {
        if (categoryHeaderPrefab != null)
        {
            GameObject header = Instantiate(categoryHeaderPrefab, statsContainer);
            _spawnedRows.Add(header);

            TextMeshProUGUI text = header.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = $"--- {title} ---";
        }
        else
        {
            // 헤더 프리팹이 없으면 빈 줄이라도 하나 띄워줌 (선택사항)
            // CreateTextRow("", ""); 
        }
    }
}
