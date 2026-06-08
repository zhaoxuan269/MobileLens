// Author: Lingying Zhang
// User-facing UGUI controller for the editable MobileLens user interface scene.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserUI : MonoBehaviour
{
    public enum NavigationMode
    {
        Free,
        Orientation,
        Depth
    }

    [Serializable]
    public class NavigationModeEvent : UnityEvent<NavigationMode>
    {
    }

    [Serializable]
    public class StatusData
    {
        public string timeText = "9:41 AM   Tue May 14";
        public string userName = "A";
        public string viewName = "My View";
        public string sharingUserName = "User B";
        public bool sharingActive = true;
        public bool castingActive = true;
    }

    [Serializable]
    public class NotificationData
    {
        public string id;
        public string senderName;
        public string senderInitial;
        public string message;
        public Color userColor = new Color32(96, 122, 153, 255);
        public string createdAtUtc;
    }

    [Header("Initial State")]
    [SerializeField] private NavigationMode defaultNavigationMode = NavigationMode.Free;
    [SerializeField] private string defaultModeInstruction = "Move the iPad to adjust viewpoint";
    [SerializeField] private StatusData initialStatus = new StatusData();

    [Header("Notifications")]
    [SerializeField] private int maxNotifications = 3;
    [SerializeField] private float notificationTimeRefreshSeconds = 30f;

    [Header("Business Events")]
    public UnityEvent ShareMyViewClicked = new UnityEvent();
    public UnityEvent RequestViewClicked = new UnityEvent();
    public UnityEvent PositionCalibrationClicked = new UnityEvent();
    public NavigationModeEvent NavigationModeChanged = new NavigationModeEvent();
    public UnityEvent TransferFunctionClicked = new UnityEvent();
    public UnityEvent SelectRoiClicked = new UnityEvent();
    public UnityEvent HighlightClicked = new UnityEvent();
    public UnityEvent ClearRoiClicked = new UnityEvent();
    public UnityEvent AddAnnotationClicked = new UnityEvent();
    public UnityEvent SaveSnapshotClicked = new UnityEvent();
    public UnityEvent RestoreSnapshotClicked = new UnityEvent();

    private readonly Color _primaryBlue = new Color32(49, 91, 234, 255);
    private readonly Color _textDark = new Color32(31, 39, 54, 255);
    private readonly Color _textMuted = new Color32(91, 101, 118, 255);
    private readonly Color _green = new Color32(96, 174, 153, 255);
    private readonly Color _purple = new Color32(135, 92, 232, 255);
    private readonly Color _inactiveDot = new Color32(178, 187, 201, 255);
    private readonly Color _fallbackUserColor = new Color32(96, 122, 153, 255);

    private StatusData _status = new StatusData();
    private NavigationMode _currentNavigationMode;
    private string _modeInstruction;
    private string _customBottomMessage;
    private bool _isBound;

    private Text _timeText;
    private Text _userText;
    private Text _viewText;
    private Text _sharingText;
    private Text _castingText;
    private Text _bottomModeText;

    private Image _sharingDot;
    private Image _castingDot;

    private Button _shareMyViewButton;
    private Button _requestViewButton;
    private Button _positionCalibrationButton;
    private Button _transferFunctionButton;
    private Button _selectRoiButton;
    private Button _highlightButton;
    private Button _clearRoiButton;
    private Button _addAnnotationButton;
    private Button _saveSnapshotButton;
    private Button _restoreSnapshotButton;

    private SegmentBinding _freeSegment;
    private SegmentBinding _orientationSegment;
    private SegmentBinding _depthSegment;

    private readonly List<NotificationData> _notifications = new List<NotificationData>();
    private readonly List<NotificationView> _notificationViews = new List<NotificationView>();
    private RectTransform _notificationTemplate;
    private Transform _notificationParent;
    private int _notificationInsertIndex;

    private class SegmentBinding
    {
        public Button Button;
        public Image Background;
        public Text Label;
    }

    private class NotificationView
    {
        public RectTransform Root;
        public Image Dot;
        public Image ItemBackground;
        public Outline ItemOutline;
        public Image AvatarBackground;
        public Text AvatarText;
        public Text MessageText;
        public Text TimeText;
        public NotificationData Data;
    }

    private void Awake()
    {
        _currentNavigationMode = defaultNavigationMode;
        _modeInstruction = defaultModeInstruction;
        CopyStatus(initialStatus, _status);

        BindSceneReferences();
        RegisterButtonListeners();
        ApplyStatus();
        ApplyNavigationMode(_currentNavigationMode);
        ApplyBottomModeHint();

        if (notificationTimeRefreshSeconds > 0f)
        {
            InvokeRepeating(nameof(RefreshNotificationTimes), notificationTimeRefreshSeconds, notificationTimeRefreshSeconds);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(RefreshNotificationTimes));
        UnregisterButtonListeners();
    }

    [ContextMenu("Rebind Scene References")]
    public void RebindSceneReferences()
    {
        UnregisterButtonListeners();
        BindSceneReferences();
        RegisterButtonListeners();
        ApplyStatus();
        ApplyNavigationMode(_currentNavigationMode);
        ApplyBottomModeHint();
    }

    public void SetNotifications(IEnumerable<NotificationData> notifications)
    {
        _notifications.Clear();

        if (notifications != null)
        {
            foreach (NotificationData notification in notifications)
            {
                if (notification == null)
                {
                    continue;
                }

                _notifications.Add(CloneNotification(notification));
            }
        }

        SortAndTrimNotifications();
        RebuildNotificationViews();
    }

    public void AddNotification(NotificationData notification)
    {
        if (notification == null)
        {
            return;
        }

        NotificationData clone = CloneNotification(notification);
        RemoveNotificationData(clone.id);
        _notifications.Insert(0, clone);
        SortAndTrimNotifications();
        RebuildNotificationViews();
    }

    public void AddNotificationFromBackend(
        string id,
        string senderName,
        string senderInitial,
        string message,
        string userColorHex,
        string createdAtUtc)
    {
        AddNotification(new NotificationData
        {
            id = id,
            senderName = senderName,
            senderInitial = senderInitial,
            message = message,
            userColor = ParseBackendColor(userColorHex),
            createdAtUtc = createdAtUtc
        });
    }

    public void RemoveNotification(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (RemoveNotificationData(id))
        {
            RebuildNotificationViews();
        }
    }

    public void ClearNotifications()
    {
        _notifications.Clear();
        RebuildNotificationViews();
    }

    public void RefreshNotificationTimes()
    {
        for (int i = 0; i < _notificationViews.Count; i++)
        {
            NotificationView view = _notificationViews[i];
            if (view != null && view.TimeText != null && view.Data != null)
            {
                view.TimeText.text = FormatRelativeTime(ParseNotificationTimeUtc(view.Data.createdAtUtc));
            }
        }
    }

    public void SetStatus(StatusData status)
    {
        if (status == null)
        {
            return;
        }

        CopyStatus(status, _status);
        ApplyStatus();
    }

    public void SetTopStatus(
        string userName,
        string viewName,
        string sharingUserName,
        bool sharingActive,
        bool castingActive)
    {
        _status.userName = userName;
        _status.viewName = viewName;
        _status.sharingUserName = sharingUserName;
        _status.sharingActive = sharingActive;
        _status.castingActive = castingActive;
        ApplyStatus();
    }

    public void SetSystemStatus(string timeText)
    {
        _status.timeText = timeText;
        ApplyStatus();
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

    public void SetNavigationMode(NavigationMode mode)
    {
        _currentNavigationMode = mode;
        ApplyNavigationMode(mode);
        ApplyBottomModeHint();
        OnNavigationModeChanged(mode);
        NavigationModeChanged.Invoke(mode);
    }

    private void BindSceneReferences()
    {
        _timeText = FindChildText("Device Time");
        _userText = FindChildText("Top User");
        _viewText = FindChildText("Top View");
        _sharingText = FindChildText("Sharing Group Text");
        _castingText = FindChildText("Casting Group Text");
        _bottomModeText = FindChildText("Toast Text");

        _sharingDot = FindChildImage("Sharing Group Dot");
        _castingDot = FindChildImage("Casting Group Dot");

        _shareMyViewButton = FindChildButton("Share My View Button");
        _requestViewButton = FindChildButton("Request View Button");
        _positionCalibrationButton = FindChildButton("Position Calibration Button");
        _transferFunctionButton = FindChildButton("Transfer Function Button");
        _selectRoiButton = FindChildButton("Select ROI Button");
        _highlightButton = FindChildButton("Highlight Button");
        _clearRoiButton = FindChildButton("Clear ROI Button");
        _addAnnotationButton = FindChildButton("Add Annotation Button");
        _saveSnapshotButton = FindChildButton("Save Button");
        _restoreSnapshotButton = FindChildButton("Restore Button");

        _freeSegment = BindSegment("Free Segment", "Free Segment Label");
        _orientationSegment = BindSegment("Orientation Segment", "Orientation Segment Label");
        _depthSegment = BindSegment("Depth Segment", "Depth Segment Label");
        BindNotificationTemplate();

        _isBound = true;
    }

    private void BindNotificationTemplate()
    {
        Transform collaborationBody = FindNamedTransformInScene("Collaboration Body");
        if (collaborationBody == null)
        {
            Debug.LogWarning("[UserUI] Missing scene object: Collaboration Body", this);
            return;
        }

        List<Transform> rows = new List<Transform>();
        FindNamedTransforms(collaborationBody, "Notification Row", rows);

        if (rows.Count == 0)
        {
            Debug.LogWarning("[UserUI] Missing Notification Row template under Collaboration Body.", collaborationBody);
            return;
        }

        _notificationTemplate = rows[0].GetComponent<RectTransform>();
        _notificationParent = _notificationTemplate.parent;
        _notificationInsertIndex = _notificationTemplate.GetSiblingIndex();

        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].gameObject.SetActive(false);
        }

        RebuildNotificationViews();
    }

    private SegmentBinding BindSegment(string buttonName, string labelName)
    {
        Button button = FindChildButton(buttonName);
        Image background = FindChildImage(buttonName);
        Text label = FindChildText(labelName);

        if (button == null && background == null && label == null)
        {
            return null;
        }

        return new SegmentBinding
        {
            Button = button,
            Background = background,
            Label = label
        };
    }

    public Text FindChildText(string objectName)
    {
        return FindSceneComponent<Text>(objectName);
    }

    public Button FindChildButton(string objectName)
    {
        return FindSceneComponent<Button>(objectName);
    }

    public Image FindChildImage(string objectName)
    {
        return FindSceneComponent<Image>(objectName);
    }

    private T FindSceneComponent<T>(string objectName) where T : Component
    {
        Transform found = FindNamedTransform(transform, objectName);

        if (found == null)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length && found == null; i++)
            {
                found = FindNamedTransform(roots[i].transform, objectName);
            }
        }

        if (found == null)
        {
            Debug.LogWarning($"[UserUI] Missing scene object: {objectName}", this);
            return null;
        }

        T component = found.GetComponent<T>();
        if (component == null)
        {
            component = found.GetComponentInChildren<T>(true);
        }

        if (component == null)
        {
            Debug.LogWarning($"[UserUI] Scene object '{objectName}' is missing component {typeof(T).Name}.", found);
        }

        return component;
    }

    private Transform FindNamedTransform(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                return children[i];
            }
        }

        return null;
    }

    private Transform FindNamedTransformInScene(string objectName)
    {
        Transform found = FindNamedTransform(transform, objectName);
        if (found != null)
        {
            return found;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            found = FindNamedTransform(roots[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void FindNamedTransforms(Transform root, string objectName, List<Transform> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                results.Add(children[i]);
            }
        }
    }

    private void RegisterButtonListeners()
    {
        RegisterButton(_shareMyViewButton, HandleShareMyView);
        RegisterButton(_requestViewButton, HandleRequestView);
        RegisterButton(_positionCalibrationButton, HandlePositionCalibration);
        RegisterButton(_transferFunctionButton, HandleTransferFunction);
        RegisterButton(_selectRoiButton, HandleSelectRoi);
        RegisterButton(_highlightButton, HandleHighlight);
        RegisterButton(_clearRoiButton, HandleClearRoi);
        RegisterButton(_addAnnotationButton, HandleAddAnnotation);
        RegisterButton(_saveSnapshotButton, HandleSaveSnapshot);
        RegisterButton(_restoreSnapshotButton, HandleRestoreSnapshot);
        RegisterButton(_freeSegment?.Button, HandleFreeNavigation);
        RegisterButton(_orientationSegment?.Button, HandleOrientationNavigation);
        RegisterButton(_depthSegment?.Button, HandleDepthNavigation);
    }

    private void UnregisterButtonListeners()
    {
        UnregisterButton(_shareMyViewButton, HandleShareMyView);
        UnregisterButton(_requestViewButton, HandleRequestView);
        UnregisterButton(_positionCalibrationButton, HandlePositionCalibration);
        UnregisterButton(_transferFunctionButton, HandleTransferFunction);
        UnregisterButton(_selectRoiButton, HandleSelectRoi);
        UnregisterButton(_highlightButton, HandleHighlight);
        UnregisterButton(_clearRoiButton, HandleClearRoi);
        UnregisterButton(_addAnnotationButton, HandleAddAnnotation);
        UnregisterButton(_saveSnapshotButton, HandleSaveSnapshot);
        UnregisterButton(_restoreSnapshotButton, HandleRestoreSnapshot);
        UnregisterButton(_freeSegment?.Button, HandleFreeNavigation);
        UnregisterButton(_orientationSegment?.Button, HandleOrientationNavigation);
        UnregisterButton(_depthSegment?.Button, HandleDepthNavigation);
    }

    private void RegisterButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UnregisterButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
    }

    private void ApplyStatus()
    {
        SetText(_timeText, SafeText(_status.timeText));
        SetText(_userText, "User: " + SafeText(_status.userName));
        SetText(_viewText, "View: " + SafeText(_status.viewName));
        SetText(_sharingText, "Sharing: " + SafeText(_status.sharingUserName));
        SetText(_castingText, "Casting");
        if (_sharingDot != null)
        {
            _sharingDot.color = _status.sharingActive ? _green : _inactiveDot;
        }

        if (_castingDot != null)
        {
            _castingDot.color = _status.castingActive ? _purple : _inactiveDot;
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

        _bottomModeText.text = "Mode: " + GetNavigationModeDisplayName(_currentNavigationMode) + " - " + SafeText(_modeInstruction);
    }

    private void ApplyNavigationMode(NavigationMode mode)
    {
        ApplySegment(_freeSegment, mode == NavigationMode.Free);
        ApplySegment(_orientationSegment, mode == NavigationMode.Orientation);
        ApplySegment(_depthSegment, mode == NavigationMode.Depth);
    }

    private void RebuildNotificationViews()
    {
        ClearNotificationViews();

        if (_notificationTemplate == null || _notificationParent == null)
        {
            return;
        }

        int count = Mathf.Min(Mathf.Max(maxNotifications, 0), _notifications.Count);
        for (int i = 0; i < count; i++)
        {
            NotificationView view = CreateNotificationView(_notifications[i], i);
            if (view != null)
            {
                _notificationViews.Add(view);
            }
        }
    }

    private NotificationView CreateNotificationView(NotificationData data, int index)
    {
        RectTransform root = Instantiate(_notificationTemplate, _notificationParent);
        root.name = "Notification Row Generated";
        root.SetSiblingIndex(_notificationInsertIndex + index);
        root.gameObject.SetActive(true);

        NotificationView view = new NotificationView
        {
            Root = root,
            Dot = GetChildImage(root, "Notification Dot"),
            ItemBackground = GetChildImage(root, "Notification Item"),
            AvatarBackground = GetChildImage(root, "Notification Avatar"),
            AvatarText = GetChildText(root, "Avatar Text"),
            MessageText = GetChildText(root, "Notification Message"),
            TimeText = GetChildText(root, "Notification Time"),
            Data = data
        };

        if (view.ItemBackground != null)
        {
            view.ItemOutline = view.ItemBackground.GetComponent<Outline>();
            if (view.ItemOutline == null)
            {
                view.ItemOutline = view.ItemBackground.gameObject.AddComponent<Outline>();
                view.ItemOutline.effectDistance = new Vector2(1f, -1f);
                view.ItemOutline.useGraphicAlpha = true;
            }
        }

        ApplyNotificationView(view);
        return view;
    }

    private void ApplyNotificationView(NotificationView view)
    {
        if (view == null || view.Data == null)
        {
            return;
        }

        Color userColor = view.Data.userColor;
        string initial = string.IsNullOrWhiteSpace(view.Data.senderInitial)
            ? GetInitialFromName(view.Data.senderName)
            : view.Data.senderInitial;

        if (view.Dot != null)
        {
            view.Dot.color = userColor;
        }

        if (view.AvatarBackground != null)
        {
            view.AvatarBackground.color = userColor;
        }

        if (view.ItemBackground != null)
        {
            view.ItemBackground.color = DeriveNotificationBackground(userColor);
        }

        if (view.ItemOutline != null)
        {
            view.ItemOutline.effectColor = DeriveNotificationBorder(userColor);
        }

        SetText(view.AvatarText, initial);
        SetText(view.MessageText, SafeText(view.Data.message));

        if (view.TimeText != null)
        {
            view.TimeText.text = FormatRelativeTime(ParseNotificationTimeUtc(view.Data.createdAtUtc));
        }
    }

    private void ClearNotificationViews()
    {
        for (int i = 0; i < _notificationViews.Count; i++)
        {
            NotificationView view = _notificationViews[i];
            if (view == null || view.Root == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(view.Root.gameObject);
            }
            else
            {
                DestroyImmediate(view.Root.gameObject);
            }
        }

        _notificationViews.Clear();
    }

    private Text GetChildText(Transform root, string objectName)
    {
        Transform child = FindNamedTransform(root, objectName);
        return child == null ? null : child.GetComponent<Text>();
    }

    private Image GetChildImage(Transform root, string objectName)
    {
        Transform child = FindNamedTransform(root, objectName);
        if (child == null)
        {
            return null;
        }

        Image image = child.GetComponent<Image>();
        if (image == null)
        {
            image = child.GetComponentInChildren<Image>(true);
        }

        return image;
    }

    private NotificationData CloneNotification(NotificationData source)
    {
        string id = string.IsNullOrWhiteSpace(source.id)
            ? Guid.NewGuid().ToString("N")
            : source.id;

        return new NotificationData
        {
            id = id,
            senderName = source.senderName,
            senderInitial = source.senderInitial,
            message = source.message,
            userColor = source.userColor,
            createdAtUtc = string.IsNullOrWhiteSpace(source.createdAtUtc)
                ? DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                : source.createdAtUtc
        };
    }

    private bool RemoveNotificationData(string id)
    {
        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            if (_notifications[i].id == id)
            {
                _notifications.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private void SortAndTrimNotifications()
    {
        _notifications.Sort((left, right) =>
            ParseNotificationTimeUtc(right.createdAtUtc).CompareTo(ParseNotificationTimeUtc(left.createdAtUtc)));

        int limit = Mathf.Max(maxNotifications, 0);
        while (_notifications.Count > limit)
        {
            _notifications.RemoveAt(_notifications.Count - 1);
        }
    }

    private Color ParseBackendColor(string userColorHex)
    {
        if (!string.IsNullOrWhiteSpace(userColorHex) &&
            ColorUtility.TryParseHtmlString(userColorHex, out Color parsed))
        {
            return parsed;
        }

        Debug.LogWarning($"[UserUI] Invalid notification color '{userColorHex}'. Expected #RRGGBB or #RRGGBBAA.", this);
        return _fallbackUserColor;
    }

    private DateTime ParseNotificationTimeUtc(string createdAtUtc)
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

    private Color DeriveNotificationBackground(Color userColor)
    {
        Color background = Color.Lerp(Color.white, userColor, 0.10f);
        background.a = 1f;
        return background;
    }

    private Color DeriveNotificationBorder(Color userColor)
    {
        Color border = Color.Lerp(Color.white, userColor, 0.32f);
        border.a = 1f;
        return border;
    }

    private string GetInitialFromName(string senderName)
    {
        if (string.IsNullOrWhiteSpace(senderName))
        {
            return "?";
        }

        return senderName.Trim()[0].ToString().ToUpperInvariant();
    }

    private void ApplySegment(SegmentBinding segment, bool selected)
    {
        if (segment == null)
        {
            return;
        }

        Color background = selected ? _primaryBlue : Color.white;
        Color text = selected ? Color.white : _textMuted;

        if (segment.Background != null)
        {
            segment.Background.color = background;
        }

        if (segment.Label != null)
        {
            segment.Label.color = text;
        }

        if (segment.Button != null)
        {
            ColorBlock colors = segment.Button.colors;
            colors.normalColor = background;
            colors.highlightedColor = selected ? _primaryBlue : new Color32(244, 247, 252, 255);
            colors.pressedColor = selected ? new Color32(38, 76, 205, 255) : new Color32(230, 235, 243, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(198, 205, 216, 255);
            colors.colorMultiplier = 1f;
            segment.Button.colors = colors;
        }
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void CopyStatus(StatusData source, StatusData target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.timeText = source.timeText;
        target.userName = source.userName;
        target.viewName = source.viewName;
        target.sharingUserName = source.sharingUserName;
        target.sharingActive = source.sharingActive;
        target.castingActive = source.castingActive;
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

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private void HandleFreeNavigation()
    {
        SetNavigationMode(NavigationMode.Free);
    }

    private void HandleOrientationNavigation()
    {
        SetNavigationMode(NavigationMode.Orientation);
    }

    private void HandleDepthNavigation()
    {
        SetNavigationMode(NavigationMode.Depth);
    }

    private void HandleShareMyView()
    {
        OnShareMyView();
        ShareMyViewClicked.Invoke();
    }

    private void HandleRequestView()
    {
        OnRequestView();
        RequestViewClicked.Invoke();
    }

    private void HandlePositionCalibration()
    {
        OnPositionCalibration();
        PositionCalibrationClicked.Invoke();
    }

    private void HandleTransferFunction()
    {
        OnTransferFunction();
        TransferFunctionClicked.Invoke();
    }

    private void HandleSelectRoi()
    {
        OnSelectRoi();
        SelectRoiClicked.Invoke();
    }

    private void HandleHighlight()
    {
        OnHighlight();
        HighlightClicked.Invoke();
    }

    private void HandleClearRoi()
    {
        OnClearRoi();
        ClearRoiClicked.Invoke();
    }

    private void HandleAddAnnotation()
    {
        OnAddAnnotation();
        AddAnnotationClicked.Invoke();
    }

    private void HandleSaveSnapshot()
    {
        OnSaveSnapshot();
        SaveSnapshotClicked.Invoke();
    }

    private void HandleRestoreSnapshot()
    {
        OnRestoreSnapshot();
        RestoreSnapshotClicked.Invoke();
    }

    public virtual void OnShareMyView()
    {
        Debug.Log("[UserUI] Share My View clicked.");
    }

    public virtual void OnRequestView()
    {
        Debug.Log("[UserUI] Request View clicked.");
    }

    public virtual void OnPositionCalibration()
    {
        Debug.Log("[UserUI] Position Calibration clicked.");
    }

    public virtual void OnNavigationModeChanged(NavigationMode mode)
    {
        Debug.Log("[UserUI] Navigation mode changed: " + mode);
    }

    public virtual void OnTransferFunction()
    {
        Debug.Log("[UserUI] Transfer Function clicked.");
    }

    public virtual void OnSelectRoi()
    {
        Debug.Log("[UserUI] Select ROI clicked.");
    }

    public virtual void OnHighlight()
    {
        Debug.Log("[UserUI] Highlight clicked.");
    }

    public virtual void OnClearRoi()
    {
        Debug.Log("[UserUI] Clear ROI clicked.");
    }

    public virtual void OnAddAnnotation()
    {
        Debug.Log("[UserUI] Add Annotation clicked.");
    }

    public virtual void OnSaveSnapshot()
    {
        Debug.Log("[UserUI] Save Snapshot clicked.");
    }

    public virtual void OnRestoreSnapshot()
    {
        Debug.Log("[UserUI] Restore Snapshot clicked.");
    }
}
