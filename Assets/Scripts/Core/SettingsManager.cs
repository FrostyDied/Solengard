using UnityEngine;
using System;

namespace Solengard.Core
{
    /// <summary>
    /// Gerenciador de configurações agnóstico de plataforma (mobile + Steam).
    /// Backend puro: áudio, idioma, reset. A UI (mobile/Steam) apenas consome esta API.
    /// Áudio: hooks prontos para MMSoundManager (Feel) — basta conectar em ApplyAudioToEngine().
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // ── Chaves PlayerPrefs ──
        const string PREF_MUSIC_VOL = "settings_music_volume";
        const string PREF_SFX_VOL   = "settings_sfx_volume";
        const string PREF_MUSIC_ON  = "settings_music_on";
        const string PREF_SFX_ON    = "settings_sfx_on";

        // ── Estado em memória ──
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume   { get; private set; } = 0.8f;
        public bool  MusicOn     { get; private set; } = true;
        public bool  SfxOn       { get; private set; } = true;

        // Eventos para a UI reagir (ex: atualizar slider/toggle)
        public event Action OnSettingsChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }

        // ── Carregar / Salvar ──
        public void LoadSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.8f);
            SfxVolume   = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
            MusicOn     = PlayerPrefs.GetInt(PREF_MUSIC_ON, 1) == 1;
            SfxOn       = PlayerPrefs.GetInt(PREF_SFX_ON, 1) == 1;
            ApplyAudioToEngine();
        }

        void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, SfxVolume);
            PlayerPrefs.SetInt(PREF_MUSIC_ON, MusicOn ? 1 : 0);
            PlayerPrefs.SetInt(PREF_SFX_ON, SfxOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Setters (chamados pela UI) ──
        public void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); SaveSettings(); ApplyAudioToEngine(); OnSettingsChanged?.Invoke(); }
        public void SetSfxVolume(float v)   { SfxVolume = Mathf.Clamp01(v); SaveSettings(); ApplyAudioToEngine(); OnSettingsChanged?.Invoke(); }
        public void SetMusicOn(bool on)     { MusicOn = on; SaveSettings(); ApplyAudioToEngine(); OnSettingsChanged?.Invoke(); }
        public void SetSfxOn(bool on)       { SfxOn = on; SaveSettings(); ApplyAudioToEngine(); OnSettingsChanged?.Invoke(); }

        /// <summary>
        /// Aplica os volumes ao motor de áudio.
        /// TODO: conectar ao MMSoundManager (Feel) quando o áudio for integrado.
        /// Exemplo futuro:
        ///   MMSoundManager.Current.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music, MusicOn ? MusicVolume : 0f);
        ///   MMSoundManager.Current.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx, SfxOn ? SfxVolume : 0f);
        /// Por ora, controla o AudioListener global como fallback seguro.
        /// </summary>
        void ApplyAudioToEngine()
        {
            // Fallback até integrar o MMSoundManager: master volume reflete música ligada
            // (quando o áudio real entrar, troque por chamadas ao MMSoundManager por track)
            float master = (MusicOn ? MusicVolume : 0f);
            AudioListener.volume = Mathf.Clamp01(master);
        }

        // ── Idioma (delega ao LocalizationManager existente) ──
        public void SetLanguage(int langIndex)
        {
            // Mantém compatível com a chave PREF_LANGUAGE do LocalizationManager
            PlayerPrefs.SetInt("PREF_LANGUAGE", langIndex);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
        public int GetLanguage() => PlayerPrefs.GetInt("PREF_LANGUAGE", 0);

        // ── Reset de progresso (confirmação dupla é responsabilidade da UI) ──
        /// <summary>
        /// Apaga TODO o progresso: diamantes, classes, upgrades, missões, streak, score.
        /// A UI DEVE pedir confirmação dupla antes de chamar este método.
        /// </summary>
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Settings] Progresso resetado — PlayerPrefs.DeleteAll()");
            // Recarrega settings para os defaults (já que DeleteAll apagou as chaves de áudio também)
            LoadSettings();
        }
    }
}
