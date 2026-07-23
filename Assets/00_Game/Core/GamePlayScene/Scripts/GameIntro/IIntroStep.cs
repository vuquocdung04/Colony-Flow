using UnityEngine;

public interface IIntroStep
{
    void Prepare(GameIntroConfig config);
    Awaitable Play(GameIntroConfig config);
}
