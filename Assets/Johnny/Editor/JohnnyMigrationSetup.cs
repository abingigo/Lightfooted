using UnityEngine;

public static class JohnnyMigrationSetup
{
    public static void Create()
    {
        ObstacleLabSetup.Create();
        LightPlatformLabSetup.Create();
        GameUiSetup.CreateMenuScene();
        Debug.Log("JOHNNY_MIGRATION_SETUP_COMPLETE");
    }

    public static void BuildPreview()
    {
        Create();
        GameUiSetup.BuildPreview();
    }
}
