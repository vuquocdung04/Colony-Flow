using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameIntro : MonoBehaviour
{
    public static GameIntro Instance { get; private set; }

    public void InitInstance() => Instance = this;

    [SerializeField] private GameIntroConfig config;
    [SerializeField] private List<MonoBehaviour> introSteps;
    [SerializeField] private bool playIntro = true;

    public void Init()
    {
        if (!playIntro) return;

        foreach (var step in introSteps)
            ((IIntroStep)step).Prepare(config);
    }

    public async Awaitable PlayIntro()
    {
        if (!playIntro) return;

        foreach (var step in introSteps)
            await ((IIntroStep)step).Play(config);
    }

    [Button("Test All Intro")]
    private void TestAllIntro()
    {
        Init();
        PlayIntro().Forget();
    }

    [Button("Auto Collect Intro Steps")]
    private void AutoCollectIntroSteps()
    {
        introSteps = new List<MonoBehaviour>();
        foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mono is IIntroStep)
                introSteps.Add(mono);
    }
}
