using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine.InputSystem;
using StarterAssets;
using System.Collections;
public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("UI Toolkit References")]
    public UIDocument uiDocument;
    public VisualElement root;

    [Header("Icons")]
    public Sprite lockIconSprite;

    private VisualElement selectionList;
    private VisualElement itemContainer;
    private ScrollView statList;
    private ScrollView synergyScroll;
    private VisualElement synergyDetailPanel;

    private List<VisualElement> weaponSlots = new List<VisualElement>();
    private List<VisualElement> coreSlots = new List<VisualElement>();

    private VisualElement baseStatList;
    private VisualElement specialStatList;
    private Label inspectorNameLabel;
    private VisualElement _inspectorContainer;
    private int _currentRerollCost = 100;
    private Label _currentGoldLabel;
    private Label _rerollCostLabel;
    private Button _rerollButton;

    private PlayerInput playerInput;

    private int _currentFocusIndex = 0;
    private List<Button> _activeCards = new List<Button>();
    // 현재 UI에 표시된 업그레이드 선택지 캐시
    private List<AppliedUpgrade> _currentChoices = new List<AppliedUpgrade>();
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    private void Update()
    {
        if (root.style.display == DisplayStyle.None || _activeCards.Count == 0) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool inputDetected = false;

        // 키보드 입력 감지
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        {
            _currentFocusIndex = (_currentFocusIndex - 1 + _activeCards.Count) % _activeCards.Count;
            inputDetected = true;
        }
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
        {
            _currentFocusIndex = (_currentFocusIndex + 1) % _activeCards.Count;
            inputDetected = true;
        }

        if (inputDetected)
        {
            UpdateCardFocus();
        }

        // 스페이스바/엔터 입력 감지
        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
        {
            if (_currentFocusIndex >= 0 && _currentFocusIndex < _activeCards.Count)
            {
                _activeCards[_currentFocusIndex].Focus();
                var clickable = _activeCards[_currentFocusIndex].clickable;
                ExecuteCardSelection(_currentFocusIndex);
            }
        }

        // R키로 리롤
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            OnRerollClick();
        }
    }

    private void ExecuteCardSelection(int index)
    {
        if (_currentChoices == null || _currentChoices.Count == 0) return;
        if (index < 0 || index >= _currentChoices.Count) return;

        AppliedUpgrade data = _currentChoices[index];

        if (data.isNewWeapon)
        {
            if (data.weaponPrefab != null)
                PlayerWeaponManager.Instance.EquipWeapon(data.weaponPrefab);
        }
        else
        {
            data.targetWeapon.ApplySelectedUpgrade(data);
        }

        CloseUpgradeUI();
    }

    private void UpdateCardFocus()
    {
        for (int i = 0; i < _activeCards.Count; i++)
        {
            if (i == _currentFocusIndex)
            {
                _activeCards[i].AddToClassList("card-focused");
                _activeCards[i].Focus();
            }
            else
            {
                _activeCards[i].RemoveFromClassList("card-focused");
            }
        }
    }

    public void Initialize(VisualElement upgradeRoot)
    {
        root = upgradeRoot;
        InitializeUIReferences();
        root.style.display = DisplayStyle.None;
    }

    private void InitializeUIReferences()
    {
        selectionList = root.Q<VisualElement>("selection-list");
        itemContainer = root.Q<VisualElement>("item-container");
        statList = root.Q<ScrollView>("stat-list");
        synergyScroll = root.Q<ScrollView>("synergy-scroll");
        synergyDetailPanel = root.Q<VisualElement>("synergy-detail-panel");
        _inspectorContainer = root.Q<VisualElement>("weapon-inspector");
        _currentGoldLabel = root.Q<Label>("current-gold-label");
        _rerollCostLabel = root.Q<Label>("reroll-cost-label");
        _rerollButton = root.Q<Button>("reroll-button");
        // 리스트 초기화 후 추가
        weaponSlots.Clear(); 
        coreSlots.Clear();

        for (int i = 0; i < 4; i++)
        {
            weaponSlots.Add(root.Q<VisualElement>($"weapon-slot-{i}"));
            coreSlots.Add(root.Q<VisualElement>($"core-slot-{i}"));
        }

        if (_rerollButton != null)
            _rerollButton.clicked += OnRerollClick;

        SetupInspectorReferences();
    }
    private void SetupInspectorReferences()
    {
        inspectorNameLabel = root.Q<Label>("inspector-name");
        baseStatList = root.Q<VisualElement>("base-stat-list");
        specialStatList = root.Q<VisualElement>("special-stat-list");
    }
    private IEnumerator SetInitialFocus()
    {
        yield return null; // 한 프레임 대기
        UpdateCardFocus();
    }

    public void OpenUpgradeUI(Weapon targetWeapon)
    {
        Time.timeScale = 0f;
        root.style.display = DisplayStyle.Flex;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerInput = playerObj.GetComponent<PlayerInput>();

            // 2. 입력 시스템 차단
            if (playerInput != null) playerInput.enabled = false;

            // 3. StarterAssetsInputs 제어 (커서 가둠 해제)
            var inputs = playerObj.GetComponent<StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorLocked = false;
                inputs.cursorInputForLook = false; // 시선 회전 입력도 명시적으로 차단
            }
        }

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        // UI가 열릴 때 한 번만 선택지 생성 후 캐시
        _currentChoices = UpgradeManager.Instance.GetUpgradeChoices();
        RefreshAllPanels(targetWeapon);

        _currentRerollCost = 100;
        UpdateGoldAndRerollUI();

        _currentFocusIndex = 0;
        StartCoroutine(SetInitialFocus());
    }

    public void CloseUpgradeUI()
    {
        if (PlayerXPManager.Instance != null)
        {
            if (PlayerXPManager.Instance.HasPendingUpgrades())
            {
                PlayerXPManager.Instance.TryOpenUpgradeUI();
                return;
            }
        }

        root.style.display = DisplayStyle.None;
        UIManager.Instance.ShowHUD();

        if (playerInput != null)
        {
            playerInput.enabled = true;
            var inputs = playerInput.GetComponent<StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorLocked = true;
                inputs.cursorInputForLook = true;
            }
        }

        Time.timeScale = 1f;
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void RefreshAllPanels(Weapon weapon)
    {
        UpdateCharacterSection();    // 1. 캐릭터 기본 정보
        UpdateSynergySection();      // 2. 시너지 리스트
        UpdateWeaponAndCoreSlots();  // 3. 무기/코어 슬롯
        UpdateStatSheet();           // 4. 상세 스탯
        UpdateUpgradeCards(weapon);  // 5. 중앙 업그레이드 카드
    }
    private void UpdateCharacterSection()
    {
        CharacterData data = GameManager.Instance?.selectedCharacter;
        if (data == null) return;

        // 레벨 표시
        var lvLabel = root.Q<Label>("char_LV");
        if (lvLabel != null && PlayerXPManager.Instance != null)
        {
            lvLabel.text = $"LV {PlayerXPManager.Instance.CurrentLevel}";
        }

        // 초상화 설정
        var portrait = root.Q<VisualElement>("char-portrait");
        if (portrait != null && data.characterIcon != null)
        {
            portrait.style.backgroundImage = new StyleBackground(data.characterIcon);
        }

        var classLabel = root.Q<Label>("char-class");
        if (classLabel != null)
        {
            classLabel.text = LocalizationManager.GetText(data.characterName);
        }
        else
        {
            // 혹시 char-class가 VisualElement고 그 안에 Label이 들어있는 구조라면:
            var nestedLabel = root.Q<VisualElement>("char-class")?.Q<Label>();
            if (nestedLabel != null) nestedLabel.text = classLabel.text = LocalizationManager.GetText(data.characterName);
        }

        // 정적 라벨 번역
        SetLabelText("label-character-title", "ui_title_character");
        SetLabelText("label-weapons-subtitle", "ui_subtitle_weapons");
        SetLabelText("label-cores-subtitle", "ui_subtitle_cores");
        SetLabelText("label-items-subtitle", "ui_subtitle_items");
        SetLabelText("label-stats-title", "ui_title_stats");
        SetLabelText("label-upgrade-title", "ui_title_upgrade");
        SetLabelText("label-synergy-title", "ui_subtitle_synergy");

        // 캐릭터 이름 등 동적 정보
        var nameLabel = root.Q<Label>("char-name");
        if (nameLabel != null) nameLabel.text = "CHLOE";
    }

    // 헬퍼 함수: 라벨이 존재할 때만 번역 텍스트를 할당
    private void SetLabelText(string uiName, string locKey)
    {
        var label = root.Q<Label>(uiName);
        if (label != null) label.text = LocalizationManager.GetText(locKey);
    }

    private void UpdateSynergySection()
    {
        // 타이틀 번역
        var synergyTitle = root.Q<Label>("label-synergy-title");
        if (synergyTitle != null)
            synergyTitle.text = LocalizationManager.GetText("ui_subtitle_synergy");

        if (synergyScroll == null) return;
        synergyScroll.Clear();

        var player = PlayerStatusManager.Instance;
        if (player == null) return;

        var tagCounts = player.GetCurrentTagCounts();
        if (tagCounts == null) return;

        foreach (var tagPair in tagCounts.OrderByDescending(x => x.Value))
        {
            if (tagPair.Value <= 0) continue;

            // 시너지 아이템 생성
            VisualElement synergyItem = new VisualElement();
            synergyItem.AddToClassList("synergy-item-style");

            // [태그 이름 : 개수] 포맷
            string tagName = LocalizationManager.GetText(tagPair.Key.ToString());
            int count = tagPair.Value;
            Label infoLabel = new Label($"{tagName} : {count} / 4");
            infoLabel.AddToClassList("stat-text");

            if (count == 1)
            {
                synergyItem.AddToClassList("synergy-tier-1");
                synergyItem.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            }
            else if (count == 2)
            {
                synergyItem.AddToClassList("synergy-tier-2");
            }
            else if (count == 3)
            {
                synergyItem.AddToClassList("synergy-tier-3");
            }
            else if (count >= 4)
            {
                synergyItem.AddToClassList("synergy-tier-4");
                synergyItem.style.borderLeftColor = new Color(1f, 0.2f, 0.2f);
            }

            WeaponTag currentTag = tagPair.Key;
            int currentCount = tagPair.Value;

            synergyItem.RegisterCallback<MouseEnterEvent>(evt => ShowSynergyDetail(currentTag, currentCount));
            synergyItem.RegisterCallback<MouseLeaveEvent>(evt => HideSynergyDetail());

            synergyItem.Add(infoLabel);
            synergyScroll.Add(synergyItem);
        }
    }

    private void ShowSynergyDetail(WeaponTag tag, int currentCount)
    {
        if (synergyDetailPanel == null) return;

        synergyDetailPanel.Clear();
        synergyDetailPanel.style.display = DisplayStyle.Flex;

        // 태그 이름 추가
        Label title = new Label(LocalizationManager.GetText(tag.ToString()));
        title.AddToClassList("detail-tag-name");
        synergyDetailPanel.Add(title);

        // 2, 3, 4단계 효과 순차적으로 표시
        for (int tier = 2; tier <= 4; tier++)
        {
            string effectKey = $"Tag_{tag}_Tier{tier}_Desc";
            string effectDesc = LocalizationManager.GetText(effectKey);

            Label effectLabel = new Label($"- {tier}단계: {effectDesc}");
            effectLabel.AddToClassList("detail-effect-row");

            if (currentCount >= tier)
            {
                effectLabel.AddToClassList("detail-effect-active");
            }

            synergyDetailPanel.Add(effectLabel);
        }
    }

    private void HideSynergyDetail()
    {
        if (synergyDetailPanel != null)
            synergyDetailPanel.style.display = DisplayStyle.None;
    }
    private void UpdateWeaponAndCoreSlots()
    {
        var weaponManager = PlayerWeaponManager.Instance;
        if (weaponManager == null) weaponManager = Object.FindFirstObjectByType<PlayerWeaponManager>();
        if (weaponManager == null) return;

        var activeWeapons = weaponManager.activeWeapons;
        int unlockedCount = weaponManager.CurrentUnlockedSlots;

        for (int i = 0; i < 4; i++)
        {
            VisualElement slot = weaponSlots[i];
            slot.Clear();
            slot.RemoveFromClassList("slot-locked");

            weaponSlots[i].Clear();
            if (i < unlockedCount)
            {
                // [해금된 슬롯]
                if (i < activeWeapons.Count)
                {
                    VisualElement iconContainer = new VisualElement();
                    iconContainer.AddToClassList("icon-container");
                    iconContainer.style.width = Length.Percent(85);
                    iconContainer.style.height = Length.Percent(85);

                    if (activeWeapons[i].weaponData.icon != null)
                    {
                        iconContainer.style.backgroundImage = new StyleBackground(activeWeapons[i].weaponData.icon);
                    }
                    slot.Add(iconContainer);

                    int index = i;
                    slot.RegisterCallback<MouseEnterEvent>(evt => ShowWeaponTooltip(slot, activeWeapons[index]));
                    slot.RegisterCallback<MouseLeaveEvent>(evt => HideWeaponTooltip());
                }
            }
            else
            {
                // [잠긴 슬롯]
                slot.AddToClassList("slot-locked");
                VisualElement lockIcon = new VisualElement();
                lockIcon.AddToClassList("lock-icon");

                if (lockIconSprite != null)
                {
                    lockIcon.style.backgroundImage = new StyleBackground(lockIconSprite);
                }

                slot.Add(lockIcon);
            }
        }
    }
    private void UpdateStatSheet()
    {
        statList.Clear();
        var player = PlayerStatusManager.Instance;
        if (player == null) return;

        FieldInfo[] fields = typeof(PlayerStatusManager).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(Stat))
            {
                Stat stat = (Stat)field.GetValue(player);
                string localizedName = LocalizationManager.GetText(field.Name);
                string unit = LocalizationManager.GetUnit(field.Name);

                AddStatRow(localizedName, $"{stat.Value:F1}{unit}");
            }
        }
    }
    private void UpdateUpgradeCards(Weapon dummy)
    {
        // 이미 캐시된 선택지를 사용 (UI 표시와 실제 선택 일치 보장)
        if (_currentChoices == null || _currentChoices.Count == 0)
        {
            _currentChoices = UpgradeManager.Instance.GetUpgradeChoices();
        }

        List<AppliedUpgrade> choices = _currentChoices;
        _activeCards.Clear();

        for (int i = 0; i < 3; i++)
        {
            Button card = root.Q<Button>($"option-{i}");
            if (i >= choices.Count) { 
                card.style.display = DisplayStyle.None; 
                continue; 
            }
            card.style.display = DisplayStyle.Flex;
            AppliedUpgrade data = choices[i];
            _activeCards.Add(card);

            Label tierLabel = card.Q<Label>("tier-label");
            Label nameLabel = card.Q<Label>("stat-text-1");
            Label descLabel = card.Q<Label>("stat-text-2");

            VisualElement oldContainer = card.Q<VisualElement>("dynamic-tag-container");
            if (oldContainer != null) oldContainer.RemoveFromHierarchy();

            if (data.isNewWeapon)
            {
                tierLabel.text = "NEW WEAPON";
                nameLabel.text = LocalizationManager.GetText(data.weaponData.weaponName);
                descLabel.text = "새로운 무기를 장착합니다.";

                VisualElement titleRow = new VisualElement();
                titleRow.name = "dynamic-title-row";
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                titleRow.style.height = 20;
                VisualElement parent = nameLabel.parent;
                int originalIndex = parent.IndexOf(nameLabel);
                nameLabel.RemoveFromHierarchy();
                titleRow.Add(nameLabel);

                VisualElement tagContainer = new VisualElement();
                tagContainer.name = "dynamic-tag-container";
                tagContainer.AddToClassList("tag-container");
                tagContainer.style.flexDirection = FlexDirection.Row;

                // 무기가 가진 각 태그마다 배지 생성
                foreach (var tag in data.weaponTags)
                {
                    VisualElement badge = new VisualElement();
                    badge.AddToClassList("tag-badge");
                    Label badgeText = new Label(LocalizationManager.GetText(tag.ToString()));
                    badgeText.AddToClassList("tag-badge-text");
                    badge.Add(badgeText);
                    tagContainer.Add(badge);
                }
                titleRow.Add(tagContainer);
                parent.Insert(originalIndex, titleRow);
            }
            else
            {
                if (data.targetWeapon == null)
                {
                    Debug.LogError("기존 무기 강화 데이터에 targetWeapon이 없습니다!");
                    continue;
                }
                // 기존 무기 강화일 때 표시
                tierLabel.text = LocalizationManager.GetText(data.rarity.ToString());
                string weaponName = LocalizationManager.GetText(data.targetWeapon.weaponData.weaponName);
                string statName = LocalizationManager.GetText(data.statType.ToString());
                nameLabel.text = $"{weaponName} 강화";
                descLabel.text = $"{statName} +{data.value:F1}{LocalizationManager.GetUnit(data.statType.ToString())}";
            }

            ApplyTierStyle(card, tierLabel, data.rarity);
            // 클릭 이벤트
            int index = i;
            card.clickable = new Clickable(() => ExecuteCardSelection(index));

            card.RegisterCallback<MouseEnterEvent>(evt => {
                _currentFocusIndex = index;
                UpdateCardFocus();
            });
        }
    }

    private void AddStatRow(string labelText, string valueText)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("stat-row-style");

        Label nameLabel = new Label(labelText);
        nameLabel.AddToClassList("stat-text");

        Label valLabel = new Label(valueText);
        valLabel.AddToClassList("stat-text");

        row.Add(nameLabel);
        row.Add(valLabel);
        statList.Add(row);
    }
    private void ApplyTierStyle(VisualElement card, Label label, UpgradeRarity rarity)
    {
        for (int i = 1; i <= 5; i++)
        {
            card.RemoveFromClassList($"card-tier-{i}");
            label.RemoveFromClassList($"text-tier-{i}");
        }

        int tierNum = (int)rarity + 1;
        card.AddToClassList($"card-tier-{tierNum}");
        label.AddToClassList($"text-tier-{tierNum}");

        if (rarity == UpgradeRarity.Tier_V)
        {
            card.AddToClassList("mythic-pulse");
        }
    }

    // 툴팁 로직
    private void ShowWeaponTooltip(VisualElement slot, Weapon weapon)
    {
        if (weapon == null || slot == null) return;

        //이름 및 레벨 설정
        VisualElement inspectorContainer = root.Q<VisualElement>("weapon-inspector");
        if (inspectorContainer == null) return;

        string localizedWeaponName = LocalizationManager.GetText(weapon.weaponData.weaponName);
        inspectorNameLabel.text = $"{localizedWeaponName} (Lv.{weapon.currentLevel})";

        baseStatList.Clear();
        specialStatList.Clear();

        // 왼쪽 : 기본 정보
        AddStatToInspector(baseStatList, "Damage", weapon.GetFinalDamage().ToString("F1"));
        AddStatToInspector(baseStatList, "AttackSpeed", weapon.GetFinalFireRate().ToString("F2"));
        AddStatToInspector(baseStatList, "MaxDistance", weapon.GetFinalMaxDistance().ToString("F1"));
        AddStatToInspector(baseStatList, "CritChance", weapon.GetFinalCritChance().ToString("F1"));
        AddStatToInspector(baseStatList, "CritMultiplier", weapon.GetFinalCritMultiplier().ToString("F1"));
        AddStatToInspector(baseStatList, "EliteDamageMult", weapon.GetFinalEliteDamage().ToString("F1"));

        // 오른쪽 : 특수 정보
        var data = weapon.weaponData;

        // 투사체 개수: 근접이 아닐 때
        if (!data.isMelee && weapon.fireMode != Weapon.FireMode.Sequential)
        {
            AddStatToInspector(specialStatList, "ProjectileCount", weapon.GetFinalProjectileCount().ToString());
        }

        // 연발 횟수 (Burst Count)
        if (weapon.fireMode == Weapon.FireMode.Sequential ||
       (weapon.fireMode == Weapon.FireMode.Scatter && weapon.GetFinalBurstCount() > 1))
        {
            AddStatToInspector(specialStatList, "BurstCount", weapon.GetFinalBurstCount().ToString());
        }

        // 폭발 및 장판
        if (data.isExplosive) AddStatToInspector(specialStatList, "ExplosionRadius", weapon.GetFinalExplosionRadius().ToString("F1"));
        if (data.isField)
        {
            AddStatToInspector(specialStatList, "FieldDuration", weapon.GetFinalFieldDuration().ToString("F1"));
            AddStatToInspector(specialStatList, "FieldTickInterval", weapon.GetFinalFieldTickInterval().ToString("F2"));
        }

        // 관통 및 도탄
        if (weapon.GetFinalPierceCount() > 0) AddStatToInspector(specialStatList, "PierceCount", weapon.GetFinalPierceCount().ToString());
        if (weapon.GetFinalBounceCount() > 0) AddStatToInspector(specialStatList, "BounceCount", weapon.GetFinalBounceCount().ToString());

        // 연쇄(Chain)
        if (data.isChain)
        {
            AddStatToInspector(specialStatList, "ChainCount", weapon.GetFinalChainCount().ToString());
            AddStatToInspector(specialStatList, "ChainRange", weapon.GetFinalChainRange().ToString("F1"));
        }

        // 상태 이상 (StatusType에 따라 분리 표기)
        if (data.statusType != StatusType.None)
        {
            string typeName = LocalizationManager.GetText(data.statusType.ToString());

            AddStatToInspector(specialStatList, $"{typeName} 대미지", weapon.GetFinalStatusDamage().ToString("F1"));
            AddStatToInspector(specialStatList, $"{typeName} 축적률", weapon.GetFinalStatusGauge().ToString("F1"));
        }

        Rect slotBound = slot.worldBound;
        Debug.Log($"Slot Bound: {slotBound}, Root Width: {root.resolvedStyle.width}");
        if (slotBound.width == 0)
        {
            Debug.LogWarning("Tooltip: 슬롯의 크기가 0입니다. 아직 레이아웃이 계산되지 않았을 수 있습니다.");
        }
        float tooltipWidth = 1200f;
        float targetLeft = slotBound.xMax + 30f; // 오른쪽으로 15px 띄움
        float targetTop = slotBound.yMin;       // 슬롯 상단 높이에 맞춤

        // 오른쪽 화면을 벗어나면 왼쪽에 표시
        float screenWidth = root.resolvedStyle.width;
        if (targetLeft + tooltipWidth > screenWidth)
        {
            targetLeft = slotBound.xMin - tooltipWidth - 15f;
        }

        // 스타일 적용
        inspectorContainer.style.position = Position.Absolute;
        inspectorContainer.style.left = targetLeft;
        inspectorContainer.style.top = targetTop;

        inspectorContainer.pickingMode = PickingMode.Ignore;
        inspectorContainer.style.display = DisplayStyle.Flex;
        inspectorContainer.style.opacity = 1f;
    }

    // 스탯 줄 생성 헬퍼 함수
    private void AddStatToInspector(VisualElement container, string labelKey, string value)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("inspector-stat-row");

        Label label = new Label(LocalizationManager.GetText(labelKey));
        label.AddToClassList("inspector-stat-label");

        string unit = LocalizationManager.GetUnit(labelKey);

        Label val = new Label($"{value}{unit}");
        val.AddToClassList("inspector-stat-value");

        row.Add(label);
        row.Add(val);
        container.Add(row);
    }

    private void HideWeaponTooltip()
    {
        if (root == null || _inspectorContainer == null) return;
        _inspectorContainer.style.display = DisplayStyle.None;
        _inspectorContainer.style.opacity = 0f;
        _inspectorContainer.pickingMode = PickingMode.Ignore;

        if (inspectorNameLabel != null)
            inspectorNameLabel.text = ""; // 기본 텍스트 대신 비워둠

        baseStatList?.Clear();
        specialStatList?.Clear();
    }

    private void OnRerollClick()
    {
        if (PlayerGoldManager.Instance.TrySpendGold(_currentRerollCost))
        {
            // 성공 시 로직
            _currentRerollCost *= 2;
            _currentChoices = UpgradeManager.Instance.GetUpgradeChoices();
            UpdateUpgradeCards(null);
            UpdateGoldAndRerollUI();

            _currentFocusIndex = 0;
            UpdateCardFocus();
        }
        else
        {
            // 실패 시 (골드 부족)
            Debug.Log("골드가 부족하여 리롤할 수 없습니다!");
        }
    }

    private void UpdateGoldAndRerollUI()
    {
        int currentGold = PlayerGoldManager.Instance.GetCurrentGold();

        if (_currentGoldLabel != null)
            _currentGoldLabel.text = $"보유 골드: {currentGold:N0} G";

        if (_rerollCostLabel != null)
            _rerollCostLabel.text = $"새로 고침 : {_currentRerollCost} G";

        // 골드 부족 시 버튼 시각적 비활성화
        if (_rerollButton != null)
        {
            bool canAfford = currentGold >= _currentRerollCost;
            _rerollButton.style.opacity = canAfford ? 1f : 0.5f;
        }
    }
}