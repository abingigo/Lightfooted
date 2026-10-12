using System.Collections;
using System.Linq;
using LightPlatformer.Obstacles;
using LightPlatformer.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LightPlatformTests
{
    const string Lab = "Assets/Scenes/Johnny/LightPlatformLab.unity";
    LightStoppingPlatform platform;
    Rigidbody2D player;
    GameMenuController Menu => GameMenuController.Instance;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync(Lab);
        yield return null;
        platform = Object.FindFirstObjectByType<LightStoppingPlatform>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController").enabled = false;
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
    }
    IEnumerator Frames(int count = 3)
    { for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate(); yield return null; }
    IEnumerator Until(System.Func<bool> condition, float seconds = 5)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.That(condition(), Is.True, "Timed out waiting for platform state");
    }
    BoxCollider2D Brake(string name)
    {
        var obj = new GameObject(name); obj.tag = "Light";
        obj.transform.position = platform.transform.position;
        var box = obj.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(1, 1);
        return box;
    }
    void PositionPlayer(Vector2 position, float gravity = 5)
    { player.position = position; player.linearVelocity = Vector2.zero; player.gravityScale = gravity; Physics2D.SyncTransforms(); }

    [UnityTest]
    public IEnumerator DemoIsListedAndDarkPlatformMovesBetweenBoundedEndpoints()
    {
        Assert.That(LevelCatalog.ReadBuildLevels().Any(level => level.ScenePath == Lab), Is.True);
        Assert.That(platform.IsIlluminated, Is.False);
        Vector2 start = platform.StartPosition;
        platform.speed = 12;
        bool reachedEnd = false, returned = false;
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForFixedUpdate();
            float x = platform.transform.position.x;
            Assert.That(x, Is.InRange(start.x - 0.01f, start.x + platform.travelOffset.x + 0.01f));
            if (x > start.x + 9.8f) reachedEnd = true;
            if (reachedEnd && x < start.x + 0.3f) returned = true;
        }
        Assert.That(reachedEnd && returned, Is.True, "Platform must ping-pong without overshooting");
        Assert.That(platform.stateLabel.text, Is.EqualTo("MOVING"));
        Assert.That(player.GetComponent<BoxCollider2D>().enabled, Is.True);
        Assert.That(player.position.y, Is.GreaterThan(0.3f));
    }

    [UnityTest]
    public IEnumerator ActiveLightHoldsExactPositionAndRemovingItResumesTheRoute()
    {
        var brake = Brake("Brake light");
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True);
        Assert.That(platform.stateLabel.text, Is.EqualTo("HELD BY LIGHT"));
        var held = platform.transform.position;
        yield return new WaitForSeconds(0.25f);
        Assert.That(Vector2.Distance(platform.transform.position, held), Is.LessThan(0.001f));
        brake.transform.position += Vector3.up * 10;
        yield return Frames(5);
        Assert.That(platform.IsIlluminated, Is.False);
        Assert.That(platform.transform.position.x, Is.GreaterThan(held.x));
    }

    [UnityTest]
    public IEnumerator MultipleLightsDisableDestroyAndBlinkWithoutStaleFreezes()
    {
        var first = Brake("First light"); var second = Brake("Second light");
        yield return Frames();
        first.enabled = false;
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True, "One remaining light must still hold the platform");
        Object.Destroy(second.gameObject);
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.False, "Destroy/disable must release without an exit callback");
        first.enabled = true;
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True, "Blinking back on must hold again");
        first.gameObject.SetActive(false);
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.False);
    }

    [UnityTest]
    public IEnumerator UnrelatedTriggerDoesNotStopThePlatform()
    {
        var other = Brake("Untagged trigger"); other.tag = "Untagged";
        var start = platform.transform.position;
        yield return Frames(8);
        Assert.That(platform.IsIlluminated, Is.False);
        Assert.That(platform.transform.position.x, Is.GreaterThan(start.x));
    }

    [UnityTest]
    public IEnumerator ExistingControllableLightCanAimAtAndReleaseThePlatform()
    {
        var lamp = GameObject.Find("Controllable brake light");
        var control = lamp.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "ControllableLight");
        control.SendMessage("SetSelected", true);
        control.SendMessage("SetDirection", (object)2); // Two F turns from the initial upward heading.
        // A rotating cone can touch briefly on its way to the selected heading.
        yield return Until(() => Mathf.Abs(Mathf.DeltaAngle(lamp.transform.eulerAngles.z, 180)) < 0.01f);
        yield return Until(() => platform.IsIlluminated);
        var held = platform.transform.position;
        yield return new WaitForSeconds(0.15f);
        Assert.That(Vector2.Distance(platform.transform.position, held), Is.LessThan(0.001f));
        control.SendMessage("SetDirection", (object)0); // Box zero to select the payload overload.
        yield return Until(() => !platform.IsIlluminated);
        yield return Frames(8);
        Assert.That(platform.transform.position.x, Is.GreaterThan(held.x));
    }

    [UnityTest]
    public IEnumerator StandingPlayerRidesAndJumpingPlayerDetaches()
    {
        PositionPlayer(new Vector2(platform.transform.position.x, 0.65f));
        yield return new WaitForSeconds(0.25f);
        float relative = player.position.x - platform.transform.position.x;
        yield return new WaitForSeconds(0.7f);
        Assert.That(player.position.x - platform.transform.position.x, Is.EqualTo(relative).Within(0.08f));
        Assert.That(player.position.y, Is.EqualTo(0.51f).Within(0.08f));
        var brake = Brake("Rider brake light");
        yield return Frames();
        var heldPlatform = platform.transform.position;
        float heldPlayer = player.position.x;
        yield return new WaitForSeconds(0.2f);
        Assert.That(platform.transform.position, Is.EqualTo(heldPlatform));
        Assert.That(player.position.x, Is.EqualTo(heldPlayer).Within(0.01f));
        Object.Destroy(brake.gameObject); yield return Frames();
        float jumpX = player.position.x;
        player.linearVelocity = new Vector2(0, 20);
        yield return new WaitForSeconds(0.2f);
        Assert.That(player.position.y, Is.GreaterThan(2));
        Assert.That(player.position.x, Is.EqualTo(jumpX).Within(0.08f), "Platform must not drag an ascending jump");
    }

    [UnityTest]
    public IEnumerator SensorOnlySideContactIsNotCarried()
    {
        // Freeze travel direction to move away from this side, avoiding a real collision.
        platform.travelOffset = new Vector2(-10, 0);
        PositionPlayer(new Vector2(platform.transform.position.x + 2.1f, 0.51f), 0);
        float x = player.position.x;
        yield return new WaitForSeconds(0.25f);
        Assert.That(player.position.x, Is.EqualTo(x).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator PauseStopsTravelAndRestartRestoresStartingState()
    {
        yield return new WaitForSeconds(0.35f);
        Menu.Pause();
        var paused = platform.transform.position;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(platform.transform.position, Is.EqualTo(paused));
        Menu.Resume(); yield return null; yield return null;
        yield return Frames(5);
        Assert.That(platform.transform.position.x, Is.GreaterThan(paused.x));
        var brake = Brake("Brake before restart"); yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True);
        Menu.Pause(); Assert.That(Menu.Restart(), Is.True);
        yield return Until(() => !Menu.IsLoading);
        yield return null;
        var reset = Object.FindFirstObjectByType<LightStoppingPlatform>();
        Assert.That(reset.StartPosition.x, Is.EqualTo(10));
        Assert.That(reset.transform.position.x, Is.LessThan(10.3f));
        Assert.That(reset.IsIlluminated, Is.False);
        Assert.That(GameObject.Find("Brake before restart"), Is.Null);
        Assert.That(Time.timeScale, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DemonstrationCanBeFinishedWithTeamForceBasedController()
    {
        bool jumpedOn = false, rode = false, jumpedOff = false;
        float timeout = Time.realtimeSinceStartup + 25;
        while (Menu.State == MenuState.Playing && Time.realtimeSinceStartup < timeout)
        {
            var velocity = player.linearVelocity;
            bool jump = false;
            bool grounded = Physics2D.Raycast(player.position, Vector2.down, 0.65f, LayerMask.GetMask("Ground"));
            if (!jumpedOn)
            {
                velocity.x = player.position.x < 7.3f ? 5 : 0;
                if (player.position.x >= 7.3f && grounded && platform.transform.position.x < 10.6f)
                { velocity.x = 5; jump = true; jumpedOn = true; }
            }
            else if (!rode)
            {
                velocity.x = 5;
                if (grounded && player.position.x > 9 && player.position.y > 0.3f)
                { rode = true; velocity.x = 0; }
            }
            else if (!jumpedOff)
            {
                velocity.x = 0;
                if (platform.transform.position.x >= 19 && grounded)
                { jumpedOff = true; velocity.x = 5; jump = true; }
            }
            else velocity.x = 5;
            var movement = player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            movement.GetType().GetField("moveX", flags).SetValue(movement, velocity.x / 5);
            movement.GetType().GetField("jumpQueued", flags).SetValue(movement, jump);
            movement.SendMessage("FixedUpdate");
            yield return new WaitForFixedUpdate();
        }
        Assert.That(jumpedOn && rode && jumpedOff, Is.True, "Route must use the moving platform");
        Assert.That(Menu.State, Is.EqualTo(MenuState.Victory), "Team force-based movement must reach the real exit");
    }
}
