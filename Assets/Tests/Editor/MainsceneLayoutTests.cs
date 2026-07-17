using NUnit.Framework;
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

            var slot = canvas.transform.Find("MainContent/SlotMachinePanel").GetComponent<RectTransform>();
            var shop = canvas.transform.Find("MainContent/SerenayShopPanel").GetComponent<RectTransform>();
            Assert.That(slot, Is.Not.Null);
            Assert.That(shop, Is.Not.Null);
            Assert.That(slot.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(slot.anchorMax.x, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(shop.anchorMin.x, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(shop.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(slot.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(slot.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(shop.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(shop.offsetMax, Is.EqualTo(Vector2.zero));

            Assert.That(Object.FindObjectOfType<EventSystem>(), Is.Not.Null);
            Assert.That(canvas.transform.Find("MainContent/SlotMachinePanel/SlotGrid"), Is.Not.Null);
            var batchControls = canvas.transform.Find("MainContent/SlotMachinePanel/BatchControls");
            var offerList = canvas.transform.Find("MainContent/SerenayShopPanel/OfferList");
            Assert.That(batchControls, Is.Not.Null);
            Assert.That(offerList, Is.Not.Null);
            Assert.That(batchControls.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(offerList.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(batchControls.childCount, Is.EqualTo(3));
            Assert.That(offerList.childCount, Is.EqualTo(3));
        }
    }
}
