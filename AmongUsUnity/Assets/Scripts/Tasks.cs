using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AmongUsLan
{
    public struct TaskStep
    {
        public string Console;      // name of the Console GameObject in the Skeld prefab
        public SystemTypes Room;
    }

    /// <summary>A crewmate task made of one or more steps at real Skeld consoles.</summary>
    public class GameTask
    {
        public TaskTypes Type;
        public TaskStep[] Steps;
        public int Done;

        public bool Complete => Done >= Steps.Length;
        public TaskStep Current => Steps[Math.Min(Done, Steps.Length - 1)];

        public string Title
        {
            get
            {
                var sb = new StringBuilder();
                var s = Type.ToString();
                for (int i = 0; i < s.Length; i++)
                {
                    if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1])) sb.Append(' ');
                    sb.Append(s[i]);
                }
                return sb.ToString();
            }
        }

        public string RoomName(SystemTypes r) => r == SystemTypes.LifeSupp ? "O2" : r == SystemTypes.Nav ? "Navigation" : r.ToString();

        public string Encode()
        {
            return (int)Type + ":" + string.Join("+", Steps.Select(s => s.Console + "@" + (int)s.Room)) + ":" + Done;
        }

        public static GameTask Decode(string text)
        {
            var parts = text.Split(':');
            return new GameTask
            {
                Type = (TaskTypes)int.Parse(parts[0]),
                Steps = parts[1].Split('+').Select(s => { var a = s.Split('@'); return new TaskStep { Console = a[0], Room = (SystemTypes)int.Parse(a[1]) }; }).ToArray(),
                Done = int.Parse(parts[2]),
            };
        }
    }

    /// <summary>Task catalogue for The Skeld, built from the consoles present in the original ship prefab.</summary>
    public static class TaskCatalog
    {
        static TaskStep S(string console, SystemTypes room) => new TaskStep { Console = console, Room = room };
        static GameTask T(TaskTypes type, params TaskStep[] steps) => new GameTask { Type = type, Steps = steps };
        static T Pick<T>(Random r, IList<T> list) => list[r.Next(list.Count)];

        static readonly SystemTypes[] WiringRooms =
            { SystemTypes.Electrical, SystemTypes.Storage, SystemTypes.Admin, SystemTypes.Nav, SystemTypes.Cafeteria, SystemTypes.Security };
        static readonly SystemTypes[] DivertTargets =
            { SystemTypes.Shields, SystemTypes.Weapons, SystemTypes.Comms, SystemTypes.Nav, SystemTypes.LifeSupp, SystemTypes.UpperEngine, SystemTypes.LowerEngine, SystemTypes.Security };
        static readonly SystemTypes[] DownloadRooms =
            { SystemTypes.Cafeteria, SystemTypes.Weapons, SystemTypes.Nav, SystemTypes.Electrical, SystemTypes.Comms };

        static readonly Func<Random, GameTask>[] Common =
        {
            r => T(TaskTypes.FixWiring, WiringRooms.OrderBy(_ => r.Next()).Take(3).Select(x => S("FixWiringConsole", x)).ToArray()),
            r => T(TaskTypes.SwipeCard, S("SwipeCardConsole", SystemTypes.Admin)),
        };

        static readonly Func<Random, GameTask>[] Short =
        {
            r => T(TaskTypes.CalibrateDistributor, S("CalibrateConsole", SystemTypes.Electrical)),
            r => T(TaskTypes.ChartCourse, S("ChartCourseConsole", SystemTypes.Nav)),
            r => T(TaskTypes.CleanO2Filter, S("CleanFilterConsole", SystemTypes.LifeSupp)),
            r => T(TaskTypes.DivertPower, S("DivertPowerConsole", SystemTypes.Electrical), S("DivertPowerConsole", Pick(r, DivertTargets))),
            r => T(TaskTypes.EmptyGarbage, S("GarbageConsole", r.Next(2) == 0 ? SystemTypes.Cafeteria : SystemTypes.LifeSupp)),
            r => T(TaskTypes.PrimeShields, S("ShieldConsole", SystemTypes.Shields)),
            r => T(TaskTypes.StabilizeSteering, S("StabilizeSteeringConsole", SystemTypes.Nav)),
            r => T(TaskTypes.UnlockManifolds, S("UnlockManifoldsConsole", SystemTypes.Reactor)),
            r => T(TaskTypes.SubmitScan, S("MedScanner", SystemTypes.MedBay)),
            r =>
            {
                var from = Pick(r, DownloadRooms);
                return T(TaskTypes.UploadData, S(from == SystemTypes.Cafeteria ? "DataConsole" : "UploadDataConsole", from), S("UploadDataConsole", SystemTypes.Admin));
            },
        };

        static readonly Func<Random, GameTask>[] Long =
        {
            r => T(TaskTypes.AlignEngineOutput, S("AlignEngineConsole", SystemTypes.UpperEngine), S("AlignEngineConsole", SystemTypes.LowerEngine)),
            r => T(TaskTypes.FuelEngines, S("gasCanConsole", SystemTypes.Storage), S("FuelEngineConsole", SystemTypes.UpperEngine),
                                           S("gasCanConsole", SystemTypes.Storage), S("FuelEngineConsole", SystemTypes.LowerEngine)),
            r => T(TaskTypes.ClearAsteroids, S("WeaponConsole", SystemTypes.Weapons)),
            r => T(TaskTypes.StartReactor, S("StartReactorConsole", SystemTypes.Reactor)),
            r => T(TaskTypes.InspectSample, S("MedBayConsole", SystemTypes.MedBay), S("MedBayConsole", SystemTypes.MedBay)),
        };

        public static List<GameTask> Generate(Random r, int common, int shortCount, int longCount)
        {
            var list = new List<GameTask>();
            void Add(Func<Random, GameTask>[] pool, int n)
            {
                foreach (var f in pool.OrderBy(_ => r.Next()).Take(n)) list.Add(f(r));
            }
            Add(Common, common); Add(Short, shortCount); Add(Long, longCount);
            return list;
        }
    }
}
