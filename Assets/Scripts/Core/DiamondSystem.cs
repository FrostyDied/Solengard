using UnityEngine;

// Gerencia a moeda premium do jogo com persistência local criptografada por XOR.
// Sincronização com Supabase marcada como TODO para implementação futura.
public class DiamondSystem : MonoBehaviour
{
    public static DiamondSystem Instance { get; private set; }

    public static event System.Action<int> OnDiamondsChanged;

    // Chave XOR para ofuscar o saldo no PlayerPrefs
    const byte XOR_KEY    = 0x4B;
    const string PREF_KEY = "sol_dia";

    int saldoAtual;
    [SerializeField] PlayerData playerData;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CarregarSaldo();
    }

    public int GetBalance() => saldoAtual;

    public void AddDiamonds(int quantidade)
    {
        Debug.Log($"[DiamondSystem] AddDiamonds amount={quantidade} saldoAtual={saldoAtual}");
        Debug.Log($"[DiamondSystem] AddDiamonds({quantidade}) — saldo atual: {saldoAtual}");
        if (quantidade <= 0) return;
        saldoAtual += quantidade;
        SalvarSaldo();
        if (playerData != null) { playerData.totalDiamonds = saldoAtual; playerData.Save(); }
        OnDiamondsChanged?.Invoke(saldoAtual);
        Debug.Log($"[DiamondSystem] +{quantidade} diamantes. Saldo: {saldoAtual}");

        // TODO: sincronizar saldo com Supabase quando houver conexão
    }

    // Retorna true e debita se houver saldo suficiente; false caso contrário
    public bool SpendDiamonds(int quantidade)
    {
        if (quantidade <= 0 || saldoAtual < quantidade) return false;

        saldoAtual -= quantidade;
        SalvarSaldo();
        if (playerData != null) { playerData.totalDiamonds = saldoAtual; playerData.Save(); }
        OnDiamondsChanged?.Invoke(saldoAtual);
        Debug.Log($"[DiamondSystem] -{quantidade} diamantes. Saldo: {saldoAtual}");

        // TODO: sincronizar saldo com Supabase quando houver conexão
        return true;
    }

    // ── Persistência com ofuscação XOR ──────────────────────────────────────────

    void SalvarSaldo()
    {
        PlayerPrefs.SetString(PREF_KEY, Cifrar(saldoAtual));
        PlayerPrefs.Save();
    }

    void CarregarSaldo()
    {
        string cifrado = PlayerPrefs.GetString(PREF_KEY, "");
        saldoAtual = cifrado.Length > 0 ? Decifrar(cifrado) : 0;
    }

    string Cifrar(int valor)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(valor.ToString());
        for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XOR_KEY;
        return System.Convert.ToBase64String(bytes);
    }

    int Decifrar(string cifrado)
    {
        try
        {
            byte[] bytes = System.Convert.FromBase64String(cifrado);
            for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XOR_KEY;
            return int.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        }
        catch { return 0; }
    }
}
