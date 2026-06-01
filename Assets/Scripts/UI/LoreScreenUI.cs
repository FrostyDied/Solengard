using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LoreScreenUI : MonoBehaviour
{
    public static LoreScreenUI Instance { get; private set; }

    [Header("Referências UI")]
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] Image           background;
    [SerializeField] TextMeshProUGUI nomeBioma;
    [SerializeField] TextMeshProUGUI textoLore;
    [SerializeField] TextMeshProUGUI instrucao;
    [SerializeField] Image           separador;

    bool _waitingForInput;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public IEnumerator ShowLore(BiomeSystem.BiomeConfig config, System.Action onComplete)
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        if (nomeBioma != null) nomeBioma.text  = config.nome.ToUpper();
        if (textoLore != null) textoLore.text  = "";
        if (instrucao != null) instrucao.alpha = 0f;
        if (separador != null) separador.color = new Color(0.78f, 0.65f, 0.20f, 0f);

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.8f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.8f);

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

        if (textoLore != null)
        {
            textoLore.text = config.loreTexto;
            textoLore.maxVisibleCharacters = 0;
            int   totalChars   = config.loreTexto.Length;
            float typeDuration = totalChars * 0.035f;
            DOTween.To(
                () => textoLore.maxVisibleCharacters,
                x  => textoLore.maxVisibleCharacters = x,
                totalChars, typeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(typeDuration + 0.5f);
        }

        if (instrucao != null)
        {
            instrucao.text = "— toque para continuar —";
            instrucao.DOFade(1f, 0.4f).SetUpdate(true);
            instrucao.DOFade(0.2f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        _waitingForInput = true;
        while (_waitingForInput)
        {
            if (Input.anyKeyDown ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                _waitingForInput = false;
            yield return null;
        }

        DOTween.KillAll();
        canvasGroup.DOFade(0f, 0.6f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.6f);

        Time.timeScale = 1f;
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
