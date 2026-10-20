#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UI;

public class AssemblyReferenceTests
{
    [Serializable]
    class Definition
    {
        public string name;
        public string[] references;
    }

    [TestCase("Assets/Johnny/UI/LightPlatformer.UI.asmdef")]
    [TestCase("Assets/Johnny/Obstacles/LightPlatformer.Obstacles.asmdef")]
    [TestCase("Assets/Johnny/Tests/PlayMode/LightPlatformer.UI.Tests.asmdef")]
    public void ReferencesResolveAndDeclareTheUiBaseTypes(string path)
    {
        var definition = JsonUtility.FromJson<Definition>(File.ReadAllText(path));
        Assert.That(definition.references, Is.Not.Null, path);
        foreach (string reference in definition.references)
        {
            string target = reference.StartsWith("GUID:", StringComparison.Ordinal)
                ? AssetDatabase.GUIDToAssetPath(reference.Substring(5))
                : CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(reference);
            Assert.That(target, Is.Not.Null.And.Not.Empty,
                definition.name + " has an unresolved reference: " + reference);
        }

        // Unity can supply UI types implicitly even when an explicit reference is wrong.
        string uiAssembly = typeof(Button).Assembly.GetName().Name;
        Assert.That(definition.references, Does.Contain(uiAssembly),
            definition.name + " must declare the assembly containing its UI base types.");
    }
}
#endif
