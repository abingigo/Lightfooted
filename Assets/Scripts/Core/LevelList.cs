using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Lightfooted/Level List", fileName = "LevelList")]
    public sealed class LevelList : ScriptableObject
    {
        [Serializable]
        public sealed class Level
        {
            [SerializeField] private SceneReference scene = new();

            [Tooltip("Shown in the level select and HUD. Empty = derived from the scene name.")] [SerializeField]
            private string title;

            [SerializeField, TextArea(2, 4)] private string description;

            [Tooltip("Challenge coins in the scene. Filled in automatically when the scene is saved.")] [SerializeField]
            private int coinTotal;

            public string ScenePath => scene.Path;
            public string Title => title;
            public string Description => description;
            public int CoinTotal => coinTotal;
            public bool IsPlayable => scene.IsInBuild;
        }

        [Tooltip("The level select scene. Go To Menu loads it.")] [SerializeField]
        private SceneReference menu = new();

        [Tooltip("Levels in play order.")] [SerializeField]
        private Level[] levels = { };

        public string MenuPath => menu.Path;
        public IReadOnlyList<Level> Levels => levels;
    }
}
