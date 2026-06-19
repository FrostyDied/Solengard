using UnityEngine;
using UnityEditor;

// [DEBUG] Ferramenta de teste de economia — injeta/zera diamantes via o MESMO sistema que
// persiste (DiamondSystem / PlayerPrefs "sol_dia" ofuscado por XOR). Editor-only -> NAO entra
// no build de producao (qualquer .cs em Assets/Scripts/Editor e excluido do build).
//
// Idempotente: "Adicionar (99999)" leva o saldo EXATAMENTE a 99999 (re-rodar nao acumula).
// Reset: use "[DEBUG] Zerar Diamantes" (ou apague a PlayerPrefs key 'sol_dia').
public static class SolengardDiamondDebug
{
    const string PREF_KEY = "sol_dia"; // mesma key do DiamondSystem
    const byte   XOR_KEY  = 0x4B;      // mesma cifra do DiamondSystem
    const int    ALTO     = 99999;

    [MenuItem("Solengard/Debug/[DEBUG] Adicionar Diamantes (99999)")]
    static void SetAlto() => DefinirSaldo(ALTO);

    [MenuItem("Solengard/Debug/[DEBUG] Zerar Diamantes")]
    static void Zerar() => DefinirSaldo(0);

    static void DefinirSaldo(int valor)
    {
        // Em Play: usa o singleton vivo -> atualiza saldo + dispara OnDiamondsChanged (UI atualiza na hora).
        if (Application.isPlaying && DiamondSystem.Instance != null)
        {
            var ds  = DiamondSystem.Instance;
            int cur = ds.GetBalance();
            if      (valor > cur) ds.AddDiamonds(valor - cur);
            else if (valor < cur) ds.SpendDiamonds(cur - valor);
            Debug.Log($"[DEBUG] Diamantes (runtime) = {ds.GetBalance()}");
        }
        else
        {
            // Em Edit mode: grava direto na PlayerPrefs ofuscada (mesma cifra) -> o proximo Play carrega.
            PlayerPrefs.SetString(PREF_KEY, Cifrar(valor));
            PlayerPrefs.Save();
            Debug.Log($"[DEBUG] Diamantes (PlayerPrefs '{PREF_KEY}') = {valor}. Entre em Play para aplicar.");
        }
    }

    static string Cifrar(int valor)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(valor.ToString());
        for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XOR_KEY;
        return System.Convert.ToBase64String(bytes);
    }
}
