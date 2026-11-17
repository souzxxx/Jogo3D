using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Mede em que fracao de cada clipe de ataque do pack a mao da arma desce mais rapido (o instante do golpe).
/// `unity run . -- -executeMethod AnimContactProbe.Run` grava Logs/contato.txt
public static class AnimContactProbe
{
    public static void Run()
    {
        var sb = new StringBuilder();
        const string fbx = "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx";
        string path = AssetDatabase.FindAssets("Skeleton_Warrior t:Model").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault() ?? fbx;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = Object.Instantiate(model);
        var hand = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "handslot.r") ?? go.GetComponentsInChildren<Transform>().First(t => t.name.Contains("hand") && t.name.EndsWith(".r"));
        var clipsPath = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/KayKit" }).Select(AssetDatabase.GUIDToAssetPath);
        var names = new[] { "1H_Melee_Attack_Chop", "1H_Melee_Attack_Jump_Chop", "2H_Melee_Attack_Chop", "1H_Melee_Attack_Slice_Diagonal", "2H_Melee_Attack_Slice", "Spellcast_Shoot", "Spellcast_Long", "Unarmed_Melee_Attack_Punch_A", "Block_Attack", "1H_Melee_Attack_Stab", "Throw" };
        foreach (var n in names)
        {
            var clip = clipsPath.SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>()).FirstOrDefault(c => c.name == n);
            if (clip == null) { sb.AppendLine($"{n}: nao achei"); continue; }
            const int steps = 120;
            // estocada e golpe de escudo vao para a frente, nao para baixo: neles vale a mao mais rapida em qualquer direcao
            bool thrust = n.Contains("Stab") || n.Contains("Block") || n.Contains("Throw");
            float bestSpeed = 0f, bestT = 0f, maxY = float.MinValue;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float t = clip.length * i / steps;
                clip.SampleAnimation(go, t);
                var p = hand.position;
                if (i > 0)
                {
                    float speed = (p - prev).magnitude / (clip.length / steps);
                    // golpe: mao descendo depois de ja ter subido acima do ponto de partida
                    if ((thrust || (p.y < prev.y && maxY > p.y)) && speed > bestSpeed) { bestSpeed = speed; bestT = t; }
                }
                maxY = Mathf.Max(maxY, p.y);
                prev = p;
            }
            sb.AppendLine($"{n}: duracao {clip.length:0.00} s, golpe em {bestT:0.00} s ({bestT / clip.length:0.00} do clipe)");
        }
        Object.DestroyImmediate(go);
        File.WriteAllText("Logs/contato.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }
}
