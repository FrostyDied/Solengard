using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Solengard.Core;

namespace Solengard.UI
{
    /// <summary>
    /// Controla a tela de Configurações em UI Toolkit (piloto Steam).
    /// Anexar ao mesmo GameObject do UIDocument que carrega Config.uxml.
    ///
    /// Espelha o padrão do ConfigUIBinder.cs (uGUI):
    ///   - religa Música/SFX/Idioma no SettingsManager;
    ///   - usa SetValueWithoutNotify ao sincronizar;
    ///   - assina OnSettingsChanged para refletir mudanças externas.
    ///
    /// Escopo do piloto: só ÁUDIO (Música/SFX) + JOGO (Idioma) religados.
    /// Abas Vídeo/Conta são visuais (sem religar). UIDocument recria a árvore
    /// a cada OnEnable, então fazemos query+wire por enable (sem duplicar).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConfigPanelController : MonoBehaviour
    {
        VisualElement _root;

        // Abas e painéis (índices pareados)
        readonly List<Button> _tabs = new List<Button>();
        readonly List<VisualElement> _panels = new List<VisualElement>();

        // Áudio
        Slider _sliderMusica, _sliderSfx;
        Label _valMusica, _valSfx;
        VisualElement _fillMusica, _fillSfx;

        // Jogo
        DropdownField _dropdownIdioma;

        // Defaults de configuração (NÃO confundir com ResetAllProgress!)
        const float DEFAULT_MUSIC = 0.8f;
        const float DEFAULT_SFX = 0.8f;
        const int DEFAULT_LANG = 0; // 0 = Português

        void OnEnable()
        {
            // A ordem de OnEnable entre UIDocument e este controller é indefinida.
            // Esperar 1 frame garante que o UIDocument já construiu a árvore visual
            // (rootVisualElement pronto), inclusive ao reabrir o painel via SetActive.
            StartCoroutine(SetupWhenReady());
        }

        IEnumerator SetupWhenReady()
        {
            yield return null;

            var doc = GetComponent<UIDocument>();
            _root = doc != null ? doc.rootVisualElement : null;
            if (_root == null) yield break;

            WireTabs();
            WireAudio();
            WireJogo();
            WireButtons();

            RefreshFromSettings();

            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged += RefreshFromSettings;
        }

        void OnDisable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= RefreshFromSettings;
        }

        // ===================== ABAS =====================
        void WireTabs()
        {
            _tabs.Clear();
            _panels.Clear();

            AddTab("TabAudio", "PanelAudio");
            AddTab("TabVideo", "PanelVideo");
            AddTab("TabJogo",  "PanelJogo");
            AddTab("TabConta", "PanelConta");
        }

        void AddTab(string tabName, string panelName)
        {
            var tab = _root.Q<Button>(tabName);
            var panel = _root.Q<VisualElement>(panelName);
            if (tab == null || panel == null) return;

            _tabs.Add(tab);
            _panels.Add(panel);
            tab.clicked += () => ShowTab(tab, panel);
        }

        void ShowTab(Button activeTab, VisualElement activePanel)
        {
            foreach (var t in _tabs) t.RemoveFromClassList("is-active");
            foreach (var p in _panels) p.RemoveFromClassList("is-active");
            activeTab.AddToClassList("is-active");
            activePanel.AddToClassList("is-active");
        }

        // ===================== ÁUDIO =====================
        void WireAudio()
        {
            _sliderMusica = _root.Q<Slider>("SliderMusica");
            _valMusica    = _root.Q<Label>("ValMusica");
            _fillMusica   = EnsureSliderFill(_sliderMusica);
            if (_sliderMusica != null)
                _sliderMusica.RegisterValueChangedCallback(evt =>
                {
                    SettingsManager.Instance?.SetMusicVolume(evt.newValue);
                    UpdateSliderVisual(_sliderMusica, _fillMusica, _valMusica);
                });

            _sliderSfx = _root.Q<Slider>("SliderSfx");
            _valSfx    = _root.Q<Label>("ValSfx");
            _fillSfx   = EnsureSliderFill(_sliderSfx);
            if (_sliderSfx != null)
                _sliderSfx.RegisterValueChangedCallback(evt =>
                {
                    SettingsManager.Instance?.SetSfxVolume(evt.newValue);
                    UpdateSliderVisual(_sliderSfx, _fillSfx, _valSfx);
                });
        }

        /// <summary>Cria (uma vez) o elemento de fill âmbar dentro do tracker nativo.</summary>
        VisualElement EnsureSliderFill(Slider slider)
        {
            if (slider == null) return null;
            var tracker = slider.Q(className: "unity-base-slider__tracker");
            if (tracker == null) return null;

            var existing = tracker.Q("sol-slider-fill");
            if (existing != null) return existing;

            var fill = new VisualElement { name = "sol-slider-fill" };
            fill.AddToClassList("sol-slider-fill");
            fill.pickingMode = PickingMode.Ignore; // não rouba o drag do slider
            tracker.Add(fill);
            return fill;
        }

        void UpdateSliderVisual(Slider s, VisualElement fill, Label valueLabel)
        {
            if (s == null) return;
            float pct = Mathf.InverseLerp(s.lowValue, s.highValue, s.value) * 100f;
            if (fill != null) fill.style.width = new Length(pct, LengthUnit.Percent);
            if (valueLabel != null) valueLabel.text = Mathf.RoundToInt(pct) + "%";
        }

        // ===================== JOGO (Idioma) =====================
        void WireJogo()
        {
            _dropdownIdioma = _root.Q<DropdownField>("DropdownIdioma");
            if (_dropdownIdioma != null)
                _dropdownIdioma.RegisterValueChangedCallback(_ =>
                {
                    SettingsManager.Instance?.SetLanguage(_dropdownIdioma.index);
                });
        }

        // ===================== BOTÕES / RODAPÉ =====================
        void WireButtons()
        {
            var btnFechar = _root.Q<Button>("BtnFechar");
            if (btnFechar != null) btnFechar.clicked += Close;

            var btnSalvar = _root.Q<Button>("BtnSalvar");
            if (btnSalvar != null) btnSalvar.clicked += OnSalvar;

            var btnReset = _root.Q<Button>("BtnRestaurarPadroes");
            if (btnReset != null) btnReset.clicked += OnRestaurarPadroes;

            // Links do rodapé (são Labels — usam ClickEvent)
            var linkPriv = _root.Q<Label>("LinkPrivacidade");
            if (linkPriv != null)
                linkPriv.RegisterCallback<ClickEvent>(_ =>
                    Application.OpenURL("https://arctmind.com/solengard/privacidade"));

            var linkCred = _root.Q<Label>("LinkCreditos");
            if (linkCred != null)
                linkCred.RegisterCallback<ClickEvent>(_ =>
                    Debug.Log("[Config] Créditos (popup a implementar)."));
        }

        void OnSalvar()
        {
            // Os setters do SettingsManager já persistem (SaveSettings) a cada
            // alteração. 'Salvar' apenas confirma e fecha o painel.
            Close();
        }

        void OnRestaurarPadroes()
        {
            // IMPORTANTE: restaura apenas as CONFIGURAÇÕES para os defaults.
            // NÃO usar SettingsManager.ResetAllProgress() — aquilo apaga TODO o
            // progresso (diamantes, classes, upgrades) e não é o objetivo aqui.
            var sm = SettingsManager.Instance;
            if (sm == null) return;
            sm.SetMusicVolume(DEFAULT_MUSIC);
            sm.SetSfxVolume(DEFAULT_SFX);
            sm.SetLanguage(DEFAULT_LANG);
            RefreshFromSettings();
        }

        /// <summary>Fecha a tela (desativa o GameObject do UIDocument).</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        // ===================== SINCRONIZAÇÃO =====================
        void RefreshFromSettings()
        {
            var sm = SettingsManager.Instance;
            if (sm == null || _root == null) return;

            if (_sliderMusica != null)
            {
                _sliderMusica.SetValueWithoutNotify(sm.MusicVolume);
                UpdateSliderVisual(_sliderMusica, _fillMusica, _valMusica);
            }
            if (_sliderSfx != null)
            {
                _sliderSfx.SetValueWithoutNotify(sm.SfxVolume);
                UpdateSliderVisual(_sliderSfx, _fillSfx, _valSfx);
            }
            if (_dropdownIdioma != null && _dropdownIdioma.choices != null && _dropdownIdioma.choices.Count > 0)
            {
                int lang = Mathf.Clamp(sm.GetLanguage(), 0, _dropdownIdioma.choices.Count - 1);
                _dropdownIdioma.SetValueWithoutNotify(_dropdownIdioma.choices[lang]);
            }
        }
    }
}
