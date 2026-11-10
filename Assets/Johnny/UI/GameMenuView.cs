using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
 using Core;

namespace LightPlatformer.UI
{
    public sealed class GameMenuView : MonoBehaviour
    {
        static readonly Color Background = new Color32(17, 22, 29, 255);
        static readonly Color Panel = new Color32(29, 37, 47, 255);
        static readonly Color Muted = new Color32(161, 177, 193, 255);
        static readonly Color Accent = new Color32(165, 234, 203, 255);
        readonly List<Button> focusOrder = new List<Button>();
        readonly List<Button> levelButtons = new List<Button>();
        readonly List<string> levelPaths = new List<string>();
        GameObject canvasObject;
        RectTransform canvas;
        GameMenuController controller;
        TMP_Text errorText;
        TMP_Text levelTitle;
        TMP_Text levelDescription;
        TMP_Text levelCoins;
        TMP_Text playLabel;
        Button playButton;
        LevelEntry selected;
        string selectedPath;
        GameObject controls;
        GameObject mainContent;
        Button focusBeforeControls;

        public void Render(GameMenuController owner, IReadOnlyList<LevelEntry> levelOverride = null)
        {
            controller = owner;
            if (canvasObject != null) { canvasObject.SetActive(false); Destroy(canvasObject); }
            focusOrder.Clear(); levelButtons.Clear(); levelPaths.Clear();
            errorText = null; controls = null;
            canvasObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<RectTransform>();
            var uiCanvas = canvasObject.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (owner.State == MenuState.Playing) RenderHud();
            else if (owner.State == MenuState.LevelSelect) RenderLevelSelect(levelOverride ?? LevelCatalog.ReadBuildLevels());
            else if (owner.State == MenuState.Loading) RenderLoading();
            else if (owner.State == MenuState.Victory)
                RenderScoreboard(owner.VictoryScore);
            else
                RenderPauseOrResult(owner.State);
            Canvas.ForceUpdateCanvases();
            if (owner.State == MenuState.Playing) EventSystem.current?.SetSelectedGameObject(null);
            else FocusFirst();
        }

        public void RenderLevelSelect(IReadOnlyList<LevelEntry> levels)
        {
            Fill(canvas, Background, "Menu background");
            var shell = Box(canvas, "Level selection", 880, 520, 0, 0);
            mainContent = shell.gameObject;
            Text(shell, "LIGHTFOOTED", 14, Accent, 0, 242, 864, 24);
            Text(shell, "Stay in the light or fall off the platform", 34, Color.white, 0, 204, 864, 48);
            var listPanel = Box(shell, "Levels", 336, 326, -264, -32, Panel);
            Text(listPanel, "LEVELS", 13, Muted, 0, 139, 296, 24);
            var scrollRoot = Box(listPanel, "Level list", 304, 232, 0, 0);
            var viewport = Fill(scrollRoot, Color.clear, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;

            var detail = Box(shell, "Selected level", 512, 326, 176, -32, Panel);
            Text(detail, "YOUR NEXT CHALLENGE", 13, Accent, 0, 131, 448, 24);
            levelTitle = Text(detail, "", 32, Color.white, 0, 89, 448, 46);
            levelDescription = Text(detail, "", 18, Muted, 0, 28, 448, 68);
            levelCoins = Text(detail, "", 15, Muted, 0, -48, 448, 48);
            playButton = Button(detail, "Play", 448, 52, 0, -112, () =>
            {
                if (selected != null) controller.LoadLevel(selected.ScenePath);
            }, true);
            playLabel = playButton.GetComponentInChildren<TMP_Text>();
            // Level cards come first in keyboard navigation, followed by Play and Controls.
            focusOrder.Remove(playButton);
            selected = null;
            for (int i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                var button = Button(content, i.ToString("00") + "    " + entry.Title, 296, 60, 0, 0, () => SelectLevel(entry));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
                button.gameObject.AddComponent<MenuFocusScroll>().Scroll = scroll;
                levelButtons.Add(button);
                levelPaths.Add(entry.ScenePath);
                if (selectedPath == entry.ScenePath) selected = entry;
            }
            if (selected == null && levels.Count > 0) selected = levels[0];
            focusOrder.Add(playButton);
            Text(listPanel, levels.Count == 1 ? "1 level available" : levels.Count + " levels available", 14, Muted, 0, -139, 296, 24);
            Text(shell, levels.Count == 0 ? "Your next adventure is on its way." : "More challenges are on their way.", 14, Muted, -70, -222, 720, 28);
            Button(shell, "Controls", 116, 36, 374, -222, ToggleControls);
            errorText = Text(shell, "", 15, new Color32(255, 180, 168, 255), 0, -263, 864, 28);
            if (selected != null) SelectLevel(selected);
            else
            {
                levelTitle.text = "No levels yet";
                levelDescription.text = "There are no playable levels available right now. Please check back soon.";
                playLabel.text = "No level available";
                playLabel.color = Muted;
                playButton.interactable = false;
            }
        }

        void SelectLevel(LevelEntry entry)
        {
            selected = entry; selectedPath = entry.ScenePath;
            levelTitle.text = entry.Title; levelDescription.text = entry.Description;
            if (levelCoins != null) levelCoins.text = LevelCatalog.CoinSummary(entry.ScenePath);
            playLabel.text = "Play " + entry.Title;
            for (int i = 0; i < levelButtons.Count; i++)
            {
                var label = levelButtons[i].GetComponentInChildren<TMP_Text>();
                bool active = levelPaths[i] == entry.ScenePath;
                label.color = active ? Accent : Color.white;
                var colors = levelButtons[i].colors;
                colors.normalColor = active ? new Color32(45, 78, 71, 255) : new Color32(43, 54, 67, 255);
                levelButtons[i].colors = colors;
            }
        }
        void OnGUI()
        {
            if (controller == null || controller.State != MenuState.Playing) return;
            int seconds = Mathf.FloorToInt(LevelStats.AttemptSeconds);

            GUI.Box(new Rect(16f, 64f, 150f, 28f),
                "Time passed: " + (seconds / 60).ToString("00") + ":" +
                (seconds % 60).ToString("00"));
        }
        void RenderHud()
        {
            var bar = Box(canvas, "Gameplay menu", 920, 44, 0, 0);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1); bar.anchoredPosition = new Vector2(0, -32);
            var scene = LevelCatalog.Describe(SceneManager.GetActiveScene().path);
            Text(bar, scene.Title, 16, Color.white, -330, 0, 240, 36);
            var coins = Text(bar, "", 16, Accent, 0, 0, 200, 36);
            coins.gameObject.name = "Coin counter";
            coins.alignment = TextAlignmentOptions.Center;
            coins.gameObject.AddComponent<CoinCounter>();
            var pause = Button(bar, "Pause  [Esc]", 144, 40, 378, 0, controller.Pause);
            pause.navigation = new Navigation { mode = Navigation.Mode.None };

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            foreach (var behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(behaviour is ILanternStatus lantern)) continue;
                var meter = new GameObject("Lantern meter", typeof(RectTransform)).AddComponent<LanternMeter>();
                meter.transform.SetParent(canvas, false);
                meter.Bind(lantern);
                break;
            }
        }
        void RenderScoreboard(LevelScoreboard.Result score)
        {
            Fill(canvas, new Color(0.035f, 0.05f, 0.065f, 0.93f), "Modal backdrop");
            var panel = Box(canvas, "Level scoreboard", 800, 500, 0, 0, Panel);
            mainContent = panel.gameObject;

            string path = SceneManager.GetActiveScene().path;
            string title = LevelCatalog.Describe(path).Title;
            Text(panel, "LIGHTFOOTED / " + title, 13, Accent, 0, 215, 728, 24);
            Text(panel, "Level complete", 34, Color.white, 0, 175, 728, 50);

            bool hasCoins = score.current.coinTotal > 0;
            int columns = 3;
            if (hasCoins) columns = 4;

            float width = 728f / columns;
            var stats = Box(panel, "This run", 728, 76, 0, 105, Background);

            ScoreStat(stats, "YOUR TIME", score.current.TimeLabel(), width, 0);
            ScoreStat(stats, "PERSONAL RANK", $"#{score.rank} of {score.totalRuns}", width, 1);
            ScoreStat(stats, "DEATHS", score.current.deaths.ToString(), width, 2);
            if (hasCoins)
                ScoreStat(stats, "COINS", score.current.CoinLabel(), width, 3);

            var table = Box(panel, "Personal best times", 456, 226, -136, -61, Background);
            Text(table, "PERSONAL BEST TIMES", 13, Accent, 0, 92, 424, 22);
            ScoreRow(table, 62, "Rank", "Time", "Deaths", "Coins", hasCoins, Muted, 13);

            for (int i = 0; i < score.bestRuns.Count; i++)
            {
                var run = score.bestRuns[i];
                int rank = i + 1;
                string label = "#" + rank;
                Color color = Color.white;

                if (rank == score.rank)
                {
                    label += "  This run";
                    color = Accent;
                }

                ScoreRow(table, 34 - i * 29, label, run.TimeLabel(),
                    run.deaths.ToString(), run.CoinLabel(), hasCoins, color, 16);
            }

            var levels = LevelCatalog.ReadBuildLevels();
            int index = levels.FindIndex(level => level.ScenePath == path);

            if (index >= 0 && index + 1 < levels.Count)
            {
                Button(panel, "Next level", 240, 44, 244, 13, () => controller.NextLevel(), true);
                Button(panel, "Restart level", 240, 44, 244, -40, () => controller.Restart());
            }
            else
            {
                Button(panel, "Play again", 240, 44, 244, -40, () => controller.Restart(), true);
            }

            Button(panel, "Level select", 240, 44, 244, -93, () => controller.GoToMenu());
            Button(panel, "Controls", 240, 44, 244, -146, ToggleControls);

            Text(panel, "Personal times are stored on this device.", 14, Muted, 0, -196, 728, 24);
            errorText = Text(panel, "", 14, new Color32(255, 180, 168, 255), 0, -226, 728, 28);
        }

        static void ScoreStat(Transform parent, string label, string value, float width, int index)
        {
            float x = -364 + width * (index + 0.5f);
            Text(parent, label, 12, Muted, x, 17, width - 32, 20);
            Text(parent, value, 25, Color.white, x, -11, width - 32, 34);
        }

        static void ScoreRow(Transform parent, float y, string rank, string time,
            string deaths, string coins, bool hasCoins, Color color, float size)
        {
            Text(parent, rank, size, color, -157, y, 110, 26);
                Text(parent, time, size, color, -30, y, 132, 26);
            Text(parent, deaths, size, color, 77, y, 70, 26);

            if (hasCoins)
                 Text(parent, coins, size, color, 173, y, 78, 26);
        }
        void RenderPauseOrResult(MenuState state)
        {
            Fill(canvas, new Color(0.035f, 0.05f, 0.065f, 0.93f), "Modal backdrop");

            bool victory = state == MenuState.Victory;
            bool paused = state == MenuState.Paused;
            bool hasSave = (paused || state == MenuState.GameOver) && CheckpointProgress.HasCheckpointForScene(SceneManager.GetActiveScene().path);

            int current = LevelCatalog.ReadBuildLevels().FindIndex(entry => entry.ScenePath == SceneManager.GetActiveScene().path);
            bool next = victory && current >= 0 && current + 1 < LevelCatalog.ReadBuildLevels().Count;

            var actions = new List<(string label, Action action)>();
            if (paused) actions.Add(("Resume game", controller.Resume));
            else if (next) actions.Add(("Next level", () => controller.NextLevel()));
            else actions.Add((victory ? "Play again" : hasSave ? "Respawn at checkpoint" : "Restart level", () => controller.Restart()));

            if (paused && hasSave) actions.Add(("Respawn at checkpoint", () => controller.Restart()));
            if (hasSave) actions.Add(("Restart from beginning", () => controller.RestartFromBeginning()));
            else if (paused || next) actions.Add(("Restart level", () => controller.Restart()));
            actions.Add(("Level select", () => controller.GoToMenu()));

            float half = Mathf.Max(0, actions.Count - 3) * 30f;
            var panel = Box(canvas, "Pause and results", 464, 490 + half * 2f, 0, 0, Panel);
            mainContent = panel.gameObject;
            Text(panel, "LIGHTFOOTED", 13, Accent, 0, half + 207, 400, 24);
            Text(panel, paused ? "Take a breath" : victory ? "Level complete" : "Try again", 34, Color.white, 0, half + 158, 400, 50);
            Text(panel, paused ? "Your game is paused." : victory ? "You found your way through." : "A new attempt starts with a little light.", 17, Muted, 0, half + 111, 400, 42);

            float y = half + 43;
            for (int i = 0; i < actions.Count; i++, y -= 60)
                Button(panel, actions[i].label, 392, 48, 0, y, actions[i].action, i == 0);
            Button(panel, "Controls", 392, 42, 0, y + 3, ToggleControls);

            Text(panel, paused ? "Esc or P to resume  ·  Deaths: " + CheckpointProgress.deathTimes : victory && !next ? "All available levels complete. Thanks for playing!" : state == MenuState.GameOver ? "Deaths: " + CheckpointProgress.deathTimes : "You can retry as often as you like.", 14, Muted, 0, -half - 188, 400, 42);
            errorText = Text(panel, "", 14, new Color32(255, 180, 168, 255), 0, -half - 224, 416, 32);
        }

        void RenderLoading()
        {
            Fill(canvas, Background, "Loading background");
            Text(canvas, "Finding the light...", 32, Color.white, 0, 22, 600, 52);
            Text(canvas, "Opening your level", 16, Muted, 0, -24, 600, 32);
        }

        void ToggleControls()
        {
            if (controls != null)
            {
                controls.SetActive(false); Destroy(controls); controls = null;
                mainContent.SetActive(true);
                if (focusBeforeControls != null) EventSystem.current?.SetSelectedGameObject(focusBeforeControls.gameObject);
                return;
            }
            focusBeforeControls = EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>();
            mainContent.SetActive(false);
            var overlay = Fill(canvas, Background, "Controls screen"); controls = overlay.gameObject;
            var panel = Box(overlay, "Controls", 620, 570, 0, 0, Panel);
            Text(panel, "A little light goes a long way", 28, Color.white, 0, 233, 560, 56);
            Text(panel, "CONTROLS", 13, Accent, 0, 180, 560, 24);
            Text(panel, "A / D                       Move\nSpace                       Jump\nLeft Shift                  Dash\nClick a light, then F     Rotate light\nE / Q                          Pick up / throw light\nR                                Pick up / insert battery\nEsc / P                      Pause or resume", 20, Color.white, 0, 25, 560, 280);
            Text(panel, "Stay in the light to remain solid.\nTab / Shift+Tab moves between buttons; Enter selects.", 16, Muted, 0, -170, 560, 62);
            var back = Button(panel, "Back", 432, 48, 0, -233, ToggleControls, true);
            EventSystem.current?.SetSelectedGameObject(back.gameObject);
        }

        public bool CloseControls()
        {
            if (controls == null) return false;
            ToggleControls();
            return true;
        }

        public void ShowError(string message) { if (errorText != null) errorText.text = message; }

        void FocusFirst()
        {
            foreach (var button in focusOrder)
                if (button != null && button.IsActive() && button.interactable)
                { EventSystem.current?.SetSelectedGameObject(button.gameObject); return; }
        }

        public void MoveFocus(bool backwards)
        {
            if (controller.State == MenuState.Playing) return;
            var available = focusOrder.FindAll(button => button != null && button.IsActive() && button.interactable);
            if (available.Count == 0 || EventSystem.current == null) return;
            int i = available.FindIndex(button => button.gameObject == EventSystem.current.currentSelectedGameObject);
            int next = i < 0 ? 0 : (i + (backwards ? -1 : 1) + available.Count) % available.Count;
            EventSystem.current.SetSelectedGameObject(available[next].gameObject);
        }

        static RectTransform Box(Transform parent, string name, float width, float height, float x, float y, Color? color = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y);
            if (color.HasValue) { var image = rect.gameObject.AddComponent<Image>(); image.color = color.Value; }
            return rect;
        }

        static RectTransform Fill(Transform parent, Color color, string name)
        {
            var rect = Box(parent, name, 0, 0, 0, 0, color);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static TMP_Text Text(Transform parent, string value, float size, Color color, float x, float y, float width, float height)
        {
            var rect = Box(parent, "Text " + value, width, height, x, y);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.font = TMP_Settings.defaultFontAsset; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        Button Button(Transform parent, string label, float width, float height, float x, float y, Action action, bool primary = false)
        {
            var rect = Box(parent, label, width, height, x, y, Color.white);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = primary ? Accent : new Color32(43, 54, 67, 255);
            colors.highlightedColor = primary ? new Color32(193, 249, 224, 255) : new Color32(65, 85, 101, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(114, 183, 153, 255);
            colors.disabledColor = new Color32(44, 48, 53, 255);
            colors.fadeDuration = 0.08f; button.colors = colors;
            var text = Text(rect, label, 17, primary ? Background : Color.white, 0, 0, width - 32, height - 4);
            text.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action());
            focusOrder.Add(button);
            return button;
        }
    }

    // Keep focused cards visible when arrow keys / Tab move beyond the viewport.
    public sealed class MenuFocusScroll : MonoBehaviour, ISelectHandler
    {
        public ScrollRect Scroll;
        public void OnSelect(BaseEventData eventData)
        {
            if (Scroll == null) return;
            Canvas.ForceUpdateCanvases();
            float contentHeight = Scroll.content.rect.height;
            float viewportHeight = Scroll.viewport.rect.height;
            if (contentHeight <= viewportHeight) return;
            var rect = (RectTransform)transform;
            float top = -rect.anchoredPosition.y - rect.rect.height * (1 - rect.pivot.y);
            float visibleTop = Scroll.content.anchoredPosition.y;
            float target = top < visibleTop ? top : top + rect.rect.height > visibleTop + viewportHeight ? top + rect.rect.height - viewportHeight : visibleTop;
            Scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(target / (contentHeight - viewportHeight));
        }
    }
}
