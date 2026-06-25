using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LoreScreenUI : MonoBehaviour
{
    public static LoreScreenUI Instance { get; private set; }

    [Header("Referências UI")]
    [SerializeField] GameObject    lorePanel;
    [SerializeField] CanvasGroup   canvasGroup;
    [SerializeField] Image           background;
    [SerializeField] TextMeshProUGUI nomeBioma;
    [SerializeField] TextMeshProUGUI textoLore;
    [SerializeField] TextMeshProUGUI instrucao;
    [SerializeField] Image           separador;

    bool          _isShowing;
    bool          _waitingForInput;
    System.Action _onComplete;
    Coroutine     _showCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (background != null)
        {
            var c = background.color;
            c.a = 1f;
            background.color = c;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (lorePanel != null) lorePanel.SetActive(true);
    }

    void Update()
    {
        if (!_isShowing) return;
        if (Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            SkipLore();
        }
    }

    void SkipLore()
    {
        if (!_isShowing) return;
        _isShowing       = false;
        _waitingForInput = false;
        if (_showCoroutine != null) { StopCoroutine(_showCoroutine); _showCoroutine = null; }
        DOTween.KillAll();
        if (lorePanel != null) lorePanel.SetActive(false);
        Time.timeScale = 1f;
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

    public IEnumerator ShowLore(string nome, string loreTexto, System.Action onComplete)
    {
        Debug.Log($"[Lore] ShowLore iniciado, lorePanel={lorePanel != null}, active={lorePanel?.activeInHierarchy}");

        _onComplete = onComplete;

        if (Camera.main != null) Camera.main.backgroundColor = Color.black;

        if (background != null)
        {
            var c = background.color;
            c.a = 1f;
            background.color = c;
        }

        if (lorePanel != null) lorePanel.SetActive(true);
        Debug.Log($"[Lore] Após SetActive(true), lorePanel.active={lorePanel?.activeInHierarchy}");

        yield return null;
        Debug.Log($"[Lore] Após yield, active={lorePanel?.activeInHierarchy}, timeScale={Time.timeScale}");

        if (lorePanel == null || !lorePanel.activeInHierarchy)
        {
            Debug.LogWarning("[Lore] lorePanel inativo após yield — early exit, timeScale forçado para 1");
            Time.timeScale = 1f;
            _onComplete = null;
            onComplete?.Invoke();
            yield break;
        }

        _isShowing     = true;
        Time.timeScale = 0f;

        if (nomeBioma != null) nomeBioma.text  = (nome ?? "").ToUpper();
        if (textoLore != null) textoLore.text  = "";
        if (instrucao != null) instrucao.alpha = 0f;
        if (separador != null) separador.color = new Color(0.78f, 0.65f, 0.20f, 0f);

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.8f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.8f);

        if (!_isShowing) yield break;

        if (separador != null)
            separador.DOColor(new Color(0.78f, 0.65f, 0.20f, 1f), 0.5f).SetUpdate(true);

        if (nomeBioma != null)
        {
            Vector3 origPos = nomeBioma.transform.localPosition;
            nomeBioma.transform.localPosition = origPos + Vector3.up * 30f;
            nomeBioma.DOFade(1f, 0.6f).SetUpdate(true);
            nomeBioma.transform.DOLocalMoveY(origPos.y, 0.6f)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }
        yield return new WaitForSecondsRealtime(0.8f);

        if (!_isShowing) yield break;

        if (textoLore != null)
        {
            textoLore.text = loreTexto;
            textoLore.maxVisibleCharacters = 0;
            int   totalChars   = (loreTexto ?? "").Length;
            float typeDuration = totalChars * 0.035f;
            DOTween.To(
                () => textoLore.maxVisibleCharacters,
                x  => textoLore.maxVisibleCharacters = x,
                totalChars, typeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(typeDuration + 0.5f);
        }

        if (!_isShowing) yield break;

        if (instrucao != null)
        {
            instrucao.text = "— toque para continuar —";
            instrucao.DOFade(1f, 0.4f).SetUpdate(true);
            instrucao.DOFade(0.2f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        // aguarda input — Update() também pode chamar SkipLore() antes disso
        _waitingForInput = true;
        while (_waitingForInput && _isShowing)
            yield return null;

        if (!_isShowing) yield break;

        _isShowing = false;
        DOTween.KillAll();
        canvasGroup.DOFade(0f, 0.6f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.6f);

        if (lorePanel != null) lorePanel.SetActive(false);
        Time.timeScale = 1f;
        _onComplete = null;
        onComplete?.Invoke();
    }
}
