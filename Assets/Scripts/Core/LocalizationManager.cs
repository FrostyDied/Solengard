using System.Collections.Generic;
using UnityEngine;

public enum Language { Portuguese, English, Spanish }

// Sistema de localização. Carrega strings de Resources/Localization/{idioma}.json
// Formato JSON esperado: { "keys": ["key1","key2"], "values": ["val1","val2"] }
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    const string PREF_LANGUAGE = "sol_language";

    Language idiomaAtual;
    Dictionary<string, string> traducoes = new();

    public Language IdiomaAtual => idiomaAtual;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Language idioma = CarregarIdiomaPreferido();
        SetLanguage(idioma);
    }

    // Retorna a string traduzida para a chave informada ou a própria chave se não encontrada
    public string GetString(string chave)
    {
        return traducoes.TryGetValue(chave, out string valor) ? valor : chave;
    }

    public void SetLanguage(Language idioma)
    {
        idiomaAtual = idioma;
        CarregarArquivoDeIdioma(idioma);
        PlayerPrefs.SetInt(PREF_LANGUAGE, (int)idioma);
        PlayerPrefs.Save();
        Debug.Log($"[LocalizationManager] Idioma: {idioma}");
    }

    Language CarregarIdiomaPreferido()
    {
        if (PlayerPrefs.HasKey(PREF_LANGUAGE))
            return (Language)PlayerPrefs.GetInt(PREF_LANGUAGE);

        // Detecta idioma do sistema no primeiro launch
        return DetectarIdiomaDoSistema();
    }

    Language DetectarIdiomaDoSistema()
    {
        string cod = Application.systemLanguage.ToString();
        return cod switch
        {
            "Portuguese" or "BrazilianPortuguese" => Language.Portuguese,
            "Spanish"                              => Language.Spanish,
            _                                      => Language.English,
        };
    }

    void CarregarArquivoDeIdioma(Language idioma)
    {
        string arquivo = idioma switch
        {
            Language.Portuguese => "pt",
            Language.Spanish    => "es",
            _                   => "en",
        };

        TextAsset asset = Resources.Load<TextAsset>($"Localization/{arquivo}");
        if (asset == null)
        {
            Debug.LogWarning($"[LocalizationManager] Arquivo Resources/Localization/{arquivo}.json não encontrado.");
            // Cria ao menos entradas vazias para evitar erros
            traducoes = new Dictionary<string, string>();
            return;
        }

        LocalizationData dados = JsonUtility.FromJson<LocalizationData>(asset.text);
        traducoes = new Dictionary<string, string>();

        if (dados?.keys == null) return;

        for (int i = 0; i < dados.keys.Count && i < dados.values.Count; i++)
            traducoes[dados.keys[i]] = dados.values[i];

        Debug.Log($"[LocalizationManager] {traducoes.Count} strings carregadas ({arquivo}).");
    }

    [System.Serializable]
    class LocalizationData
    {
        public List<string> keys   = new();
        public List<string> values = new();
    }
}
