// Author: Lingying Zhang
// Scene-bound UGUI controller for the MobileLens host interface.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class HostUI : MonoBehaviour
{
    public enum HostTab
    {
        HostControl,
        LightInteraction
    }

    public enum PublicScreenMode
    {
        Single,
        Compare
    }

    public enum NavigationMode
    {
        Free,
        Orientation,
        Depth
    }

    [Serializable]
    public class HostTabEvent : UnityEvent<HostTab>
    {
    }

    [Serializable]
    public class PublicScreenModeEvent : UnityEvent<PublicScreenMode>
    {
    }

    [Serializable]
    public class NavigationModeEvent : UnityEvent<NavigationMode>
    {
    }

    [Serializable]
    public class StringEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class HostStatusData
    {
        public string timeText = "9:41 AM   Tue May 14";
        public string hostName = "Moderator";
        public string viewName = "Light Interaction";
        public string inspectingUserName = "User B";
        public string castingTargetName = "Public";
        public bool castingActive = true;
    }

    [Serializable]
    public class CastingRequestData
    {
        public string id;
        public string requesterUserId;
        public string requesterName;
        public string requesterInitial;
        public string message;
        public Color userColor = new Color32(49, 91, 234, 255);
        public string createdAtUtc;
    }

    [Serializable]
    public class UserPerspectiveData
    {
        public string userId;
        public string userName;
        public string userInitial;
        public string statusText;
        public Color userColor = new Color32(49, 91, 234, 255);
    }

    [Serializable]
    public class DisplaySlotData
    {
        public int slotNumber = 1;
        public string label = "Empty slot";
        public bool selected;
    }

    [Header("Binding")]
    [SerializeField] private bool bindOnStart = true;
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private bool searchSceneWhenRootMissing = true;
    [SerializeField] private bool clearRuntimeButtonListenersBeforeBind = true;
    [SerializeField] private bool enablePanelCollapse = true;
    [SerializeField] private bool enableButtonFeedback = true;
    [SerializeField] private float buttonFeedbackSeconds = 0.16f;

    [Header("Defaults")]
    [SerializeField] private HostTab defaultTab = HostTab.HostControl;
    [SerializeField] private PublicScreenMode defaultPublicScreenMode = PublicScreenMode.Single;
    [SerializeField] private NavigationMode defaultNavigationMode = NavigationMode.Free;
    [SerializeField] private string defaultModeInstruction = "Move the iPad to adjust viewpoint";
    [SerializeField] private bool seedDemoData = true;
    [SerializeField] private bool hideCastingRequestListWhenEmpty = true;
    [SerializeField] private HostStatusData initialStatus = new HostStatusData();

    [Header("Events")]
    public HostTabEvent HostTabChanged = new HostTabEvent();
    public PublicScreenModeEvent PublicScreenModeChanged = new PublicScreenModeEvent();
    public NavigationModeEvent NavigationModeChanged = new NavigationModeEvent();
    public StringEvent CastingRequestAccepted = new StringEvent();
    public StringEvent CastingRequestRejected = new StringEvent();
    public StringEvent UserPerspectiveViewClicked = new StringEvent();
    public UnityEvent CastPublicViewClicked = new UnityEvent();
    public UnityEvent OpenSnapshotListClicked = new UnityEvent();
    public UnityEvent PushPublicScreenToUserClicked = new UnityEvent();
    public UnityEvent PositionCalibrationClicked = new UnityEvent();
    public UnityEvent CastCurrentViewToPublicClicked = new UnityEvent();
    public UnityEvent PushCurrentViewToUserClicked = new UnityEvent();

    private readonly Color _primaryBlue = new Color32(49, 91, 234, 255);
    private readonly Color _primaryBlueDark = new Color32(38, 76, 205, 255);
    private readonly Color _selectedPaleBlue = new Color32(232, 239, 255, 255);
    private readonly Color _textDark = new Color32(31, 39, 54, 255);
    private readonly Color _textMuted = new Color32(91, 101, 118, 255);
    private readonly Color _green = new Color32(96, 174, 153, 255);
    private readonly Color _inactiveDot = new Color32(178, 187, 201, 255);

    private readonly List<CastingRequestData> _castingRequests = new List<CastingRequestData>();
    private readonly List<UserPerspectiveData> _userPerspectives = new List<UserPerspectiveData>();
    private readonly List<DisplaySlotData> _displaySlots = new List<DisplaySlotData>();
    private readonly List<Transform> _castingRequestItems = new List<Transform>();
    private readonly List<Transform> _userPerspectiveItems = new List<Transform>();
    private readonly List<Transform> _displaySlotItems = new List<Transform>();
    private readonly List<Button> _castPublicViewButtons = new List<Button>();
    private readonly List<PanelBinding> _collapsiblePanels = new List<PanelBinding>();

    private HostStatusData _status = new HostStatusData();
    private HostTab _activeTab;
    private PublicScreenMode _publicScreenMode;
    private NavigationMode _navigationMode;
    private string _modeInstruction;
    private string _customBottomMessage;

    private GameObject _hostControlRoot;
    private GameObject _lightInteractionRoot;
    private GameObject _publicScreenPreviewBadge;
    private GameObject _modeToastRoot;
    private GameObject _castingRequestScrollView;
    private GameObject _castingRequestTemplate;
    private RectTransform _castingRequestContent;

    private Text _timeText;
    private Text _hostText;
    private Text _viewText;
    private Text _inspectingText;
    private Text _castingText;
    private Text _bottomModeText;
    private Text _castingRequestCountText;
    private Image _castingDot;
    private RawImage _viewportVideoDisplay;

    private SegmentBinding _hostControlTab;
    private SegmentBinding _lightInteractionTab;
    private SegmentBinding _singleModeSegment;
    private SegmentBinding _compareModeSegment;
    private SegmentBinding _freeNavigationSegment;
    private SegmentBinding _orientationNavigationSegment;
    private SegmentBinding _depthNavigationSegment;

    private class SegmentBinding
    {
        public Button Button;
        public Image Background;
        public Text Label;
    }

    private class PanelBinding
    {
        public string Name;
        public RectTransform Panel;
        public RectTransform Content;
        public Transform Header;
        public Text CollapseText;
        public float ExpandedHeight;
        public float CollapsedHeight;
        public bool Collapsed;
    }

    private void Start()
    {
        if (bindOnStart)
        {
            BindScene();
        }
    }

    [ContextMenu("Bind Existing Host UI")]
    public void BindScene()
    {
        ResolveRoot();

        if (uiRoot == null)
        {
            Debug.LogWarning("[HostUI] No UI root found. Assign uiRoot or keep a Canvas named HostUI_Canvas in the scene.", this);
            return;
        }

        _activeTab = defaultTab;
        _publicScreenMode = defaultPublicScreenMode;
        _navigationMode = defaultNavigationMode;
        _modeInstruction = defaultModeInstruction;
        CopyStatus(initialStatus, _status);

        BindCoreObjects();
        BindSegments();
        BindButtons();
        BindListItems();
        BindPanelCollapse();

        if (seedDemoData)
        {
            SeedDemoDataIfNeeded();
        }

        ApplyStatus();
        ApplyActiveTab(false);
        ApplyPublicScreenMode(_publicScreenMode, false);
        ApplyNavigationMode(_navigationMode, false);
        ApplyBottomModeHint();
        UpdateCastingRequestRows();
        UpdateUserPerspectiveRows();
        UpdateDisplaySlotRows();

    }

    public void SetStatus(HostStatusData status)
    {
        if (status == null)
        {
            return;
        }

        CopyStatus(status, _status);
        ApplyStatus();
    }

    public void SetTopStatus(
        string hostName,
        string viewName,
        string inspectingUserName,
        string castingTargetName,
        bool castingActive)
    {
        _status.hostName = hostName;
        _status.viewName = viewName;
        _status.inspectingUserName = inspectingUserName;
        _status.castingTargetName = castingTargetName;
        _status.castingActive = castingActive;
        ApplyStatus();
    }

    public void SetSystemStatus(string timeText)
    {
        _status.timeText = timeText;
        ApplyStatus();
    }

    public void SetDeviceTime(string timeText)
    {
        _status.timeText = timeText;
        ApplyStatus();
    }

    public void SetHostName(string hostName)
    {
        _status.hostName = hostName;
        ApplyStatus();
    }

    public void SetViewName(string viewName)
    {
        _status.viewName = viewName;
        ApplyStatus();
    }

    public void SetInspectingUser(string inspectingUserName)
    {
        _status.inspectingUserName = inspectingUserName;
        ApplyStatus();
    }

    public void SetCastingStatus(string targetName, bool active)
    {
        _status.castingTargetName = targetName;
        _status.castingActive = active;
        ApplyStatus();
    }

    public void SetViewportVideoTexture(Texture texture)
    {
        if (_viewportVideoDisplay == null)
        {
            ResolveRoot();
            BindCoreObjects();
        }

        if (_viewportVideoDisplay == null)
        {
            Debug.LogWarning("[HostUI] Viewport Video Display was not found under the bound UI root.", this);
            return;
        }

        _viewportVideoDisplay.texture = texture;
        ShowViewportVideo(texture != null);
    }

    public void ShowViewportVideo(bool visible)
    {
        if (_viewportVideoDisplay == null)
        {
            ResolveRoot();
            BindCoreObjects();
        }

        if (_viewportVideoDisplay != null)
        {
            _viewportVideoDisplay.gameObject.SetActive(visible && _viewportVideoDisplay.texture != null);
        }
    }

    public void ClearViewportVideo()
    {
        if (_viewportVideoDisplay == null)
        {
            ResolveRoot();
            BindCoreObjects();
        }

        if (_viewportVideoDisplay != null)
        {
            _viewportVideoDisplay.texture = null;
            _viewportVideoDisplay.gameObject.SetActive(false);
        }
    }

    public void SetActiveTab(HostTab tab)
    {
        _activeTab = tab;
        ApplyActiveTab(true);
        ApplyBottomModeHint();
        ClearSelectedGameObject();
    }

    public void SetPublicScreenMode(PublicScreenMode mode)
    {
        _publicScreenMode = mode;
        ApplyPublicScreenMode(mode, true);
        ClearSelectedGameObject();
    }

    public void SetNavigationMode(NavigationMode mode)
    {
        _navigationMode = mode;
        ApplyNavigationMode(mode, true);
        ApplyBottomModeHint();
        ClearSelectedGameObject();
    }

    public void SetBottomModeInstruction(string instruction)
    {
        _modeInstruction = instruction;
        _customBottomMessage = null;
        ApplyBottomModeHint();
    }

    public void SetBottomMessage(string message)
    {
        _customBottomMessage = message;
        ApplyBottomModeHint();
    }

    public void SetCastingRequests(IEnumerable<CastingRequestData> requests)
    {
        _castingRequests.Clear();

        if (requests != null)
        {
            foreach (CastingRequestData request in requests)
            {
                if (request != null)
                {
                    _castingRequests.Add(CloneCastingRequest(request));
                }
            }
        }

        _castingRequests.Sort((left, right) => ParseTimeUtc(right.createdAtUtc).CompareTo(ParseTimeUtc(left.createdAtUtc)));
        UpdateCastingRequestRows();
    }

    public void AddCastingRequest(CastingRequestData request)
    {
        if (request == null)
        {
            return;
        }

        CastingRequestData clone = CloneCastingRequest(request);
        RemoveCastingRequestData(clone.id);
        _castingRequests.Insert(0, clone);
        _castingRequests.Sort((left, right) => ParseTimeUtc(right.createdAtUtc).CompareTo(ParseTimeUtc(left.createdAtUtc)));
        UpdateCastingRequestRows();
    }

    public void RemoveCastingRequest(string requestId)
    {
        if (RemoveCastingRequestData(requestId))
        {
            UpdateCastingRequestRows();
        }
    }

    public void ClearCastingRequests()
    {
        _castingRequests.Clear();
        UpdateCastingRequestRows();
    }

    public void SetUserPerspectives(IEnumerable<UserPerspectiveData> perspectives)
    {
        _userPerspectives.Clear();

        if (perspectives != null)
        {
            foreach (UserPerspectiveData perspective in perspectives)
            {
                if (perspective != null)
                {
                    _userPerspectives.Add(CloneUserPerspective(perspective));
                }
            }
        }

        UpdateUserPerspectiveRows();
    }

    public void SetDisplaySlots(IEnumerable<DisplaySlotData> slots)
    {
        _displaySlots.Clear();

        if (slots != null)
        {
            foreach (DisplaySlotData slot in slots)
            {
                if (slot != null)
                {
                    _displaySlots.Add(CloneDisplaySlot(slot));
                }
            }
        }

        _displaySlots.Sort((left, right) => left.slotNumber.CompareTo(right.slotNumber));
        UpdateDisplaySlotRows();
    }

    public void RefreshCastingRequestTimes()
    {
        UpdateCastingRequestRows();
    }

    private void ResolveRoot()
    {
        if (uiRoot != null || !searchSceneWhenRootMissing)
        {
            return;
        }

        Transform namedRoot = FindSceneTransform("HostUI_Canvas") ?? FindSceneTransform("HostScenesUI_Canvas");
        if (namedRoot != null)
        {
            uiRoot = namedRoot as RectTransform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
#endif
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].GetComponent<RectTransform>() != null)
            {
                uiRoot = canvases[i].GetComponent<RectTransform>();
                return;
            }
        }
    }

    private void BindCoreObjects()
    {
        _hostControlRoot = FindObject("Host Control Root");
        _lightInteractionRoot = FindObject("Light Interaction Root");
        _publicScreenPreviewBadge = FindObject("Public Screen Preview Badge");
        _modeToastRoot = FindObject("Mode Toast");
        _castingRequestScrollView = FindObject("Casting Request Scroll View");
        _castingRequestTemplate = FindObject("Casting Request Template");
        _castingRequestContent = FindTransform("Casting Request Content") as RectTransform;

        _timeText = FindText("Device Time");
        _hostText = FindText("Top Host");
        _viewText = FindText("Top View");
        _inspectingText = FindText("Inspecting Text");
        _castingText = FindText("Casting Group Text") ?? FindText("Casting Text");
        _bottomModeText = FindText("Mode Toast Text");
        _castingRequestCountText = FindText("Casting Request Count");
        _castingDot = FindImage("Casting Group Dot");
        _viewportVideoDisplay = FindRawImage("Viewport Video Display");

        if (_castingRequestTemplate != null)
        {
            _castingRequestTemplate.SetActive(false);
        }
    }

    private void BindSegments()
    {
        _hostControlTab = CreateSegmentBinding("Host Control Tab", "Host Control Tab Label", () => SetActiveTab(HostTab.HostControl));
        _lightInteractionTab = CreateSegmentBinding("Light Interaction Tab", "Light Interaction Tab Label", () => SetActiveTab(HostTab.LightInteraction));

        _singleModeSegment = CreateSegmentBinding("Single Mode Segment", "Single Mode Segment Label", () => SetPublicScreenMode(PublicScreenMode.Single));
        _compareModeSegment = CreateSegmentBinding("Compare Mode Segment", "Compare Mode Segment Label", () => SetPublicScreenMode(PublicScreenMode.Compare));

        _freeNavigationSegment = CreateSegmentBinding("Free Segment", "Free Segment Label", () => SetNavigationMode(NavigationMode.Free));
        _orientationNavigationSegment = CreateSegmentBinding("Orientation Segment", "Orientation Segment Label", () => SetNavigationMode(NavigationMode.Orientation));
        _depthNavigationSegment = CreateSegmentBinding("Depth Segment", "Depth Segment Label", () => SetNavigationMode(NavigationMode.Depth));
    }

    private void BindButtons()
    {
        _castPublicViewButtons.Clear();
        _castPublicViewButtons.AddRange(BindNamedButtons("Cast Public View Button", HandleCastPublicView));
        BindNamedButtons("Open Snapshot List Button", HandleOpenSnapshotList);
        BindNamedButtons("Push Public Screen Button", HandlePushPublicScreenToUser);
        BindNamedButtons("Position Calibration Button", HandlePositionCalibration);
        BindNamedButtons("Cast This View To Public Button", HandleCastCurrentViewToPublic);
        BindNamedButtons("Push This View To User Button", HandlePushCurrentViewToUser);

        BindIndexedButtons("View Button", index => HandleUserPerspectiveView(GetUserPerspectiveId(index)));
    }

    private void BindListItems()
    {
        _castingRequestItems.Clear();
        _userPerspectiveItems.Clear();
        _displaySlotItems.Clear();

        _userPerspectiveItems.AddRange(FindTransforms("User Perspective Item"));
        _displaySlotItems.AddRange(FindTransforms("Display Slot Item"));
    }

    private void BindPanelCollapse()
    {
        _collapsiblePanels.Clear();

        if (!enablePanelCollapse)
        {
            return;
        }

        RegisterCollapsiblePanel("Host Console Panel", "Host Console Content", "Host Console Header", "Host Console Collapse");
        RegisterCollapsiblePanel("Public View Control Panel", "Public View Control Content", "Public View Control Header", "Public View Control Collapse");
        RegisterCollapsiblePanel("Light Data Tools Panel", "Light Data Tools Content", "Data Tools Header", "Data Tools Collapse");
    }

    private void RegisterCollapsiblePanel(string panelName, string contentName, string headerName, string collapseTextName)
    {
        Transform panelTransform = FindTransform(panelName);
        Transform contentTransform = FindTransform(contentName);
        Transform headerTransform = FindTransform(headerName);

        if (panelTransform == null || contentTransform == null || headerTransform == null)
        {
            return;
        }

        RectTransform panel = panelTransform as RectTransform;
        RectTransform content = contentTransform as RectTransform;
        if (panel == null || content == null)
        {
            return;
        }

        PanelBinding binding = new PanelBinding
        {
            Name = panelName,
            Panel = panel,
            Content = content,
            Header = headerTransform,
            CollapseText = FindChildText(headerTransform, collapseTextName),
            ExpandedHeight = panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y,
            CollapsedHeight = 88f,
            Collapsed = false
        };

        AddHeaderClickTarget(binding);
        _collapsiblePanels.Add(binding);
    }

    private void AddHeaderClickTarget(PanelBinding binding)
    {
        Image targetGraphic = binding.Header.GetComponent<Image>();
        if (targetGraphic == null)
        {
            targetGraphic = binding.Header.gameObject.AddComponent<Image>();
            targetGraphic.color = new Color(1f, 1f, 1f, 0f);
        }

        targetGraphic.raycastTarget = true;

        Button button = binding.Header.GetComponent<Button>();
        if (button == null)
        {
            button = binding.Header.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = targetGraphic;
        button.transition = Selectable.Transition.None;
        AddListener(button, () => TogglePanel(binding));
    }

    private SegmentBinding CreateSegmentBinding(string objectName, string labelName, UnityAction action)
    {
        Transform target = FindTransform(objectName);
        if (target == null)
        {
            return null;
        }

        Button button = target.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            AddListener(button, action);
        }

        Text label = FindChildText(target, labelName) ?? target.GetComponentInChildren<Text>(true);
        return new SegmentBinding
        {
            Button = button,
            Background = target.GetComponent<Image>(),
            Label = label
        };
    }

    private List<Button> BindNamedButtons(string objectName, UnityAction action)
    {
        List<Button> buttons = FindButtons(objectName);
        for (int i = 0; i < buttons.Count; i++)
        {
            AddListener(buttons[i], action);
        }

        return buttons;
    }

    private void BindIndexedButtons(string objectName, Action<int> action)
    {
        List<Button> buttons = FindButtons(objectName);
        for (int i = 0; i < buttons.Count; i++)
        {
            int capturedIndex = i;
            AddListener(buttons[i], () => action(capturedIndex));
        }
    }

    private void AddListener(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        if (clearRuntimeButtonListenersBeforeBind)
        {
            button.onClick.RemoveAllListeners();
        }

        button.onClick.AddListener(action);
    }

    private void TogglePanel(PanelBinding panel)
    {
        if (panel == null)
        {
            return;
        }

        SetPanelCollapsed(panel, !panel.Collapsed);
    }

    private void SetPanelCollapsed(PanelBinding panel, bool collapsed)
    {
        panel.Collapsed = collapsed;

        if (panel.CollapseText != null)
        {
            panel.CollapseText.text = collapsed ? "v" : "^";
        }

        if (panel.Content != null)
        {
            for (int i = 0; i < panel.Content.childCount; i++)
            {
                Transform child = panel.Content.GetChild(i);
                if (child == panel.Header)
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                child.gameObject.SetActive(!collapsed);
            }
        }

        if (panel.Panel != null)
        {
            Vector2 size = panel.Panel.sizeDelta;
            size.y = collapsed ? panel.CollapsedHeight : panel.ExpandedHeight;
            panel.Panel.sizeDelta = size;
        }

        if (panel.Content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.Content);
        }
    }

    private void ApplyStatus()
    {
        SetText(_timeText, SafeText(_status.timeText));
        SetText(_hostText, "Host: " + SafeText(_status.hostName));
        SetText(_viewText, "View: " + SafeText(_status.viewName));
        SetText(_inspectingText, "Inspecting: " + SafeText(_status.inspectingUserName));
        SetText(_castingText, "Casting: " + SafeText(_status.castingTargetName));

        if (_castingDot != null)
        {
            _castingDot.color = _status.castingActive ? _green : _inactiveDot;
        }
    }

    private void ApplyActiveTab(bool notify)
    {
        bool hostControl = _activeTab == HostTab.HostControl;

        SetActive(_hostControlRoot, hostControl);
        SetActive(_lightInteractionRoot, !hostControl);
        SetActive(_publicScreenPreviewBadge, hostControl);
        SetActive(_modeToastRoot, !hostControl);

        ApplyTabSegment(_hostControlTab, hostControl);
        ApplyTabSegment(_lightInteractionTab, !hostControl);

        if (notify)
        {
            OnHostTabChanged(_activeTab);
            HostTabChanged.Invoke(_activeTab);
        }
    }

    private void ApplyPublicScreenMode(PublicScreenMode mode, bool notify)
    {
        ApplySolidSegment(_singleModeSegment, mode == PublicScreenMode.Single);
        ApplySolidSegment(_compareModeSegment, mode == PublicScreenMode.Compare);

        if (notify)
        {
            OnPublicScreenModeChanged(mode);
            PublicScreenModeChanged.Invoke(mode);
        }
    }

    private void ApplyNavigationMode(NavigationMode mode, bool notify)
    {
        ApplySolidSegment(_freeNavigationSegment, mode == NavigationMode.Free);
        ApplySolidSegment(_orientationNavigationSegment, mode == NavigationMode.Orientation);
        ApplySolidSegment(_depthNavigationSegment, mode == NavigationMode.Depth);

        if (notify)
        {
            OnNavigationModeChanged(mode);
            NavigationModeChanged.Invoke(mode);
        }
    }

    private void ApplyBottomModeHint()
    {
        if (_bottomModeText == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_customBottomMessage))
        {
            _bottomModeText.text = _customBottomMessage;
            return;
        }

        _bottomModeText.text = "Mode: " + GetNavigationModeDisplayName(_navigationMode) + " - " + SafeText(_modeInstruction);
    }

    private void ApplyTabSegment(SegmentBinding segment, bool selected)
    {
        if (segment == null)
        {
            return;
        }

        if (segment.Background != null)
        {
            segment.Background.color = selected ? _selectedPaleBlue : Color.white;
        }

        if (segment.Label != null)
        {
            segment.Label.color = selected ? _primaryBlue : _textMuted;
        }

        ForceStaticButtonState(segment.Button, selected ? _selectedPaleBlue : Color.white);
    }

    private void ApplySolidSegment(SegmentBinding segment, bool selected)
    {
        if (segment == null)
        {
            return;
        }

        if (segment.Background != null)
        {
            segment.Background.color = selected ? _primaryBlue : Color.white;
        }

        if (segment.Label != null)
        {
            segment.Label.color = selected ? Color.white : _textMuted;
        }

        ForceStaticButtonState(segment.Button, selected ? _primaryBlue : Color.white);
    }

    private void ForceStaticButtonState(Button button, Color color)
    {
        if (button == null)
        {
            return;
        }

        button.transition = Selectable.Transition.None;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;
        colors.disabledColor = color;
        button.colors = colors;
    }

    private void UpdateCastingRequestRows()
    {
        if (_castingRequestCountText != null)
        {
            _castingRequestCountText.text = _castingRequests.Count.ToString(CultureInfo.InvariantCulture);
        }

        ClearCastingRequestItemViews();

        if (_castingRequestTemplate == null)
        {
            Debug.LogWarning("[HostUI] Casting Request Template was not found. The request queue cannot render dynamic items.", this);
            SetActive(_castingRequestScrollView, !hideCastingRequestListWhenEmpty && _castingRequests.Count > 0);
            return;
        }

        if (_castingRequestContent == null)
        {
            _castingRequestContent = _castingRequestTemplate.transform.parent as RectTransform;
        }

        bool hasRequests = _castingRequests.Count > 0;
        SetActive(_castingRequestScrollView, hasRequests || !hideCastingRequestListWhenEmpty);

        if (!hasRequests || _castingRequestContent == null)
        {
            return;
        }

        for (int i = 0; i < _castingRequests.Count; i++)
        {
            CastingRequestData data = _castingRequests[i];
            GameObject item = Instantiate(_castingRequestTemplate, _castingRequestContent, false);
            item.name = "Casting Request Item";
            item.SetActive(true);
            _castingRequestItems.Add(item.transform);

            ApplyCastingRequestData(item.transform, data);
            BindCastingRequestItemButtons(item.transform, data.id);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_castingRequestContent);

        ScrollRect scrollRect = _castingRequestScrollView != null ? _castingRequestScrollView.GetComponent<ScrollRect>() : null;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ApplyCastingRequestData(Transform item, CastingRequestData data)
    {
        SetText(FindChildText(item, "Avatar Text"), GetInitial(data.requesterInitial, data.requesterName));
        SetText(FindChildText(item, "Casting Request Message"), SafeText(data.message));
        SetText(FindChildText(item, "Casting Request Time"), FormatRelativeTime(ParseTimeUtc(data.createdAtUtc)));

        Image avatar = FindChildImage(item, "Casting Request Avatar");
        if (avatar != null)
        {
            avatar.color = data.userColor;
        }
    }

    private void BindCastingRequestItemButtons(Transform item, string requestId)
    {
        Button accept = FindChildButton(item, "Accept Button");
        Button reject = FindChildButton(item, "Reject Button");

        AddListener(accept, () => HandleCastingRequestAccepted(requestId));
        AddListener(reject, () => HandleCastingRequestRejected(requestId));
    }

    private void ClearCastingRequestItemViews()
    {
        for (int i = 0; i < _castingRequestItems.Count; i++)
        {
            if (_castingRequestItems[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(_castingRequestItems[i].gameObject);
            }
            else
            {
                DestroyImmediate(_castingRequestItems[i].gameObject);
            }
        }

        _castingRequestItems.Clear();

        if (_castingRequestContent == null)
        {
            return;
        }

        for (int i = _castingRequestContent.childCount - 1; i >= 0; i--)
        {
            Transform child = _castingRequestContent.GetChild(i);
            if (child == null || child.gameObject == _castingRequestTemplate || child.name != "Casting Request Item")
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void UpdateUserPerspectiveRows()
    {
        for (int i = 0; i < _userPerspectiveItems.Count; i++)
        {
            Transform item = _userPerspectiveItems[i];
            bool hasData = i < _userPerspectives.Count;
            SetActive(item != null ? item.gameObject : null, hasData);

            if (!hasData || item == null)
            {
                continue;
            }

            UserPerspectiveData data = _userPerspectives[i];
            SetText(FindChildText(item, "Avatar Text"), GetInitial(data.userInitial, data.userName));
            SetText(FindChildText(item, "User Perspective Name"), SafeText(data.userName));
            SetText(FindChildText(item, "User Perspective Status"), SafeText(data.statusText));

            Image avatar = FindChildImage(item, "User Perspective Avatar");
            if (avatar != null)
            {
                avatar.color = data.userColor;
            }
        }
    }

    private void UpdateDisplaySlotRows()
    {
        for (int i = 0; i < _displaySlotItems.Count; i++)
        {
            Transform item = _displaySlotItems[i];
            bool hasData = i < _displaySlots.Count;
            SetActive(item != null ? item.gameObject : null, hasData);

            if (!hasData || item == null)
            {
                continue;
            }

            DisplaySlotData data = _displaySlots[i];
            Text numberText = FindChildText(item, "Display Slot Number");
            Text labelText = FindChildText(item, "Display Slot Label");
            Image numberImage = FindChildImage(item, "Display Slot Number Background");

            SetText(numberText, Mathf.Max(1, data.slotNumber).ToString(CultureInfo.InvariantCulture));
            SetText(labelText, SafeText(data.label));

            if (numberImage != null)
            {
                numberImage.color = data.selected ? _primaryBlue : Color.white;
            }

            if (numberText != null)
            {
                numberText.color = data.selected ? Color.white : _textDark;
            }

            if (labelText != null)
            {
                labelText.color = data.selected ? _textDark : _textMuted;
            }
        }
    }

    private void SeedDemoDataIfNeeded()
    {
        if (_userPerspectives.Count == 0)
        {
            _userPerspectives.Add(new UserPerspectiveData
            {
                userId = "user-b",
                userName = "User B",
                userInitial = "B",
                statusText = "Observing",
                userColor = _primaryBlue
            });

            _userPerspectives.Add(new UserPerspectiveData
            {
                userId = "user-c",
                userName = "User C",
                userInitial = "C",
                statusText = "Snapshot review",
                userColor = _primaryBlue
            });
        }

        if (_displaySlots.Count == 0)
        {
            _displaySlots.Add(new DisplaySlotData { slotNumber = 1, label = "Global overview", selected = true });
            _displaySlots.Add(new DisplaySlotData { slotNumber = 2, label = "Empty slot" });
            _displaySlots.Add(new DisplaySlotData { slotNumber = 3, label = "Empty slot" });
            _displaySlots.Add(new DisplaySlotData { slotNumber = 4, label = "Empty slot" });
        }
    }

    private string GetCastingRequestId(int index)
    {
        if (index >= 0 && index < _castingRequests.Count && !string.IsNullOrWhiteSpace(_castingRequests[index].id))
        {
            return _castingRequests[index].id;
        }

        return "request-" + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private bool RemoveCastingRequestData(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }

        for (int i = _castingRequests.Count - 1; i >= 0; i--)
        {
            if (_castingRequests[i].id == requestId)
            {
                _castingRequests.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private string GetUserPerspectiveId(int index)
    {
        if (index >= 0 && index < _userPerspectives.Count && !string.IsNullOrWhiteSpace(_userPerspectives[index].userId))
        {
            return _userPerspectives[index].userId;
        }

        return "user-" + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private CastingRequestData CloneCastingRequest(CastingRequestData source)
    {
        return new CastingRequestData
        {
            id = string.IsNullOrWhiteSpace(source.id) ? Guid.NewGuid().ToString("N") : source.id,
            requesterUserId = source.requesterUserId,
            requesterName = source.requesterName,
            requesterInitial = source.requesterInitial,
            message = source.message,
            userColor = source.userColor,
            createdAtUtc = string.IsNullOrWhiteSpace(source.createdAtUtc)
                ? DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                : source.createdAtUtc
        };
    }

    private UserPerspectiveData CloneUserPerspective(UserPerspectiveData source)
    {
        return new UserPerspectiveData
        {
            userId = string.IsNullOrWhiteSpace(source.userId) ? Guid.NewGuid().ToString("N") : source.userId,
            userName = source.userName,
            userInitial = source.userInitial,
            statusText = source.statusText,
            userColor = source.userColor
        };
    }

    private DisplaySlotData CloneDisplaySlot(DisplaySlotData source)
    {
        return new DisplaySlotData
        {
            slotNumber = source.slotNumber,
            label = source.label,
            selected = source.selected
        };
    }

    private void CopyStatus(HostStatusData source, HostStatusData target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.timeText = source.timeText;
        target.hostName = source.hostName;
        target.viewName = source.viewName;
        target.inspectingUserName = source.inspectingUserName;
        target.castingTargetName = source.castingTargetName;
        target.castingActive = source.castingActive;
    }

    private DateTime ParseTimeUtc(string createdAtUtc)
    {
        if (!string.IsNullOrWhiteSpace(createdAtUtc) &&
            DateTime.TryParse(
                createdAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }

    private string FormatRelativeTime(DateTime createdAtUtc)
    {
        TimeSpan elapsed = DateTime.UtcNow - createdAtUtc.ToUniversalTime();

        if (elapsed.TotalSeconds < 60)
        {
            return "now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return Mathf.FloorToInt((float)elapsed.TotalMinutes) + "m ago";
        }

        if (elapsed.TotalHours < 24)
        {
            return Mathf.FloorToInt((float)elapsed.TotalHours) + "h ago";
        }

        return createdAtUtc.ToLocalTime().ToString("MMM d", CultureInfo.InvariantCulture);
    }

    private string GetInitial(string explicitInitial, string name)
    {
        if (!string.IsNullOrWhiteSpace(explicitInitial))
        {
            return explicitInitial.Trim().Substring(0, 1).ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim().Substring(0, 1).ToUpperInvariant();
        }

        return "?";
    }

    private string GetNavigationModeDisplayName(NavigationMode mode)
    {
        switch (mode)
        {
            case NavigationMode.Orientation:
                return "Orientation";
            case NavigationMode.Depth:
                return "Depth";
            default:
                return "Free Navigation";
        }
    }

    private GameObject FindObject(string objectName)
    {
        Transform found = FindTransform(objectName);
        return found != null ? found.gameObject : null;
    }

    private Text FindText(string objectName)
    {
        Transform found = FindTransform(objectName);
        return found != null ? found.GetComponent<Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform found = FindTransform(objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private RawImage FindRawImage(string objectName)
    {
        Transform found = FindTransform(objectName);
        return found != null ? found.GetComponent<RawImage>() : null;
    }

    private Transform FindTransform(string objectName)
    {
        if (uiRoot != null)
        {
            Transform[] transforms = uiRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }
        }

        return searchSceneWhenRootMissing ? FindSceneTransform(objectName) : null;
    }

    private List<Transform> FindTransforms(string objectName)
    {
        List<Transform> results = new List<Transform>();

        if (uiRoot != null)
        {
            Transform[] transforms = uiRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    results.Add(transforms[i]);
                }
            }
        }

        return results;
    }

    private List<Button> FindButtons(string objectName)
    {
        List<Button> buttons = new List<Button>();

        if (uiRoot == null)
        {
            return buttons;
        }

        Button[] allButtons = uiRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null && allButtons[i].name == objectName)
            {
                buttons.Add(allButtons[i]);
            }
        }

        return buttons;
    }

    private Transform FindSceneTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private Text FindChildText(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Text[] texts = parent.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private Image FindChildImage(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Image[] images = parent.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == objectName)
            {
                return images[i];
            }
        }

        return null;
    }

    private Button FindChildButton(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == objectName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void ClearSelectedGameObject()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void PlayButtonFeedback(List<Button> buttons)
    {
        if (!enableButtonFeedback || buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null && isActiveAndEnabled)
            {
                StartCoroutine(FlashButton(buttons[i]));
            }
        }
    }

    private IEnumerator FlashButton(Button button)
    {
        Graphic graphic = button != null ? button.targetGraphic : null;
        RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;

        Color originalColor = graphic != null ? graphic.color : Color.white;
        Vector3 originalScale = rect != null ? rect.localScale : Vector3.one;

        if (graphic != null)
        {
            graphic.color = _primaryBlueDark;
        }

        if (rect != null)
        {
            rect.localScale = originalScale * 0.985f;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.02f, buttonFeedbackSeconds));

        if (graphic != null)
        {
            graphic.color = originalColor;
        }

        if (rect != null)
        {
            rect.localScale = originalScale;
        }
    }

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private void HandleCastPublicView()
    {
        PlayButtonFeedback(_castPublicViewButtons);
        OnCastPublicView();
        CastPublicViewClicked.Invoke();
    }

    private void HandleOpenSnapshotList()
    {
        OnOpenSnapshotList();
        OpenSnapshotListClicked.Invoke();
    }

    private void HandlePushPublicScreenToUser()
    {
        OnPushPublicScreenToUser();
        PushPublicScreenToUserClicked.Invoke();
    }

    private void HandlePositionCalibration()
    {
        OnPositionCalibration();
        PositionCalibrationClicked.Invoke();
    }

    private void HandleCastCurrentViewToPublic()
    {
        OnCastCurrentViewToPublic();
        CastCurrentViewToPublicClicked.Invoke();
    }

    private void HandlePushCurrentViewToUser()
    {
        OnPushCurrentViewToUser();
        PushCurrentViewToUserClicked.Invoke();
    }

    private void HandleCastingRequestAccepted(string requestId)
    {
        OnCastingRequestAccepted(requestId);
        CastingRequestAccepted.Invoke(requestId);
    }

    private void HandleCastingRequestRejected(string requestId)
    {
        OnCastingRequestRejected(requestId);
        CastingRequestRejected.Invoke(requestId);
    }

    private void HandleUserPerspectiveView(string userId)
    {
        OnUserPerspectiveViewClicked(userId);
        UserPerspectiveViewClicked.Invoke(userId);
    }

    public virtual void OnHostTabChanged(HostTab tab)
    {
        Debug.Log("[HostUI] Host tab changed: " + tab);
    }

    public virtual void OnPublicScreenModeChanged(PublicScreenMode mode)
    {
        Debug.Log("[HostUI] Public screen mode changed: " + mode);
    }

    public virtual void OnNavigationModeChanged(NavigationMode mode)
    {
        Debug.Log("[HostUI] Navigation mode changed: " + mode);
    }

    public virtual void OnCastingRequestAccepted(string requestId)
    {
        Debug.Log("[HostUI] Casting request accepted: " + requestId);
    }

    public virtual void OnCastingRequestRejected(string requestId)
    {
        Debug.Log("[HostUI] Casting request rejected: " + requestId);
    }

    public virtual void OnUserPerspectiveViewClicked(string userId)
    {
        Debug.Log("[HostUI] User perspective view clicked: " + userId);
    }

    public virtual void OnCastPublicView()
    {
        Debug.Log("[HostUI] Cast Public View clicked.");
    }

    public virtual void OnOpenSnapshotList()
    {
        Debug.Log("[HostUI] Open Snapshot List clicked.");
    }

    public virtual void OnPushPublicScreenToUser()
    {
        Debug.Log("[HostUI] Push Public Screen to User clicked.");
    }

    public virtual void OnPositionCalibration()
    {
        Debug.Log("[HostUI] Position Calibration clicked.");
    }

    public virtual void OnCastCurrentViewToPublic()
    {
        Debug.Log("[HostUI] Cast This View To Public clicked.");
    }

    public virtual void OnPushCurrentViewToUser()
    {
        Debug.Log("[HostUI] Push This View To User clicked.");
    }
}
