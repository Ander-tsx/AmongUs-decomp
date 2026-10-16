using System.Collections.Generic;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Maps task types to the original minigame prefabs and runs them when the player uses a console.</summary>
    public partial class Game
    {
        static readonly Dictionary<TaskTypes, string> MinigameFor = new Dictionary<TaskTypes, string>
        {
            { TaskTypes.SwipeCard, "CardMinigame" },
            { TaskTypes.FixWiring, "WireMinigame" },
            { TaskTypes.EmptyGarbage, "EmptyGarbageMinigame" },
            { TaskTypes.CalibrateDistributor, "SweepMinigame" },
            { TaskTypes.ChartCourse, "CourseMinigame" },
            { TaskTypes.CleanO2Filter, "LeafMinigame" },
            { TaskTypes.PrimeShields, "ShieldsMinigame" },
            { TaskTypes.StabilizeSteering, "NavMinigame" },
            { TaskTypes.UnlockManifolds, "UnlockManifoldsMinigame" },
            { TaskTypes.SubmitScan, "MedScanMinigame" },
            { TaskTypes.ClearAsteroids, "WeaponsMinigame" },
            { TaskTypes.AlignEngineOutput, "AlignMinigame" },
            { TaskTypes.FuelEngines, "RefuelMinigame" },
            { TaskTypes.StartReactor, "SimonSaysGame" },
            { TaskTypes.InspectSample, "SampleMinigame" },
            { TaskTypes.UploadData, "UploadMinigame" },
        };

        /// <summary>Returns true if an original minigame took over the task step.</summary>
        bool OpenTaskMinigame(int idx)
        {
            var t = _myTasks[idx];
            string prefab;
            if (t.Type == TaskTypes.DivertPower) prefab = t.Done == 0 ? "DivertPowerGame" : "DivertMinigame";
            else if (!MinigameFor.TryGetValue(t.Type, out prefab)) return false;
            var step = t.Current;
            var mg = Minigame.Open(prefab, ok => { if (ok) Cmd("TASK|" + idx); }, t.Done, step.Room, step.Console, _bot);
            if (mg == null) return false;
            if (_me != null && _me.Body != null) _me.Body.linearVelocity = Vector2.zero;
            return true;
        }
    }
}
