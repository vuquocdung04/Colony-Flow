using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;

public enum Language
{
    En = 5,
    Vi = 10,
}

public class LocalizationManager : MonoBehaviour
{
    private Dictionary<Language, Dictionary<string, string>> localizationDictionary = new()
    {
        { Language.En, new Dictionary<string, string>() },
        { Language.Vi, new Dictionary<string, string>() },
    };

    private Language currentLanguage;
    public void Init(DataRepo dataRepo)
    {
        var localizationData = dataRepo.localizationDataBase;
        
        var dataTable = localizationData.entries;
        foreach (var data in dataTable)
        {
            localizationDictionary[Language.En].TryAdd(data.key, data.EN);
            localizationDictionary[Language.Vi].TryAdd(data.key, data.VI);
        }

        ChangeLanguage(currentLanguage);
    }

    public void ChangeLanguage(Language language)
    {
        if (!localizationDictionary.ContainsKey(language))
        {
            Debug.LogError($"Cannot change to language: {language}");
            return;
        }

        currentLanguage = language;
        //this.PostEvent(EventID.CHANGE_LOCALIZATION);
    }

    public string GetString(string key)
    {
        if (localizationDictionary[currentLanguage].TryGetValue(key, out string localizedString))
        {
            return localizedString;
        }
        Debug.LogError($"Missing Localize Key for: {key}");
        return "Missing Localization";
    }

    public string GetString(string key, Language targetLanguage)
    {
        if (localizationDictionary[targetLanguage].TryGetValue(key, out string localizedString))
        {
            return localizedString;
        }

        Debug.LogError($"Missing Localize Key for: {key}");
        return "Missing Localization";
    }
}