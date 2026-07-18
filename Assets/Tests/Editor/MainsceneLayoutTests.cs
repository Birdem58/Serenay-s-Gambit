using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SerenaysGambit.Tests
{
    public sealed class MainsceneLayoutTests
    {
        [Test]
        public void MainContentKeepsTheResponsiveSeventyFiveTwentyFiveSplit()
        {
            var canvas = GameObject.Find("GameCanvas");
            Assert.That(canvas, Is.Not.Null, "Mainscene must include the GameCanvas.");

            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));

            var shelf = canvas.transform.Find("MainContent/VerticalShelfPanel").GetComponent<RectTransform>();
            var slot = canvas.transform.Find("MainContent/SlotMachinePanel").GetComponent<RectTransform>();
            var shop = canvas.transform.Find("MainContent/SerenayShopPanel").GetComponent<RectTransform>();
            Assert.That(shelf, Is.Not.Null);
            Assert.That(slot, Is.Not.Null);
            Assert.That(shop, Is.Not.Null);

            Assert.That(shelf.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(shelf.anchorMax.x, Is.EqualTo(0.20f).Within(0.001f));
            Assert.That(slot.anchorMin.x, Is.EqualTo(0.20f).Within(0.001f));
            Assert.That(slot.anchorMax.x, Is.EqualTo(0.80f).Within(0.001f));
            Assert.That(shop.anchorMin.x, Is.EqualTo(0.80f).Within(0.001f));
            Assert.That(shop.anchorMax.x, Is.EqualTo(1f).Within(0.001f));

            Assert.That(shelf.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(shelf.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(slot.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(slot.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(shop.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(shop.offsetMax, Is.EqualTo(Vector2.zero));

            var shelfBackground = shelf.Find("Background");
            Assert.That(shelfBackground, Is.Not.Null, "VerticalShelfPanel needs an explicit background child.");
            Assert.That(shelfBackground.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(shelfBackground.GetComponent<Image>(), Is.Not.Null);
            Assert.That(shelfBackground.GetComponent<Canvas>(), Is.Null,
                "The shelf background must stay on the parent screen-space canvas.");
            Assert.That(shelfBackground.GetComponent<LayoutElement>().ignoreLayout, Is.True);

            var layeredTextNames = new[]
            {
                "TargetText",
                "CashText",
                "PayoutText",
                "RoundText",
                "RollsText",
                "OwnedUpgradesHeading",
                "OrgansHeading"
            };

            foreach (var textName in layeredTextNames)
            {
                var textContainer = shelf.Find(textName);
                Assert.That(textContainer, Is.Not.Null);
                Assert.That(textContainer.GetComponent<TextMeshProUGUI>(), Is.Null,
                    textName + " should be a layout container so its background can render in front of the shelf.");

                var textBackground = textContainer.Find("Background");
                var textLabel = textContainer.Find("Label");
                Assert.That(textBackground, Is.Not.Null);
                Assert.That(textLabel, Is.Not.Null);
                Assert.That(textBackground.GetSiblingIndex(), Is.EqualTo(0));
                Assert.That(textLabel.GetSiblingIndex(), Is.EqualTo(1));
                Assert.That(textBackground.GetComponent<Canvas>(), Is.Null);
                Assert.That(textBackground.GetComponent<Image>(), Is.Not.Null);
                Assert.That(textLabel.GetComponent<TextMeshProUGUI>(), Is.Not.Null);
            }

            var backgroundOnlyNames = new[] { "BatchControls", "OwnedUpgradesLayout", "OrgansText" };
            foreach (var containerName in backgroundOnlyNames)
            {
                var textBackground = shelf.Find(containerName + "/Background");
                Assert.That(textBackground, Is.Not.Null);
                Assert.That(textBackground.GetSiblingIndex(), Is.EqualTo(0));
                Assert.That(textBackground.GetComponent<Canvas>(), Is.Null);
                Assert.That(textBackground.GetComponent<Image>(), Is.Not.Null);
                Assert.That(textBackground.GetComponent<LayoutElement>().ignoreLayout, Is.True);
            }

            Assert.That(Object.FindObjectOfType<EventSystem>(), Is.Not.Null);
            Assert.That(canvas.transform.Find("MainContent/SlotMachinePanel/SlotGrid"), Is.Not.Null);
            var leverPanel = canvas.transform.Find("MainContent/SlotMachinePanel/LeverPanel");
            Assert.That(leverPanel, Is.Not.Null);
            var batchControls = canvas.transform.Find("MainContent/VerticalShelfPanel/BatchControls");
            var offerList = canvas.transform.Find("MainContent/SerenayShopPanel/OfferList");
            Assert.That(batchControls, Is.Not.Null);
            Assert.That(offerList, Is.Not.Null);
            Assert.That(batchControls.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(offerList.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            var buttonsRow = batchControls.Find("ButtonsRow");
            Assert.That(buttonsRow, Is.Not.Null);
            Assert.That(buttonsRow.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(buttonsRow.childCount, Is.EqualTo(3));
            Assert.That(offerList.childCount, Is.EqualTo(3));
        }

        [Test]
        public void EndGameOverlaysIncludeRunStatsSummaryText()
        {
            var canvas = GameObject.Find("GameCanvas");
            Assert.That(canvas, Is.Not.Null, "Mainscene must include the GameCanvas.");

            var gameOverStats = canvas.transform.Find("GameOverOverlay/RunStatsText");
            var victoryStats = canvas.transform.Find("VictoryOverlay/RunStatsText");

            Assert.That(gameOverStats, Is.Not.Null);
            Assert.That(victoryStats, Is.Not.Null);
            Assert.That(gameOverStats.GetComponent<TextMeshProUGUI>(), Is.Not.Null);
            Assert.That(victoryStats.GetComponent<TextMeshProUGUI>(), Is.Not.Null);
        }

        [Test]
        public void OwnedUpgradesUseAnIconGridAndReusablePrefab()
        {
            var canvas = GameObject.Find("GameCanvas");
            Assert.That(canvas, Is.Not.Null, "Mainscene must include the GameCanvas.");

            var shelf = canvas.transform.Find("MainContent/VerticalShelfPanel");
            Assert.That(shelf, Is.Not.Null);
            Assert.That(shelf.Find("OwnedUpgradesText"), Is.Null);
            Assert.That(shelf.Find("OwnedUpgradesHeading").GetComponentInChildren<TextMeshProUGUI>(), Is.Not.Null);

            var layout = shelf.Find("OwnedUpgradesLayout");
            Assert.That(layout, Is.Not.Null);
            var grid = layout.GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(5));
            Assert.That(grid.cellSize, Is.EqualTo(new Vector2(56f, 56f)));
            Assert.That(grid.spacing, Is.EqualTo(new Vector2(8f, 8f)));

            var tooltip = canvas.transform.Find("UpgradeTooltip");
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip.GetComponent<UpgradeTooltip>(), Is.Not.Null);
            Assert.That(tooltip.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(tooltip.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/upgrades.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<OwnedUpgradeView>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<UpgradeTooltipTrigger>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Image>(), Is.Not.Null);
        }

        [Test]
        public void OrgansLayoutContainerIsPositionedCorrectly()
        {
            var canvas = GameObject.Find("GameCanvas");
            Assert.That(canvas, Is.Not.Null, "Mainscene must include the GameCanvas.");

            var organsTransform = canvas.transform.Find("MainContent/VerticalShelfPanel/OrgansText");
            Assert.That(organsTransform, Is.Not.Null, "Expected OrgansText/Layout in VerticalShelfPanel.");

            var rect = organsTransform.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);

            var father = organsTransform.parent;
            Assert.That(father.name, Is.EqualTo("VerticalShelfPanel"));
        }
    }
}
