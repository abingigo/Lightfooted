#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ThrowableLightTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject player;
    GameObject lamp;
    GameObject ground;
    Component interaction;
    Component light;
    Rigidbody2D body;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("Test player");
        player.transform.position = new Vector3(100, 100, 0);
        interaction = player.AddComponent(Type.GetType("PlayerThrowableLight, Assembly-CSharp", true));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lights/ThrowableLight.prefab");
        lamp = Object.Instantiate(prefab, player.transform.position + Vector3.right * 0.5f, Quaternion.identity);
        light = lamp.GetComponent(Type.GetType("ThrowableLight, Assembly-CSharp", true));
        body = lamp.GetComponent<Rigidbody2D>();

        Physics2D.SyncTransforms();
        player.SendMessage("TryPickUp");
        Assert.That(lamp.transform.parent, Is.EqualTo(player.transform));
    }

    [UnityTest]
    public IEnumerator DropsFromTheHeldPosition() => ReleaseLight(0);

    [UnityTest]
    public IEnumerator ThrowsLeft() => ReleaseLight(-1);

    [UnityTest]
    public IEnumerator ThrowsRight() => ReleaseLight(1);

    [UnityTest]
    public IEnumerator ChargeMovesTheLandingMarkerAndKeepsTheLastDirection()
    {
        ground = new GameObject("Test ground");
        ground.transform.position = player.transform.position + Vector3.down;
        var floor = ground.AddComponent<BoxCollider2D>();
        floor.size = new Vector2(20f, 1f);
        Physics2D.SyncTransforms();

        ThrowInput(-1f, pressed: true);
        Vector2 shortLanding = PredictLanding(out Vector2 shortImpulse);
        Assert.That(shortImpulse.x, Is.EqualTo(-6f).Within(0.01f));

        ThrowInput(1f);
        ThrowInput(0f);
        yield return new WaitForSeconds(0.9f);
        Assert.That(lamp.transform.parent, Is.EqualTo(player.transform));

        Vector2 chargedLanding = PredictLanding(out Vector2 chargedImpulse);
        Assert.That(chargedImpulse.x, Is.EqualTo(12f).Within(0.01f));
        Assert.That(chargedImpulse.y, Is.EqualTo(shortImpulse.y));
        Assert.That(chargedLanding.x - player.transform.position.x,
            Is.GreaterThan(player.transform.position.x - shortLanding.x));
        Assert.That(chargedLanding.y, Is.EqualTo(floor.bounds.max.y).Within(0.02f));

        ThrowInput(0f, released: true);
        Assert.That(lamp.transform.parent, Is.Null);
        var lampCollider = lamp.GetComponent<CircleCollider2D>();
        for (int i = 0; i < 60 && !lampCollider.IsTouching(floor); i++)
            yield return new WaitForFixedUpdate();

        Assert.That(lampCollider.IsTouching(floor), Is.True);
        Assert.That(body.position.x, Is.EqualTo(chargedLanding.x).Within(0.25f));
    }

    [UnityTest]
    public IEnumerator LosingFocusCancelsTheCharge()
    {
        ThrowInput(1f, pressed: true);
        player.SendMessage("OnApplicationFocus", false);
        ThrowInput(0f, released: true);
        yield return null;
        Assert.That(lamp.transform.parent, Is.EqualTo(player.transform));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(lamp);
        Object.Destroy(player);
        Object.Destroy(ground);
        yield return null;
    }

    IEnumerator ReleaseLight(int direction)
    {
        var lightType = light.GetType();
        var heldPosition = lamp.transform.position;

        ThrowInput(direction, pressed: true, released: direction != 0);
        Assert.That(lamp.transform.parent, Is.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        Assert.That(lightType.GetProperty("CanPickUp").GetValue(light), Is.False);
        Assert.That(lightType.GetProperty("HasBeenThrown").GetValue(light), Is.EqualTo(direction != 0));

        if (direction == 0)
            Assert.That(lamp.transform.position, Is.EqualTo(heldPosition));

        yield return new WaitForFixedUpdate();
        if (direction == 0)
        {
            Assert.That(body.linearVelocity.x, Is.Zero.Within(0.001f));
            Assert.That(body.linearVelocity.y, Is.LessThan(0f));
        }
        else
        {
            Assert.That(body.linearVelocity.x * direction, Is.GreaterThan(0f));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));
        }
    }

    void ThrowInput(float direction, bool pressed = false, bool released = false)
    {
        interaction.GetType().GetMethod("UpdateThrowInput", Private)
            .Invoke(interaction, new object[] { direction, pressed, released });
    }

    Vector2 PredictLanding(out Vector2 impulse)
    {
        var launch = new object[] { Vector2.zero, Vector2.zero };
        interaction.GetType().GetMethod("GetThrow", Private).Invoke(interaction, launch);
        impulse = (Vector2)launch[1];
        var path = new List<Vector2>();
        var preview = new object[] { launch[0], impulse, player.transform, path, Vector2.zero };
        Assert.That(light.GetType().GetMethod("PredictThrow", Private).Invoke(light, preview), Is.True);
        Assert.That(path, Is.Not.Empty);
        return (Vector2)preview[4];
    }
}
#endif
