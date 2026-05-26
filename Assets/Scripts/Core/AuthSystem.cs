using UnityEngine;

// Sistema de autenticação com Supabase.
// STUB COMPLETO — toda integração real marcada com TODO.
// O jogo funciona offline sem login.
public class AuthSystem : MonoBehaviour
{
    public static AuthSystem Instance { get; private set; }

    // Passa: true se logado, false se deslogado
    public static event System.Action<bool> OnAuthStateChanged;

    const string PREF_TOKEN   = "sol_auth_token";
    const string PREF_USER_ID = "sol_auth_uid";

    bool isLoggedIn;
    string currentUserId;
    string authToken;

    public bool   IsLoggedIn     => isLoggedIn;
    public string CurrentUserId  => currentUserId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CarregarTokenLocal();
    }

    // ── Autenticação ─────────────────────────────────────────────────────────────

    public void SignInWithEmail(string email, string password)
    {
        // TODO: implementar chamada POST /auth/v1/token?grant_type=password ao Supabase
        // TODO: tratar erros de credencial inválida, conta não confirmada, etc.
        Debug.Log($"[AuthSystem] SignInWithEmail stub — {email}");
        SimularLoginBemSucedido("stub_user_email");
    }

    public void SignInWithGoogle()
    {
        // TODO: implementar OAuth 2.0 com Google via Unity Gaming Services ou plugin nativo
        // TODO: redirect URI: solengard://auth/callback
        Debug.Log("[AuthSystem] SignInWithGoogle stub");
        SimularLoginBemSucedido("stub_user_google");
    }

    public void SignInWithApple()
    {
        // TODO: implementar Sign in with Apple via Apple.GameKit ou plugin nativo (obrigatório App Store)
        Debug.Log("[AuthSystem] SignInWithApple stub");
        SimularLoginBemSucedido("stub_user_apple");
    }

    public void SignOut()
    {
        // TODO: chamar DELETE /auth/v1/logout no Supabase para invalidar o token no servidor
        authToken     = "";
        currentUserId = "";
        isLoggedIn    = false;

        PlayerPrefs.DeleteKey(PREF_TOKEN);
        PlayerPrefs.DeleteKey(PREF_USER_ID);
        PlayerPrefs.Save();

        OnAuthStateChanged?.Invoke(false);
        Debug.Log("[AuthSystem] Usuário deslogado.");
    }

    public string GetCurrentUser() => currentUserId;

    public void SyncPlayerData()
    {
        if (!isLoggedIn) return;
        // TODO: GET /rest/v1/players?id=eq.{currentUserId} para carregar dados do servidor
        // TODO: mesclar dados locais com dados do servidor (última escrita vence)
        Debug.Log("[AuthSystem] SyncPlayerData stub — sem conexão Supabase");
    }

    // ── Persistência de token ───────────────────────────────────────────────────

    void CarregarTokenLocal()
    {
        authToken     = PlayerPrefs.GetString(PREF_TOKEN, "");
        currentUserId = PlayerPrefs.GetString(PREF_USER_ID, "");

        if (!string.IsNullOrEmpty(authToken))
        {
            // TODO: validar token com GET /auth/v1/user antes de marcar como logado
            isLoggedIn = true;
            OnAuthStateChanged?.Invoke(true);
            Debug.Log($"[AuthSystem] Token local encontrado para user {currentUserId}.");
        }
    }

    void SalvarTokenLocal(string token, string userId)
    {
        // Armazenamento simples — considere SecurePlayerPrefs ou Keychain em produção
        PlayerPrefs.SetString(PREF_TOKEN, token);
        PlayerPrefs.SetString(PREF_USER_ID, userId);
        PlayerPrefs.Save();
    }

    void SimularLoginBemSucedido(string userId)
    {
        authToken     = "stub_token_" + System.Guid.NewGuid().ToString("N");
        currentUserId = userId;
        isLoggedIn    = true;
        SalvarTokenLocal(authToken, currentUserId);
        OnAuthStateChanged?.Invoke(true);
        Debug.Log($"[AuthSystem] Login bem-sucedido (stub): {currentUserId}");
    }
}
