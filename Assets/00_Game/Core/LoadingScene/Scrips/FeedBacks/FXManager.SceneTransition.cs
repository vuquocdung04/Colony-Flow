using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FXManager
{
    [SerializeField] private List<SceneTransition> transitions;
    [SerializeField] private SceneTransitionType defaultTransition = SceneTransitionType.Iris;
    public float transitionDurationOut = 1f;
    public float transitionDurationIn = 1f;
    [HideInInspector] public bool isNextSceneReady;

    private SceneTransition current;

    public void LoadSceneWithIrisWipe(string sceneName, bool skipOutPhase = false)
        => LoadScene(sceneName, defaultTransition, skipOutPhase);

    public void LoadScene(string sceneName, SceneTransitionType type, bool skipOutPhase = false)
    {
        current = Resolve(type);
        TransitionAsync(sceneName, skipOutPhase).Forget();
    }

    private async Awaitable TransitionAsync(string sceneName, bool skipOutPhase)
    {
        isNextSceneReady = false;

        current.Prepare();

        if (skipOutPhase)
            current.SetCovered();
        else
            await current.Cover(transitionDurationOut);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            await Awaitable.NextFrameAsync();

        current.SetCovered();

        await AwaitableEx.WaitUntil(() => isNextSceneReady);

        await current.Reveal(transitionDurationIn);

        current.Hide();
    }

    public void PrepareWipeClosed()
    {
        current = Resolve(defaultTransition);
        current.Prepare();
        current.SetCovered();
    }

    private void HideAllTransitions()
    {
        foreach (SceneTransition t in transitions) t.Hide();
    }

    private SceneTransition Resolve(SceneTransitionType type)
    {
        foreach (SceneTransition t in transitions)
            if (t.Type == type) return t;
        return transitions[0];
    }
}
