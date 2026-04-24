using UnityEngine;
using UnityEngine.UI;

public class TidbitSettingsToggle : MonoBehaviour
{
    [SerializeField] private SettingsData settingsData;
    [SerializeField] private Toggle informationalTidbitsToggle;

    private void Awake()
    {
        if (informationalTidbitsToggle == null)
        {
            informationalTidbitsToggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (informationalTidbitsToggle == null)
        {
            Debug.LogWarning("TidbitSettingsToggle: informationalTidbitsToggle is not assigned.", this);
            return;
        }

        bool isEnabled = SettingsData.GetInformationalTidbitsEnabled(
            settingsData == null || settingsData.informationalTidbitsEnabled
        );
        informationalTidbitsToggle.SetIsOnWithoutNotify(isEnabled);
    }

    private void Start()
    {
        if (settingsData != null)
        {
            settingsData.Load();
        }

        if (informationalTidbitsToggle == null)
        {
            Debug.LogWarning("TidbitSettingsToggle: informationalTidbitsToggle is not assigned.", this);
            return;
        }

        informationalTidbitsToggle.onValueChanged.RemoveListener(SetInformationalTidbitsEnabled);
        informationalTidbitsToggle.onValueChanged.AddListener(SetInformationalTidbitsEnabled);
    }

    private void OnDestroy()
    {
        if (informationalTidbitsToggle != null)
        {
            informationalTidbitsToggle.onValueChanged.RemoveListener(SetInformationalTidbitsEnabled);
        }
    }

    public void SetInformationalTidbitsEnabled(bool isEnabled)
    {
        SettingsData.SetInformationalTidbitsEnabled(isEnabled);

        if (settingsData != null)
        {
            settingsData.informationalTidbitsEnabled = isEnabled;
            settingsData.Save();
        }
    }
}
