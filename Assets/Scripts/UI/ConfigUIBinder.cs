using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Solengard.Core;

namespace Solengard.UI
{
    /// <summary>
    /// Liga os controles visuais do PainelConfiguracoes ao SettingsManager em runtime.
    /// Anexar ao GameObject PainelConfiguracoes. Faz o bind no Start procurando
    /// os controles por nome (construídos pelo SolengardLayoutSetup).
    /// </summary>
    public class ConfigUIBinder : MonoBehaviour
    {
        bool _bound = false;

        // Sprites do switch GUI Pro (cacheados)
        Sprite _swBgOn, _swBgOff, _swHandleOn, _swHandleOff;
        bool _spritesLoaded = false;

        void LoadSwitchSprites()
        {
            if (_spritesLoaded) return;
            const string B = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/UI_Etc/";
            #if UNITY_EDITOR
            _swBgOn      = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(B + "Switch_Bg_Single_On.png");
            _swBgOff     = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(B + "Switch_Bg_Single_Off.png");
            _swHandleOn  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(B + "Switch_Handle_On.png");
            _swHandleOff = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(B + "Switch_Handle_Off.png");
            #endif
            _spritesLoaded = true;
        }

        void OnEnable()
        {
            // Bind ao ativar (o painel começa inativo e é ativado ao abrir)
            if (!_bound) Bind();
            RefreshFromSettings();
        }

        void Bind()
        {
            var sm = SettingsManager.Instance;
            if (sm == null)
            {
                Debug.LogWarning("[ConfigBinder] SettingsManager.Instance é null. Garanta que ele existe na cena (GameObject com SettingsManager).");
                return;
            }

            // ── Slider Música ──
            var musicaSlider = FindSlider("Musica_Slider");
            if (musicaSlider != null)
                musicaSlider.onValueChanged.AddListener(v => SettingsManager.Instance.SetMusicVolume(v));

            // ── Slider SFX ──
            var sfxSlider = FindSlider("SFX_Slider");
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance.SetSfxVolume(v));

            // ── Switch Música ──
            var musicaToggle = FindToggle("Musica_Switch");
            if (musicaToggle != null)
                musicaToggle.onValueChanged.AddListener(on => {
                    SettingsManager.Instance.SetMusicOn(on);
                    UpdateSwitchVisual(musicaToggle, on);
                });

            // ── Switch SFX ──
            var sfxToggle = FindToggle("SFX_Switch");
            if (sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(on => {
                    SettingsManager.Instance.SetSfxOn(on);
                    UpdateSwitchVisual(sfxToggle, on);
                });

            // ── Botões ──
            WireButton("BtnIdioma",     OnIdioma);
            WireButton("BtnConta",      OnConta);
            WireButton("BtnRestaurar",  OnRestaurar);
            WireButton("BtnPrivacidade", OnPrivacidade);
            WireButton("BtnCreditos",   OnCreditos);
            WireButton("BtnFecharConfig", OnFechar);

            // TESTE: toggle puro
            var toggleTeste = FindToggle("ToggleTeste");
            if (toggleTeste != null)
            {
                toggleTeste.onValueChanged.AddListener(on => Debug.Log($"[TESTE] Toggle puro mudou para: {on}"));
                Debug.Log("[TESTE] Listener do ToggleTeste adicionado.");
            }
            else Debug.Log("[TESTE] ToggleTeste NÃO encontrado.");

            _bound = true;
            Debug.Log("[ConfigBinder] Controles ligados ao SettingsManager.");
        }

        // ── Helpers de busca (recursiva por nome) ──
        Transform FindDeep(string name)
        {
            var all = GetComponentsInChildren<Transform>(true);
            foreach (var t in all) if (t.name == name) return t;
            return null;
        }
        Slider FindSlider(string name) { var t = FindDeep(name); return t ? t.GetComponent<Slider>() : null; }
        Toggle FindToggle(string name) { var t = FindDeep(name); return t ? t.GetComponent<Toggle>() : null; }

        void WireButton(string name, UnityEngine.Events.UnityAction action)
        {
            var t = FindDeep(name);
            if (t == null) { Debug.LogWarning($"[ConfigBinder] Botão não encontrado: {name}"); return; }
            var btn = t.GetComponent<Button>();
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); }
        }

        // ── Sincroniza UI com o estado salvo ──
        void RefreshFromSettings()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;

            var ms = FindSlider("Musica_Slider"); if (ms != null) ms.SetValueWithoutNotify(sm.MusicVolume);
            var ss = FindSlider("SFX_Slider");    if (ss != null) ss.SetValueWithoutNotify(sm.SfxVolume);
            var mt = FindToggle("Musica_Switch"); if (mt != null) mt.SetIsOnWithoutNotify(sm.MusicOn);
            var st = FindToggle("SFX_Switch");    if (st != null) st.SetIsOnWithoutNotify(sm.SfxOn);
            if (mt != null) UpdateSwitchVisual(mt, sm.MusicOn);
            if (st != null) UpdateSwitchVisual(st, sm.SfxOn);
        }

        // ── Ações dos botões ──
        void OnIdioma()
        {
            Debug.Log("[Config] Abrir seleção de idioma (popup a implementar).");
            // TODO: abrir PopupIdioma com grid de bandeiras
        }

        void OnConta()
        {
            Debug.Log("[Config] Abrir tela de Conta/Login.");
            // TODO: AccountManager.Instance.OpenLoginFlow();
        }

        void OnRestaurar()
        {
            Debug.Log("[Config] Restaurar compras.");
            // TODO: IAPManager.Instance.RestorePurchases();
        }

        void OnPrivacidade()
        {
            Debug.Log("[Config] Abrir política de privacidade.");
            Application.OpenURL("https://arctmind.com/solengard/privacidade"); // ajustar URL real
        }

        void OnCreditos()
        {
            Debug.Log("[Config] Abrir créditos.");
            // TODO: abrir popup de créditos
        }

        void OnFechar()
        {
            // Fecha o painel (volta ao menu). Segue o padrão do MainMenuManager.
            gameObject.SetActive(false);
        }

        void UpdateSwitchVisual(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            LoadSwitchSprites();

            // Fundo: roxo discreto quando ON, cinza quando OFF (mais previsível que trocar sprite)
            var bgImg = toggle.GetComponent<UnityEngine.UI.Image>();
            if (bgImg != null)
                bgImg.color = isOn ? Hex2("#5A3A90") : Hex2("#3A3A44");

            // Handle: troca sprite e posição — NUNCA some
            var handle = toggle.transform.Find("Handle");
            if (handle != null)
            {
                var himg = handle.GetComponent<UnityEngine.UI.Image>();
                if (himg != null)
                {
                    var hspr = isOn ? _swHandleOn : _swHandleOff;
                    // Usa o sprite se carregou; senão mantém o atual. Cor sempre visível (branco/cinza claro)
                    if (hspr != null) himg.sprite = hspr;
                    himg.color = isOn ? Color.white : new Color(0.75f, 0.75f, 0.78f, 1f);
                    himg.enabled = true; // garante que nunca fica invisível
                }
                var hrt = handle.GetComponent<RectTransform>();
                if (hrt != null)
                {
                    // OFF à esquerda, ON à direita — sempre dentro da área visível
                    hrt.anchorMin = new Vector2(isOn ? 0.52f : 0.05f, 0.12f);
                    hrt.anchorMax = new Vector2(isOn ? 0.95f : 0.48f, 0.88f);
                    hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
                }
            }
        }

        static Color Hex2(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
