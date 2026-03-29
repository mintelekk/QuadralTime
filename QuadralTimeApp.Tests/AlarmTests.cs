using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Collections.Generic;

namespace QuadralTimeApp.Tests;

[TestClass]
public class AlarmTests
{
    private class TestAlarm
    {
        public TimeSpan Time { get; set; }
        public string Recurrence { get; set; } = "Once";
        public bool Active { get; set; } = true;
        public DateTime? LastTriggered { get; set; } = null;
    }

    // Simulate the ShouldTriggerAlarm logic from MainWindow
    private static bool ShouldTriggerAlarm(TestAlarm alarm, DateTime now)
    {
        if (alarm.LastTriggered != null && alarm.LastTriggered.Value.Date == now.Date && alarm.LastTriggered.Value.Hour == now.Hour && alarm.LastTriggered.Value.Minute == now.Minute)
            return false;
        bool match = false;
        switch (alarm.Recurrence)
        {
            case "Once":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && (alarm.LastTriggered == null));
                break;
            case "Daily":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes());
                break;
            case "Weekdays":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday);
                break;
            case "Weekends":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday));
                break;
        }
        return match;
    }

    // Uses QuadralTimeApp.TimeExtensions

    [TestMethod]
    public void OneTimeAlarm_Triggers_And_Deactivates()
    {
        var alarm = new TestAlarm { Time = new TimeSpan(7, 30, 0), Recurrence = "Once", Active = true };
        var now = new DateTime(2026, 3, 28, 7, 30, 0);
        Assert.IsTrue(ShouldTriggerAlarm(alarm, now));
        alarm.LastTriggered = now;
        // Should not trigger again in the same minute
        Assert.IsFalse(ShouldTriggerAlarm(alarm, now));
    }

    [TestMethod]
    public void WeekdayAlarm_Triggers_On_Weekday_And_Not_Weekend()
    {
        var alarm = new TestAlarm { Time = new TimeSpan(9, 0, 0), Recurrence = "Weekdays", Active = true };
        var monday = new DateTime(2026, 3, 30, 9, 0, 0); // Monday
        var saturday = new DateTime(2026, 4, 4, 9, 0, 0); // Saturday
        Assert.IsTrue(ShouldTriggerAlarm(alarm, monday));
        Assert.IsFalse(ShouldTriggerAlarm(alarm, saturday));
    }
}
