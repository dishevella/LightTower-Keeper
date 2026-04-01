using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class WorldSubtitleView : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] private string line;

    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float fadeIn = 0.4f;
    [SerializeField] private float hold = 2.2f;
    [SerializeField] private float fadeOut = 0.6f;

    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool rotate180Fix = true;

    [SerializeField] private bool spawnAtCameraView = true;
    [SerializeField] private float forwardOffset = 8f;
    [SerializeField] private float horizontalOffset = 0f;

    [SerializeField] private bool lockToSeaLevel = true;
    [SerializeField] private float seaLevelY = 0f;
    [SerializeField] private float extraVerticalOffset = 0f;

    public Action OnFinished;

    private bool played;
    private Coroutine playRoutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        if(subtitleText != null)
        subtitleText.text = line;
        if(canvasGroup != null)
        canvasGroup.alpha = 0f;
    }

    private void LateUpdate()
    {
        if (!faceCamera || targetCamera == null) return;

        FaceToCamera();
    }

   
    public void ResetView()
    {
        played = false;
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void Play()
    {
        if (played) return;
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        played = true;
        if (spawnAtCameraView)
        {
            PlaceAtSeaView();
        }

        if (faceCamera)
        {
            FaceToCamera();
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());

       
    }

    private void PlaceAtSeaView()
    {
        if (targetCamera == null) return;

        Transform cam = targetCamera.transform;

        Vector3 flatForward = cam.forward; //only use the horizontal direction of the cam avoiding player lowers their head
        flatForward.y = 0f;
        flatForward.Normalize();

        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = cam.forward;
        }

        Vector3 flatRight = cam.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 targetPos =
            cam.position
            + flatForward * forwardOffset
            + flatRight * horizontalOffset;

        if (lockToSeaLevel)
        {
            targetPos.y = seaLevelY + extraVerticalOffset;
        }

        transform.position = targetPos;
    }

    private void FaceToCamera()
    {
        if (targetCamera == null) return;

        Vector3 dir = transform.position - targetCamera.transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.forward = dir.normalized;

            if (rotate180Fix)
            {
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }
    private IEnumerator PlayRoutine()
    {
        float time = 0f;

        while (time < fadeIn)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeIn);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(hold);

        time = 0f;

        while (time < fadeOut)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeOut);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        OnFinished?.Invoke();

        playRoutine = null;

        
    }
}