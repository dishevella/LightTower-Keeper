using UnityEngine;
using System.Collections;
public class ForestPathIntroController : ControllerAbstract
{
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private ScreenSubtitlePanel screenSubtitlePanel;
    [SerializeField] private WorldSubtitleView forestSubtitleView;
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    [TextArea(2, 4)]
    [SerializeField] private string blackScreenLine = "The Wind feels different here.";
    private void OnEnable()
    {

        App.Events.Subscribe<ForestPathStartedEvent>(OnForestStarted);
    }
    private void OnDisable()
    {
        App.Events.Unsubscribe<ForestPathStartedEvent>(OnForestStarted);
    }
    private void Awake()
    {
        SetPlayerControlLocked(true);
    }
    private void OnForestStarted(ForestPathStartedEvent evt)
    {
        StartCoroutine(PlayForestPathIntro());
    }
    private IEnumerator PlayForestPathIntro()
    {
        SetPlayerControlLocked(true);
        yield return screenSubtitlePanel.PlayLine(
            blackScreenLine,
            0.4f,
            1.8f,
            0.5f
            );
        yield return fadePanel.FadeFromBlack(1.5f);
        yield return new WaitForSeconds(0.5f);
        if(forestSubtitleView != null)
        {
            forestSubtitleView.Play();
        }
        yield return new WaitForSeconds(0.3f);
        SetPlayerControlLocked(false);
    }
    private void SetPlayerControlLocked(bool locked)
    {
        if (playerControlComponents == null) return;
        foreach (var comp in playerControlComponents)
        {
            if(comp != null)
            {
                comp.enabled =! locked;
            }
        }
    }
}

