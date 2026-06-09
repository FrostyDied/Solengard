using UnityEngine;
using UnityEngine.UI;

public class CoverPanel : MonoBehaviour
{
    public static CoverPanel Instance { get; private set; }

    Image _img;

    void Awake()
    {
        Instance = this;
        _img = GetComponent<Image>();
        if (_img == null) _img = gameObject.AddComponent<Image>();
        _img.color = Color.black;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
