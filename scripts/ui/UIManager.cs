using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

using Label = UnityEngine.UIElements.Label;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Documents")]
    public UIDocument uiDocument;

    [Header("UXML Assets")]
    public VisualTreeAsset hudAsset;
    public VisualTreeAsset upgradeAsset;
    public VisualTreeAsset clearScreenAsset;

    private VisualElement root;
    private VisualElement hudContainer;
    private VisualElement upgradeContainer;
    private VisualElement clearScreenContainer;

    // HUD 내부 요소들
    private VisualElement playerPortrait;
    private VisualElement hpFill;
    private VisualElement shieldFill;
    private VisualElement xpFill;
    private Label hpText;
    private Label shieldText;
    private Label xpText;

    private Coroutine xpCoroutine;
    private Coroutine hpCoroutine;
    private Coroutine shieldCoroutine;

    private Label goldText;
    private Coroutine goldCoroutine;

    private Label killText;
    private Label timeText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
    }

    private void Start()
    {
        InitializeContainers();
        InitializeHUDReferences();
        ApplyCharacterIcon();

        StartCoroutine(AddListeners());

        ShowHUD();
    }

    IEnumerator AddListeners()
    {
        // HP 매니저
        yield return new WaitUntil(() => FindFirstObjectByType<PlayerHPManager>() != null);
        var hpManager = FindFirstObjectByType<PlayerHPManager>();
        hpManager.OnHealthChanged.AddListener(UpdateHealthBar);
        hpManager.OnShieldChanged.AddListener(UpdateShieldBar);

        // 초기 HP/Shield UI 업데이트 (구독 후 즉시 반영)
        hpManager.InitializeUI();

        // XP 매니저
        yield return new WaitUntil(() => PlayerXPManager.Instance != null);

        // 준비됐으면 구독 진행
        var xpManager = PlayerXPManager.Instance;
        xpManager.OnXPChange.AddListener((cur, req) =>
        {
            UpdateXPBar(cur, req, xpManager.CurrentLevel);
        });

        // 초기 UI 업데이트
        UpdateXPBar(xpManager.currentXP, xpManager.requiredXP, xpManager.CurrentLevel);

        // Gold 매니저 연결
        yield return new WaitUntil(() => PlayerGoldManager.Instance != null);
        PlayerGoldManager.Instance.OnGoldChanged.AddListener(UpdateGoldText);

        UpdateGoldText(PlayerGoldManager.Instance.GetCurrentGold());
    }

    private void InitializeContainers()
    {
        root.Clear();

        // HUD 생성 및 추가
        if (hudAsset != null)
        {
            hudContainer = hudAsset.Instantiate();
            hudContainer.style.flexGrow = 1;
            root.Add(hudContainer);
        }

        // Upgrade UI 생성 및 추가
        if (upgradeAsset != null)
        {
            upgradeContainer = upgradeAsset.Instantiate();
            upgradeContainer.style.flexGrow = 1;
            root.Add(upgradeContainer);

            var upgradeManager = UpgradeUIManager.Instance;
            if (upgradeManager == null)
                upgradeManager = FindFirstObjectByType<UpgradeUIManager>();

            if (upgradeManager != null)
            {
                upgradeManager.Initialize(upgradeContainer);
            }
            else
            {
                Debug.LogError("UpgradeUIManager 인스턴스를 찾을 수 없습니다!");
            }
        }

        // Clear Screen UI 생성 및 추가
        if (clearScreenAsset != null)
        {
            clearScreenContainer = clearScreenAsset.Instantiate();
            clearScreenContainer.style.position = Position.Absolute;
            clearScreenContainer.style.width = new Length(100, LengthUnit.Percent);
            clearScreenContainer.style.height = new Length(100, LengthUnit.Percent);
            clearScreenContainer.name = "ClearScreenUIContainer"; 
            clearScreenContainer.style.display = DisplayStyle.None; 
            root.Add(clearScreenContainer);

            Button btnReturnMenu = clearScreenContainer.Q<Button>("BtnReturnMenu");
            if (btnReturnMenu != null)
            {
                btnReturnMenu.clicked += OnReturnMenuClicked;
            }
        }
    }

    private void InitializeHUDReferences()
    {
        if (hudContainer == null) return;
        playerPortrait = hudContainer.Q<VisualElement>("player-portrait");
        hpFill = hudContainer.Q<VisualElement>("hp-bar-fill");
        shieldFill = hudContainer.Q<VisualElement>("shield-bar-fill");
        xpFill = hudContainer.Q<VisualElement>("xp-bar-fill");
        hpText = hudContainer.Q<Label>("hp-text");
        shieldText = hudContainer.Q<Label>("shield-text");
        xpText = hudContainer.Q<Label>("xp-text");
        goldText = hudContainer.Q<Label>("gold-text");
        killText = hudContainer.Q<Label>("kill-text");
        timeText = hudContainer.Q<Label>("time-text");
    }

    private void ApplyCharacterIcon()
    {
        if (playerPortrait == null) return;

        CharacterData data = GameManager.Instance?.selectedCharacter;

        if (data != null && data.characterIcon != null)
        {
            playerPortrait.style.backgroundImage = new StyleBackground(data.characterIcon);
        }
    }

    // --- 실시간 전환 로직 ---
    public void UpdateHealthBar(float current, float max)
    {
        if (hpFill == null) return;

        // 텍스트는 즉시 갱신
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(current)} / {max}";

        // 바는 Lerp로 부드럽게 전환
        float targetPercent = Mathf.Clamp01(current / max) * 100f;
        if (hpCoroutine != null) StopCoroutine(hpCoroutine);
        hpCoroutine = StartCoroutine(SmoothHPUpdate(targetPercent));
    }

    private IEnumerator SmoothHPUpdate(float targetPercent)
    {
        float currentPercent = hpFill.style.width.value.value;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newWidth = Mathf.Lerp(currentPercent, targetPercent, elapsed / duration);
            hpFill.style.width = Length.Percent(newWidth);
            yield return null;
        }
        hpFill.style.width = Length.Percent(targetPercent);
    }

    public void UpdateShieldBar(float current, float max)
    {
        if (shieldFill == null) return;

        // 텍스트 업데이트
        if (shieldText != null)
            shieldText.text = current > 0 ? $"SHIELD {Mathf.CeilToInt(current)}" : "";

        // 바 부드럽게 업데이트
        float targetPercent = Mathf.Clamp01(current / max) * 100f;
        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = StartCoroutine(SmoothShieldUpdate(targetPercent));
    }

    private IEnumerator SmoothShieldUpdate(float targetPercent)
    {
        float currentPercent = shieldFill.style.width.value.value;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newWidth = Mathf.Lerp(currentPercent, targetPercent, elapsed / duration);
            shieldFill.style.width = Length.Percent(newWidth);
            yield return null;
        }
        shieldFill.style.width = Length.Percent(targetPercent);
    }

    public void UpdateXPBar(float currentXP, float requiredXP, int level)
    {
        if (xpFill == null) return;

        float ratio = (requiredXP > 0) ? (currentXP / requiredXP) * 100f : 0f;
        ratio = Mathf.Clamp(ratio, 0f, 100f);

        if (xpText != null)
            xpText.text = $"LV.{level} ({ratio:F1}%)";

        if (xpCoroutine != null) StopCoroutine(xpCoroutine);
        xpCoroutine = StartCoroutine(SmoothXPUpdate(ratio));
    }

    private IEnumerator SmoothXPUpdate(float targetPercent)
    {
        float currentPercent = xpFill.style.width.value.value;

        if (targetPercent < currentPercent)
        {
            currentPercent = 0f;
            xpFill.style.width = Length.Percent(0);
        }

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newWidth = Mathf.Lerp(currentPercent, targetPercent, elapsed / duration);

            xpFill.style.width = Length.Percent(newWidth);
            yield return null;
        }
        xpFill.style.width = Length.Percent(targetPercent);
    }

    // 골드 텍스트 갱신 함수
    public void UpdateGoldText(int targetGold)
    {
        if (goldText == null) return;

        if (goldCoroutine != null) StopCoroutine(goldCoroutine);
        goldCoroutine = StartCoroutine(SmoothGoldCounter(targetGold));
    }

    private IEnumerator SmoothGoldCounter(int targetGold)
    {
        // 현재 텍스트에서 숫자만 추출하거나 변수로 관리
        int.TryParse(goldText.text.Replace(",", ""), out int startGold);

        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            int current = (int)Mathf.Lerp(startGold, targetGold, elapsed / duration);
            goldText.text = current.ToString("N0");
            yield return null;
        }
        goldText.text = targetGold.ToString("N0");
    }

    // 킬 수 업데이트 (EnemyManager 등에서 호출)
    public void UpdateKillCount(int count)
    {
        if (killText != null) killText.text = $"💀 {count}";
    }

    // 시간 업데이트 (매 프레임 혹은 1초마다 호출)
    public void UpdateStageTime(float seconds)
    {
        if (timeText != null)
        {
            int min = Mathf.FloorToInt(seconds / 60);
            int sec = Mathf.FloorToInt(seconds % 60);
            timeText.text = $"⏳ {min:00}:{sec:00}";
        }
    }

    // --- UI 전환 로직 ---
    public void ShowHUD()
    {
        if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;
        if (upgradeContainer != null) upgradeContainer.style.display = DisplayStyle.None;
    }

    public void ShowUpgradeUI(Weapon targetWeapon)
    {
        if (hudContainer != null) hudContainer.style.display = DisplayStyle.None;
        if (upgradeContainer != null) upgradeContainer.style.display = DisplayStyle.Flex;

        UpgradeUIManager.Instance.OpenUpgradeUI(targetWeapon);
    }

    // --- 상호작용 프롬프트 ---
    private VisualElement _interactionPrompt;
    private Label _interactionText;
    private Transform _interactionTarget;
    private float _interactionTargetHeightOffset;

    public void ShowInteractionPrompt(Transform target, string text, float heightOffset = 2f)
    {
        if (_interactionPrompt == null)
        {
            _interactionPrompt = root?.Q<VisualElement>("interaction-prompt");
            _interactionText = _interactionPrompt?.Q<Label>("interaction-text");
        }

        if (_interactionPrompt == null) return;

        _interactionTarget = target;
        _interactionTargetHeightOffset = heightOffset;
        
        if (_interactionText != null) _interactionText.text = text;
        _interactionPrompt.style.display = DisplayStyle.Flex;
    }

    public void HideInteractionPrompt()
    {
        _interactionTarget = null;
        if (_interactionPrompt != null)
        {
            _interactionPrompt.style.display = DisplayStyle.None;
        }
    }

    // --- 게임 클리어 화면 표시 ---
    public void ShowClearScreen()
    {
        if (clearScreenContainer == null) 
        {
            Debug.LogError("<color=red>ClearScreen UI Document가 UIManager에 할당되지 않았습니다! (수동 추가 필요)</color>");
            // 소프트락 방지를 위해 메인 메뉴로 바로 넘김
            if (GameManager.Instance != null) GameManager.Instance.ResetRunData();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
            return;
        }

        // 실제 컨테이너의 display 상태를 켜줌 (VisualTree 내부의 요소)
        VisualElement innerContainer = clearScreenContainer.Q<VisualElement>("ClearScreenContainer");
        if (innerContainer != null) innerContainer.style.display = DisplayStyle.Flex;
        
        clearScreenContainer.style.display = DisplayStyle.Flex;

        // 1. 달성 레벨
        Label lblLevel = clearScreenContainer.Q<Label>("txt_finalLevel");
        if (lblLevel != null && PlayerXPManager.Instance != null)
        {
            lblLevel.text = $"Lv {PlayerXPManager.Instance.CurrentLevel}";
        }

        // 2. 누적 플레이 시간 (GameManager의 totalRunTime + 현재 스테이지 시간)
        Label lblTime = clearScreenContainer.Q<Label>("txt_totalTime");
        if (lblTime != null && GameManager.Instance != null && StageManager.Instance != null)
        {
            float totalTime = GameManager.Instance.totalRunTime + StageManager.Instance.GetCurrentTime();
            int minutes = Mathf.FloorToInt(totalTime / 60F);
            int seconds = Mathf.FloorToInt(totalTime - minutes * 60);
            lblTime.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // 3. 골드
        Label lblGold = clearScreenContainer.Q<Label>("txt_totalGold");
        if (lblGold != null && PlayerGoldManager.Instance != null)
        {
            lblGold.text = $"{PlayerGoldManager.Instance.GetCurrentGold():N0} G";
        }

        // 4. 클리어 페이즈
        Label lblPhases = clearScreenContainer.Q<Label>("txt_phasesCleared");
        if (lblPhases != null && GameManager.Instance != null)
        {
            lblPhases.text = $"{GameManager.Instance.currentPhase}";
        }

        // 게임 일시정지 및 커서 활성화
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void OnReturnMenuClicked()
    {
        // 일시정지 풀기
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRunData();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }

    private void LateUpdate()
    {
        if (_interactionTarget != null && _interactionPrompt != null && _interactionPrompt.style.display == DisplayStyle.Flex)
        {
            Vector3 worldPos = _interactionTarget.position + Vector3.up * _interactionTargetHeightOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 카메라 뒤에 있는 경우 숨김 처리
            if (screenPos.z < 0)
            {
                _interactionPrompt.style.opacity = 0;
            }
            else
            {
                _interactionPrompt.style.opacity = 1;
                // UI Toolkit 화면 좌표로 변환 (Screen.height 최하단 -> UI Toolkit Top 최상단)
                
                // transform.position(절대 좌표) 지원 버전에 맞춰 width/height 보정
                // Translate는 픽셀 기준이므로 -50%, -100% 대신 style.translate에 Percent 적용
                _interactionPrompt.style.left = screenPos.x;
                _interactionPrompt.style.top = Screen.height - screenPos.y;
                _interactionPrompt.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent));
            }
        }
    }
}