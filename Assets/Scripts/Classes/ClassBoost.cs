using UnityEngine;

[System.Serializable]
public class ClassBoost
{
    public string id;
    public string nome;
    public string descricao;
    public string classId; // "warrior", "mage", etc. — vazio = universal
    public System.Action onChoose; // lógica aplicada ao escolher
}
