using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    [Serializable]
    public sealed class SceneReference
    {
        [SerializeField] private string guid;
        [SerializeField] private string path;

        public string Path => path;

        public bool IsInBuild => !string.IsNullOrEmpty(path) && SceneUtility.GetBuildIndexByScenePath(path) >= 0;
    }
}
