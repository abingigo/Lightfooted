using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using LightPlatformer.Obstacles;
using LightPlatformer.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class TeamIntegrationTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    MonoBehaviour movement;
    Rigidbody2D player;
    LightStoppingPlatform platform;
    GameMenuController Menu => GameMenuController.Instance;
    Type TeamType(string name) => movement.GetType().Assembly.GetType(name, true);
    void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/Johnny/LightPlatformLab.unity");
        yield return null;
        yield return null;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        movement = player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController");
        movement.enabled = false;
        platform = Object.FindFirstObjectByType<LightStoppingPlatform>();
    }

    IEnumerator Frames(int count = 3)
    {
        for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        yield return null;
    }

    [UnityTest]
    public IEnumerator PauseAndResumeDiscardQueuedJumpAndDashBeforePhysics()
    {
        player.gravityScale = 0;
        player.linearVelocity = Vector2.zero;
        Set(movement, "moveX", 1f);
        Set(movement, "jumpQueued", true);
        Set(movement, "dashQueued", true);
        movement.GetType().GetField("isDashing").SetValue(movement, true);
        Menu.Pause();
        movement.SendMessage("FixedUpdate");
        Assert.That(Get(movement, "moveX"), Is.EqualTo(0f));
        Assert.That(Get(movement, "jumpQueued"), Is.False);
        Assert.That(Get(movement, "dashQueued"), Is.False);
        Assert.That(movement.GetType().GetField("isDashing").GetValue(movement), Is.False);
        Menu.Resume();
        Set(movement, "jumpQueued", true);
        movement.SendMessage("Update");
        Assert.That(Get(movement, "jumpQueued"), Is.False, "Resume-frame UI submit must not queue a jump");
        yield return Frames();
        Assert.That(player.linearVelocity, Is.EqualTo(Vector2.zero));
        Set(movement, "moveX", 1f);
        Set(movement, "jumpQueued", true);
        movement.SendMessage("FixedUpdate");
        yield return new WaitForFixedUpdate();
        Assert.That(player.linearVelocity.x, Is.GreaterThan(0));
        Assert.That(player.linearVelocity.y, Is.EqualTo(20f).Within(0.1f));
    }

    [UnityTest]
    public IEnumerator DemoUsesClickableEmitterMainCameraAndSelectionFeedback()
    {
        Assert.That(Camera.main, Is.Not.Null);
        var lamp = GameObject.Find("Controllable brake light");
        Assert.That(Camera.main.WorldToViewportPoint(player.position).x, Is.InRange(0f, 1f), "Spawn must be visible");
        Assert.That(Camera.main.WorldToViewportPoint(lamp.transform.position).x, Is.InRange(0f, 1f), "Brake lamp must be visible");
        var control = lamp.GetComponent(TeamType("ControllableLight"));
        var target = (Collider2D)control.GetType().GetField("clickCollider").GetValue(control);
        Assert.That(target.transform.parent, Is.EqualTo(lamp.transform));
        Assert.That(target.OverlapPoint(lamp.transform.position), Is.True);
        Assert.That(target.OverlapPoint(lamp.transform.position + Vector3.right * 3), Is.False);
        lamp.SendMessage("SetSelected", true);
        yield return null;
        Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(text => text.text == "SELECTED / PRESS F"), Is.True);
        lamp.SendMessage("SetDirection", (object)2);
        float deadline = Time.realtimeSinceStartup + 5;
        while (!platform.IsIlluminated && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(platform.IsIlluminated, Is.True);
        Menu.Pause();
        var rotation = lamp.transform.rotation;
        var interaction = player.GetComponent(TeamType("PlayerThrowableLight"));
        Set(interaction, "facing", -1f);
        interaction.SendMessage("Update");
        Assert.That(Get(interaction, "facing"), Is.EqualTo(-1f));
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(lamp.transform.rotation, Is.EqualTo(rotation));
    }

    [UnityTest]
    public IEnumerator TeamBatteryLightHoldsPlatformAndReleasesWhenRemovedOrDepleted()
    {
        var lamp = new GameObject("Battery brake"); lamp.transform.position = platform.transform.position;
        var rangeObject = new GameObject("LightRange"); rangeObject.transform.SetParent(lamp.transform, false);
        var range = rangeObject.AddComponent<CircleCollider2D>(); range.radius = 1;
        var powered = lamp.AddComponent(TeamType("BatteryPoweredLight"));
        var batteryObject = new GameObject("Test battery");
        batteryObject.AddComponent<BoxCollider2D>();
        var item = batteryObject.AddComponent(TeamType("BatteryItem"));
        var insert = powered.GetType().GetMethod("Insert");
        Assert.That(insert.Invoke(powered, new object[] { item }), Is.True);
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True);
        Menu.Pause();
        float energy = (float)item.GetType().GetProperty("Energy").GetValue(item);
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(item.GetType().GetProperty("Energy").GetValue(item), Is.EqualTo(energy));
        Menu.Resume(); yield return null; yield return null;
        powered.GetType().GetMethod("Remove").Invoke(powered, null);
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.False);
        insert.Invoke(powered, new object[] { item });
        Set(powered, "energyPerSecond", 1000f);
        yield return new WaitForSeconds(0.1f);
        yield return Frames();
        Assert.That(range.enabled, Is.False);
        Assert.That(platform.IsIlluminated, Is.False);
    }

    [UnityTest]
    public IEnumerator TeamThrowableLightOnlyBrakesWhileItsRangeIsSimulated()
    {
        var lamp = new GameObject("Throwable brake"); lamp.transform.position = platform.transform.position;
        var body = lamp.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
        var physical = lamp.AddComponent<BoxCollider2D>(); physical.size = Vector2.one * 0.2f;
        var rangeObject = new GameObject("LightRange"); rangeObject.transform.SetParent(lamp.transform, false);
        var range = rangeObject.AddComponent<CircleCollider2D>(); range.radius = 1;
        var throwable = lamp.AddComponent(TeamType("ThrowableLight"));
        yield return Frames();
        Assert.That(platform.IsIlluminated, Is.True);
        throwable.GetType().GetMethod("PickUp").Invoke(throwable, new object[] { player.transform, Vector3.up * 2 });
        yield return Frames();
        Assert.That(body.simulated, Is.False);
        Assert.That(platform.IsIlluminated, Is.False);
        throwable.GetType().GetMethod("Throw").Invoke(throwable, new object[] { (Vector2)platform.transform.position, Vector2.zero, 0.35f });
        yield return Frames();
        Assert.That(body.simulated, Is.True);
        Assert.That(platform.IsIlluminated, Is.True);
    }
}
