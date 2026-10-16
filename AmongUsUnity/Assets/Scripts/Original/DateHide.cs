using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Seasonal decorations: active only inside their yearly (or one-off) date window.</summary>
public class DateHide : MonoBehaviour
{
    public static bool SeasonalEnabled = true;

    public int MonthStart;
    public int DayStart;
    public int HourStart;
    public int MonthEnd;
    public int DayEnd;
    public int HourEnd;
    [SerializeField] bool IsYearly;
    [SerializeField] int YearStart;
    [SerializeField] int YearEnd;
    [SerializeField] bool ControlledByParent;
    [SerializeField] List<int> DisabledYears;

    void Awake() { Apply(); }

    public void Apply()
    {
        if (ControlledByParent || true) gameObject.SetActive(!ShouldHide(DateTime.Now));
    }

    public bool ShouldHide(DateTime now)
    {
        if (!SeasonalEnabled) return true;
        if (DisabledYears != null && DisabledYears.Contains(now.Year)) return true;
        int start = MonthStart * 1000000 + DayStart * 10000 + HourStart * 100;
        int end = MonthEnd * 1000000 + DayEnd * 10000 + HourEnd * 100;
        int cur = now.Month * 1000000 + now.Day * 10000 + now.Hour * 100;
        if (IsYearly)
            return start <= end ? !(cur >= start && cur < end) : !(cur >= start || cur < end);
        var from = new DateTime(YearStart, MonthStart, DayStart, HourStart, 0, 0);
        var to = new DateTime(YearEnd, MonthEnd, DayEnd, HourEnd, 0, 0);
        return !(now >= from && now < to);
    }
}
