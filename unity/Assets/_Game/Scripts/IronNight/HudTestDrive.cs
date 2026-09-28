using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The test drive: a tile on the title under the war bonds with the strongest tank the player does not own yet (her
    /// picture from the hangar, TEST DRIVE, her name), and its sheet: the tank large, what she is, DRIVE HER TONIGHT for
    /// a rewarded ad (once a day), and her price to keep her. Lent, she stands on the turntable, the tile says TONIGHT,
    /// and she leads the next night; then she goes back to the depot.
    /// </summary>
    public partial class Hud
    {
        GameObject driveBtn, driveSheet; Transform driveBody; RawImage drivePic; Text driveEyebrow, driveName, driveLine;

        /// <summary>The tank of the test drive: the one lent for the next night, or today's offer; null without either.</summary>
        static Depot.LeaderChoice DriveTank()
        {
            var id = Depot.TestDriveId; return id != null ? System.Array.Find(Depot.Leaders, x => x.id == id) : Depot.TestDriveOffer;
        }

        void BuildTestDriveButton()
        {
            const float W = 190f;
            var b = MakeButton(titleSheet.transform, "", new Vector2(1f, 1f), new Vector2(-145, -590), new Vector2(W, W), 20, ShowTestDrive); driveBtn = b;
            b.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.85f); b.transform.Find("Label").gameObject.SetActive(false);
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(b.transform, false);
            var mrt = mask.GetComponent<RectTransform>(); mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f); mrt.sizeDelta = new Vector2(W - 8f, W - 8f); mrt.anchoredPosition = Vector2.zero;
            var pg = new GameObject("Pic", typeof(RectTransform), typeof(RawImage)); pg.transform.SetParent(mask.transform, false); drivePic = pg.GetComponent<RawImage>(); drivePic.raycastTarget = false;
            var prt = drivePic.rectTransform; prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f); prt.sizeDelta = new Vector2(W - 8f, W - 8f); drivePic.uvRect = new Rect(0.215f, 0f, 0.57f, 1f);   // the middle of her portrait, square
            var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(W - 8f, 120f), new Color(0.02f, 0.02f, 0.03f, 0.95f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.2f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
            var top = MakeImage(mask.transform, "Top", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(W - 8f, 44f), new Color(0.02f, 0.02f, 0.03f, 0.75f)); top.rectTransform.pivot = new Vector2(0.5f, 1f);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, W), new Color(1f, 0.8f, 0.35f, 0.4f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            driveEyebrow = MakeText(b.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -8), TextAnchor.UpperCenter, 17, OpAmber); driveEyebrow.font = LabelFont(); driveEyebrow.rectTransform.sizeDelta = new Vector2(W - 10f, 26f);
            driveName = MakeText(b.transform, "Name", new Vector2(0.5f, 0f), new Vector2(0, 32), TextAnchor.LowerCenter, 26, OpInk); driveName.rectTransform.sizeDelta = new Vector2(W - 14f, 34f);
            driveLine = MakeText(b.transform, "Line", new Vector2(0.5f, 0f), new Vector2(0, 8), TextAnchor.LowerCenter, 18, OpAmber); driveLine.font = BoldFont(); driveLine.rectTransform.sizeDelta = new Vector2(W - 10f, 26f);
        }

        /// <summary>The tile as the day stands: today's tank to try, the one lent for tonight, or none (all owned, or tried).</summary>
        void RefreshTestDriveButton()
        {
            if (driveBtn == null) return;
            var c = DriveTank(); driveBtn.SetActive(c != null); if (c == null) return;
            bool lent = Depot.TestDriveId == c.id;
            if (garage != null) drivePic.texture = garage.Portrait(VehicleSpec.ById(c.id));
            driveEyebrow.text = Spaced(lent ? "TONIGHT" : "TEST DRIVE");
            driveName.text = c.name.ToUpperInvariant(); Serif(driveName, 1f); driveName.color = OpInk; Fit(driveName, 14);
            driveLine.text = lent ? "yours for a night" : "watch an ad";
        }

        /// <summary>The test drive's sheet: the tank large, what she is, the night for an ad (lent already: the way into the
        /// night), and her price to keep her.</summary>
        void ShowTestDrive()
        {
            var c = DriveTank(); if (c == null) return;
            var spec = VehicleSpec.ById(c.id); bool lent = Depot.TestDriveId == c.id;
            if (driveSheet == null)
            {
                driveSheet = new GameObject("TestDrive", typeof(RectTransform), typeof(Image)); driveSheet.transform.SetParent(canvas.transform, false); Stretch(driveSheet); driveSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(driveSheet.transform, false); Stretch(body); driveBody = body.transform;
            }
            foreach (Transform ch in driveBody) Destroy(ch.gameObject);
            // her portrait across the top, fading into the sheet
            var pg = new GameObject("Pic", typeof(RectTransform), typeof(RawImage)); pg.transform.SetParent(driveBody, false); var pic = pg.GetComponent<RawImage>(); pic.raycastTarget = false;
            if (garage != null) pic.texture = garage.Portrait(spec);
            var prt = pic.rectTransform; prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f); prt.anchoredPosition = new Vector2(0f, -170f); prt.sizeDelta = new Vector2(1080f, 612f);
            var fade = MakeImage(driveBody, "Fade", new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(1080f, 320f), new Color(0.02f, 0.02f, 0.03f, 1f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.1f); fade.rectTransform.pivot = new Vector2(0.5f, 1f); fade.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            MakeGhost(driveBody, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => driveSheet.SetActive(false));
            var ey = MakeText(driveBody, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -820), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced(lent ? "TEST DRIVE · YOURS TONIGHT" : "TEST DRIVE · ONE NIGHT"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1000, 40);
            var ti = MakeText(driveBody, "Title", new Vector2(0.5f, 1f), new Vector2(0, -900), TextAnchor.MiddleCenter, 96, OpInk); ti.text = c.name.ToUpperInvariant(); Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(1040, 120); Fit(ti, 50);
            var ds = MakeText(driveBody, "Desc", new Vector2(0.5f, 1f), new Vector2(0, -1010), TextAnchor.UpperCenter, 32, new Color(0.86f, 0.84f, 0.8f)); ds.text = c.desc; ds.rectTransform.sizeDelta = new Vector2(900, 100); Fit(ds, 22);
            if (lent)
                MakePrimary(driveBody, "To battle", new Vector2(0.5f, 1f), new Vector2(0, -1210), new Vector2(880, 150), 50, () => { driveSheet.SetActive(false); OnStart?.Invoke(); });
            else
                MakePrimary(driveBody, "Drive her tonight · watch an ad", new Vector2(0.5f, 1f), new Vector2(0, -1210), new Vector2(880, 150), 38, () => Ads.Rewarded("testdrive", () =>
                {
                    if (Depot.TestDriveOffer != c) return;
                    Depot.GrantTestDrive(c); Sfx.Pickup(); if (garage != null) garage.Show(spec);
                    RefreshTestDriveButton(); ShowTestDrive(); Ads.Say(c.name + " is yours tonight");
                }));
            var ln = MakeText(driveBody, "Line", new Vector2(0.5f, 1f), new Vector2(0, -1320), TextAnchor.UpperCenter, 26, OpDim); ln.rectTransform.sizeDelta = new Vector2(900, 80); Fit(ln, 18);
            ln.text = lent ? "She leads your next night, then goes back to the depot." : "Once a day · she leads your next night, then goes back to the depot.";
            // her price to keep her, and the way to pay it when the points are there
            var keep = MakeText(driveBody, "Keep", new Vector2(0.5f, 1f), new Vector2(0, -1430), TextAnchor.MiddleCenter, 30, OpAmber); keep.font = BoldFont(); keep.rectTransform.sizeDelta = new Vector2(900, 44);
            keep.text = "Keep her for " + c.cost.ToString("N0", En) + " points";
            if (Depot.Points >= c.cost)
                MakeGhost(driveBody, "Buy her · " + c.cost.ToString("N0", En) + " pts", new Vector2(0.5f, 1f), new Vector2(0, -1520), new Vector2(560, 90), 28, () =>
                {
                    if (!Depot.PickLeader(c)) return;
                    Sfx.Pickup(); if (garage != null) garage.Show(spec); driveSheet.SetActive(false); ShowTitle(titleReserve);
                });
            driveSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
            driveSheet.SetActive(true);
        }
    }
}
