using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Automated plumbing test for the original minigames (-uitest): every collider must be reachable and the close button must work.</summary>
    public partial class Game
    {
        bool _uiTestStarted;

        static readonly string[] UiTestPrefabs =
        {
            "CardMinigame", "WireMinigame", "EmptyGarbageMinigame", "SweepMinigame", "CourseMinigame", "LeafMinigame", "ShieldsMinigame", "NavMinigame",
            "UnlockManifoldsMinigame", "MedScanMinigame", "WeaponsMinigame", "AlignMinigame", "RefuelMinigame", "SimonSaysGame", "SampleMinigame", "UploadMinigame",
            "DivertMinigame", "ReceivePowerMinigame", "SwitchMinigame", "RadioMinigame", "KeyPadDisarmMinigame", "ResetReactorMinigame", "SurvMinigame",
        };

        void RunUiTest()
        {
            if (_uiTestStarted || _phase != Phase.Playing || _autoTimer < 4f) return;
            _uiTestStarted = true;
            StartCoroutine(UiTestRoutine());
        }

        IEnumerator UiTestRoutine()
        {
            var cases = new List<KeyValuePair<string, int>>();
            foreach (var n in UiTestPrefabs) cases.Add(new KeyValuePair<string, int>(n, 0));
            cases.Add(new KeyValuePair<string, int>("SampleMinigame", 1));
            cases.Add(new KeyValuePair<string, int>("RefuelMinigame", 1));
            cases.Add(new KeyValuePair<string, int>("RefuelMinigame", 2));
            cases.Add(new KeyValuePair<string, int>("RefuelMinigame", 3));
            cases.Add(new KeyValuePair<string, int>("UploadMinigame", 1));
            string only = null;
            { var args = Environment.GetCommandLineArgs(); int oi = Array.IndexOf(args, "-uionly"); if (oi >= 0) only = args[oi + 1]; }
            foreach (var kv in cases)
            {
                string name = kv.Key; int step = kv.Value;
                if (only != null && !only.Split(',').Contains(name)) continue;
                Minigame.FakePos = null; Minigame.FakeDown = Minigame.FakeHeld = Minigame.FakeUp = false;
                bool won = false, held = false;
                var mg = Minigame.Open(name, ok => { if (ok) won = true; }, step, SystemTypes.Admin, "", false, SystemTypes.Shields, "12345", h => { if (h) held = true; });
                if (mg == null) { Debug.Log("[UITEST] " + name + "#" + step + " FAILED-TO-OPEN"); continue; }
                yield return new WaitForSeconds(0.4f);
                int total = 0, bad = 0; var sb = new StringBuilder();
                foreach (var c in mg.GetComponentsInChildren<Collider2D>(false))
                {
                    if (c.name.Contains("CloseButton") || c.name == "Boundary" || c.name.StartsWith("o2_leaf")) continue;
                    total++;
                    var p = (Vector2)c.bounds.center;
                    if (!UiKit.PointAll(p).Contains(c)) { bad++; sb.Append(" unreachable:" + c.name); }
                }
                mg.StartCoroutine(mg.TestPlay());
                float until = Time.time + 75f;
                while (mg != null && !won && Time.time < until) yield return null;
                bool alive = mg != null;
                bool pass = won || (name == "ResetReactorMinigame" && held) || (name == "SurvMinigame" && alive);
                Minigame.FakePos = null; Minigame.FakeDown = Minigame.FakeHeld = Minigame.FakeUp = false;
                Debug.Log("[UITEST] " + name + "#" + step + " colliders=" + total + " unreachable=" + bad + sb + " RESULT=" + (pass ? "PASS" : "FAIL") + " won=" + won);
                if (alive && mg != null)
                {
                    var close = mg.GetComponentsInChildren<Collider2D>(false).FirstOrDefault(c => c.name == "CloseButton");
                    bool closed = false;
                    if (close != null)
                    {
                        var cp = (Vector2)close.bounds.center;
                        Minigame.FakePos = cp; Minigame.FakeDown = true; Minigame.FakeHeld = true;
                        yield return null; Minigame.FakeDown = false; yield return null;
                        Minigame.FakeHeld = false; Minigame.FakeUp = true; yield return null; Minigame.FakeUp = false;
                        closed = mg == null;
                    }
                    Minigame.FakePos = null;
                    Debug.Log("[UITEST] " + name + "#" + step + " closeByX=" + closed);
                    if (mg != null) mg.Close(false);
                }
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log("[UITEST] DONE");
            Application.Quit();
        }
    }
}
