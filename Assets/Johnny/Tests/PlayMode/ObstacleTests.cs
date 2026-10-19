using System.Collections;
using System.Linq;
using LightPlatformer.Obstacles;
using LightPlatformer.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ObstacleTests
{
    const string Lab = "Assets/Scenes/Johnny/ObstacleLab.unity";
    Rigidbody2D player;
    GameMenuController Menu => GameMenuController.Instance;
    SpikeHazard Spike => Object.FindObjectsByType<SpikeHazard>(FindObjectsSortMode.None).OrderBy(item => item.transform.position.x).First();
    PulseBeam Beam => Object.FindObjectsByType<PulseBeam>(FindObjectsSortMode.None).OrderBy(item => item.transform.position.x).First();
    CrumblePlatform Tile => Object.FindObjectsByType<CrumblePlatform>(FindObjectsSortMode.None).OrderBy(item => item.transform.position.x).First();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync(Lab);
        yield return null;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        // Drive the real rigidbody deterministically without synthesizing OS keys.
        var movement = player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController");
        movement.enabled = false;
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
    }

    void Position(Vector2 position, bool floating = true)
    {
        player.position = position; player.linearVelocity = Vector2.zero;
        player.gravityScale = floating ? 0 : 5;
        Physics2D.SyncTransforms();
    }
    IEnumerator PhysicsFrames(int count = 3)
    {
        for (int i = 0; i < count && Menu.State == MenuState.Playing; i++) yield return new WaitForFixedUpdate();
        yield return null;
    }
    IEnumerator Until(System.Func<bool> condition, float seconds = 3)
    {
        float timeout = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(condition(), Is.True, "Timed out waiting for obstacle state");
    }

    [UnityTest]
    public IEnumerator LabIsListedAndContainsThreeReusableObstacleTypes()
    {
        Assert.That(LevelCatalog.ReadBuildLevels().Any(level => level.ScenePath == Lab), Is.True);
        Assert.That(Object.FindObjectsByType<SpikeHazard>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
        Assert.That(Object.FindObjectsByType<PulseBeam>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
        Assert.That(Object.FindObjectsByType<CrumblePlatform>(FindObjectsSortMode.None).Length, Is.EqualTo(4));
        yield return new WaitForSeconds(0.3f);
        Assert.That(player.GetComponent<BoxCollider2D>().enabled, Is.True, "Steady light must keep the player solid");
        Assert.That(player.position.y, Is.GreaterThan(0.3f), "Start must remain safe while idle");
    }

    [UnityTest]
    public IEnumerator SensorProximityIsSafeButActualSpikeContactKills()
    {
        Position(new Vector2(Spike.transform.position.x - 1.7f, 0.51f));
        yield return PhysicsFrames();
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing), "Light sensor overlap must not count as a hit");
        Position(new Vector2(Spike.transform.position.x, 0.5f));
        yield return PhysicsFrames();
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DisabledSolidColliderDoesNotGrantHazardImmunity()
    {
        player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerLightTracker").enabled = false;
        player.GetComponent<BoxCollider2D>().enabled = false;
        Position(new Vector2(Spike.transform.position.x, 0.5f));
        yield return PhysicsFrames();
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
    }

    [UnityTest]
    public IEnumerator BeamWarningIsSafeAndAnOccupantIsHitWhenItTurnsOn()
    {
        var beam = Beam;
        beam.safeDuration = 0.25f; beam.warningDuration = 0.25f; beam.activeDuration = 1;
        beam.ResetCycle();
        Position(new Vector2(beam.transform.position.x, 0.5f));
        yield return PhysicsFrames();
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        yield return Until(() => beam.Phase == BeamPhase.Warning);
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        Assert.That(beam.stateLabel.text, Is.EqualTo("GET CLEAR"));
        yield return Until(() => Menu.State == MenuState.GameOver);
        Assert.That(beam.Phase, Is.EqualTo(BeamPhase.Active));
    }

    [UnityTest]
    public IEnumerator SafeBeamCanBeCrossedWithRealPhysics()
    {
        var beam = Beam; beam.ResetCycle();
        Position(new Vector2(beam.transform.position.x - 1.4f, 0.51f));
        player.linearVelocity = new Vector2(5, 0);
        yield return new WaitForSeconds(0.65f);
        Assert.That(player.position.x, Is.GreaterThan(beam.transform.position.x + 1));
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
    }

    [UnityTest]
    public IEnumerator CrumbleIgnoresSideContactAndTriggersFromARealLanding()
    {
        var tile = Tile;
        // Isolate side contact from the neighboring approach floor.
        tile.transform.position = new Vector3(4, 8, 0);
        Position(new Vector2(tile.transform.position.x - 1.69f, 7.65f));
        yield return PhysicsFrames();
        Assert.That(tile.State, Is.EqualTo(CrumbleState.Stable));
        Position(new Vector2(tile.transform.position.x, 8.7f), false);
        yield return Until(() => tile.State == CrumbleState.Warning);
        Assert.That(tile.GetComponent<BoxCollider2D>().enabled, Is.True);
        Assert.That(tile.stateLabel.text, Is.EqualTo("MOVE"));
    }

    [UnityTest]
    public IEnumerator CrumbleCollapsesAndDoesNotReturnThroughThePlayer()
    {
        var tile = Tile; tile.collapseDelay = 0.15f; tile.returnDelay = 0.2f;
        Position(new Vector2(tile.transform.position.x, 0.7f), false);
        yield return Until(() => tile.State == CrumbleState.Absent);
        Assert.That(tile.GetComponent<BoxCollider2D>().enabled, Is.False);
        Position(new Vector2(tile.transform.position.x, -0.15f));
        yield return new WaitForSeconds(0.4f);
        Assert.That(tile.State, Is.EqualTo(CrumbleState.Absent), "Cannot reappear through a player");
        Position(new Vector2(3, 0.65f));
        yield return Until(() => tile.State == CrumbleState.Stable);
        Assert.That(tile.GetComponent<BoxCollider2D>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator PauseFreezesBeamAndCrumbleTimers()
    {
        var tile = Tile;
        Position(new Vector2(tile.transform.position.x, 0.7f), false);
        yield return Until(() => tile.State == CrumbleState.Warning);
        var beam = Beam;
        Menu.Pause();
        float beamTime = beam.Elapsed, tileTime = tile.Elapsed;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(beam.Elapsed, Is.EqualTo(beamTime));
        Assert.That(tile.Elapsed, Is.EqualTo(tileTime));
        Assert.That(tile.State, Is.EqualTo(CrumbleState.Warning));
        Menu.Resume();
        yield return null;
        yield return null;
        Assert.That(beam.Elapsed, Is.GreaterThan(beamTime));
    }

    [UnityTest]
    public IEnumerator RestartResetsTheActualLevelAndObstacleState()
    {
        var tile = Tile;
        Position(new Vector2(tile.transform.position.x, 0.7f), false);
        yield return Until(() => tile.State == CrumbleState.Warning);
        Menu.Pause();
        Assert.That(Menu.Restart(), Is.True);
        yield return Until(() => !Menu.IsLoading);
        yield return null;
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Lab));
        Assert.That(Tile.State, Is.EqualTo(CrumbleState.Stable));
        Assert.That(Beam.Elapsed, Is.LessThan(0.5f));
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(GameObject.FindGameObjectWithTag("Player").transform.position.x, Is.EqualTo(3).Within(0.1f));
    }

    [UnityTest]
    public IEnumerator LabCanBeCompletedUsingTeamForceBasedMovement()
    {
        Position(new Vector2(3, 0.65f), false);
        bool firstJump = false, secondJump = false;
        float timeout = Time.realtimeSinceStartup + 35;
        while (Menu.State == MenuState.Playing && Time.realtimeSinceStartup < timeout)
        {
            float x = player.position.x;
            var velocity = player.linearVelocity; velocity.x = 5;
            bool jump = false;
            bool grounded = Physics2D.Raycast(player.position, Vector2.down, 0.65f, LayerMask.GetMask("Ground"));
            if (!firstJump && x >= 9 && grounded) { jump = true; firstJump = true; }
            if (!secondJump && x >= 14.8f && grounded) { jump = true; secondJump = true; }
            foreach (var beam in Object.FindObjectsByType<PulseBeam>(FindObjectsSortMode.None))
            {
                float distance = beam.transform.position.x - x;
                if (distance > 0.9f && distance < 1.4f && beam.SafeTimeRemaining < 1)
                    velocity.x = 0;
            }
            // Recovery path remains jumpable with the same 20-unit jump impulse.
            if (x > 51 && player.position.y < -0.4f && grounded) jump = true;
            var movement = player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            movement.GetType().GetField("moveX", flags).SetValue(movement, velocity.x / 5);
            movement.GetType().GetField("jumpQueued", flags).SetValue(movement, jump);
            movement.SendMessage("FixedUpdate");
            yield return new WaitForFixedUpdate();
        }
        Assert.That(firstJump && secondJump, Is.True);
        Assert.That(Menu.State, Is.EqualTo(MenuState.Victory), "The demonstration route must be completable without teleporting or changing player stats");
    }
}
