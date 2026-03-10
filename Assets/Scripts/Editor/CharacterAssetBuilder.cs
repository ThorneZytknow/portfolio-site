#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CharacterAssetBuilder : EditorWindow
{
    [MenuItem("Brawler/Gerar Elenco Base F2P")]
    public static void GenerateRoster()
    {
        CreateFolders();

        // 1. SOLARIS
        CharacterData solaris = CreateCharacter("Solaris", "Char_Solaris", "Balanced", 5.5f, 12f, 105f, 1f);
        CreateAbility("Fenda Solar", "Neutral", 12f, 15f, new Vector2(1, 0.5f), 6f, false, solaris.characterName);
        CreateAbility("Ascensão Radiante", "Up", 9f, 20f, new Vector2(0, 1), 8f, false, solaris.characterName);
        CreateAbility("Tremor da Luz", "Down", 7f, 10f, new Vector2(1, 0), 5f, false, solaris.characterName);
        CreateAbility("Juízo Solar", "Special", 22f, 25f, new Vector2(0, -1), 14f, false, solaris.characterName, "JuizoSolarRPC");

        // 2. NYXARA
        CharacterData nyxara = CreateCharacter("Nyxara", "Char_Nyxara", "Trickster", 5.0f, 13.5f, 78f, 1.3f);
        CreateAbility("Fragmento das Trevas", "Neutral", 8f, 8f, new Vector2(1, 0.2f), 4f, true, nyxara.characterName);
        CreateAbility("Portal Inverso", "Up", 14f, 22f, new Vector2(0, -1), 10f, false, nyxara.characterName, "PortalInversoRPC");
        CreateAbility("Sombra Espelhada", "Down", 0f, 0f, Vector2.zero, 9f, false, nyxara.characterName, "SombraEspelhadaRPC");
        CreateAbility("Abismo Devorador", "Special", 24f, 18f, new Vector2(0, 1), 16f, false, nyxara.characterName, "AbismoDevoradorRPC");

        // 3. TORRAK
        CharacterData torrak = CreateCharacter("Torrak", "Char_Torrak", "Heavy Grappler", 4.0f, 9.5f, 135f, 0.75f);
        var whip = CreateAbility("Chicote de Corrente", "Neutral", 15f, 15f, new Vector2(1, 0.5f), 5f, false, torrak.characterName);
        whip.hasArmor = true; whip.armorFrames = 10;
        CreateAbility("Lançamento Orbital", "Up", 28f, 30f, new Vector2(0, 1), 12f, false, torrak.characterName, "GrappleThrowRPC");
        CreateAbility("Terremoto", "Down", 18f, 18f, new Vector2(1, 0.1f), 9f, false, torrak.characterName);
        CreateAbility("Correntes do Caos", "Special", 35f, 20f, new Vector2(1, 1), 18f, false, torrak.characterName, "CorrentesCaosRPC");

        // 4. VELOCI
        CharacterData veloci = CreateCharacter("Veloci", "Char_Veloci", "Speedster", 8.5f, 15.0f, 82f, 1.8f);
        CreateAbility("Dança das Lâminas", "Neutral", 20f, 12f, new Vector2(1, 0.2f), 3f, false, veloci.characterName);
        CreateAbility("Tornado Ascendente", "Up", 24f, 15f, new Vector2(0, 1), 7f, false, veloci.characterName);
        CreateAbility("Ilusão de Passo", "Down", 11f, 12f, new Vector2(1, 1), 6f, false, veloci.characterName, "TeleportDashRPC");
        CreateAbility("Mil Cortes", "Special", 36f, 25f, new Vector2(1, 0.5f), 20f, false, veloci.characterName, "CinematicSlashRPC");

        // 5. GAÏA
        CharacterData gaia = CreateCharacter("Gaïa", "Char_Gaia", "Controller", 4.8f, 11.0f, 92f, 0.9f);
        CreateAbility("Rajada de Espinhos", "Neutral", 15f, 8f, new Vector2(1, 0.1f), 5f, true, gaia.characterName);
        CreateAbility("Gêiser Selvagem", "Up", 13f, 22f, new Vector2(0, 1), 8f, false, gaia.characterName);
        CreateAbility("Raízes Aprisionadoras", "Down", 6f, 0f, Vector2.zero, 11f, false, gaia.characterName, "RootSnareRPC");
        CreateAbility("Wrath of Gaia", "Special", 70f, 25f, new Vector2(0, -1), 22f, false, gaia.characterName, "MeteorShowerRPC");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Todos os ScriptableObjects de Personagens e Habilidades foram gerados!");
    }

    private static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Characters"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Characters");

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Abilities"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Abilities");
    }

    private static CharacterData CreateCharacter(string name, string id, string archetype, float speed, float jump, float weight, float atkSpeed)
    {
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName = name;
        data.characterId = id;
        data.archetype = archetype;
        data.moveSpeed = speed;
        data.jumpForce = jump;
        data.weight = weight;
        data.attackSpeedMultiplier = atkSpeed;

        string path = $"Assets/ScriptableObjects/Characters/{name}_Data.asset";
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static AbilityData CreateAbility(string name, string type, float dmg, float kb, Vector2 dir, float cd, bool isProj, string charName, string rpc = "")
    {
        AbilityData data = ScriptableObject.CreateInstance<AbilityData>();
        data.abilityName = name;
        data.abilityType = type;
        data.damage = dmg;
        data.baseKnockback = kb;
        data.knockbackDirection = dir;
        data.cooldown = cd;
        data.isProjectile = isProj;
        data.rpcMethodName = rpc;

        // Atribui valores padrão baseados no tipo
        data.staminaCost = cd * 2f;
        if (isProj) data.projectileSpeed = 15f;
        data.hitboxDuration = type == "Neutral" ? 0.2f : 0.5f;

        string folderPath = $"Assets/ScriptableObjects/Abilities/{charName}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Abilities", charName);
        }

        string safeName = name.Replace(" ", "").Replace("ï", "i").Replace("ã", "a");
        string path = $"{folderPath}/{safeName}.asset";
        AssetDatabase.CreateAsset(data, path);

        return data;
    }
}
#endif
