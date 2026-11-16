using UnityEditor;

/// Regera as cinco cenas geradas por codigo numa chamada so (Title, Run e as tres de dev).
/// `unity run . -- -executeMethod SceneRebuild.All`
public static class SceneRebuild
{
    [MenuItem("Quintessence/Regerar todas as cenas")]
    public static void All()
    {
        GameSceneBuilder.Build();
        CombatDevSceneBuilder.Build();
        EnemyDevSceneBuilder.Build();
        EnemyDevSceneBuilder.BuildBossScene();
    }
}
