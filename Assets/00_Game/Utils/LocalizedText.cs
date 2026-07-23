
using EventDispatcher;
using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizeKey;
    private TextMeshProUGUI text;
    private string dynamicSuffix;
    private bool isInitialized;
    public void Init(string key = null)
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();
        
        if(key != null)
            SetLocalizeKey(key);
        
        if (!isInitialized)
        {
            //this.RegisterListener(EventID.CHANGE_LOCALIZATION, OnLanguageChanged);
            isInitialized = true;
        }
        Refresh();
    }
    
    private void OnDisable()
    {
        if (isInitialized)
        {
            //this.RemoveListener(EventID.CHANGE_LOCALIZATION, OnLanguageChanged);
            isInitialized = false; // Reset flag khi disable
        }
    }

    private void SetLocalizeKey(string key)
    {
        localizeKey = key;
    }
    
    private void OnLanguageChanged(object obj = null)
    {
        Refresh();
    }
    
    private void Refresh()
    {
        text.SetText(SetText(dynamicSuffix));
    }

    public string SetText(string txt = null)
    {
        if (txt != null)
            dynamicSuffix = txt;
        return GameManager.Instance.localizationManager.GetString(localizeKey) + dynamicSuffix;
    }
    
    public void SetKeyOnEditor(string key) => localizeKey = key;
    
    
}