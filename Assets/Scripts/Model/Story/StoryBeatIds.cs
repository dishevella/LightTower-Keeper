using System.Collections.Generic;

public static class StoryBeatIds
{
    public static class Day0
    {
        public const string ArriveIsland = "day0.arrive_island";
        public const string ReachLighthouse = "day0.reach_lighthouse";
        public const string RestoreLighthouse = "day0.restore_lighthouse";
        public const string FirstNight = "day0.first_night";
    }

    public static class Day1
    {
        public const string MorningRoutine = "day1.morning_routine";
        public const string GoToSupplyPoint = "day1.go_to_supply_point";
        public const string FindFootprints = "day1.find_footprints";
        public const string FindBottleDrawing = "day1.find_bottle_drawing";
        public const string ReturnToLighthouse = "day1.return_to_lighthouse";
        public const string FoodMissing = "day1.food_missing";
        public const string NightDuty = "day1.night_duty";
        public const string SeeDistantFigure = "day1.see_distant_figure";
    }

    public static class Day2
    {
        public const string FoodMissingAgain = "day2.food_missing_again";
        public const string FirstClearSightOfGirl = "day2.first_clear_sight_of_girl";
        public const string LeaveFood = "day2.leave_food";
        public const string ReceiveDrawingAndStone = "day2.receive_drawing_and_stone";
        public const string SilentExchangeMontage = "day2.silent_exchange_montage";
    }

    public static class Day3
    {
        public const string StormWarning = "day3.storm_warning";
        public const string PrepareForStorm = "day3.prepare_for_storm";
        public const string SeeGirlInStorm = "day3.see_girl_in_storm";
        public const string FollowGirl = "day3.follow_girl";
        public const string EnterCave = "day3.enter_cave";
        public const string FindBriggs = "day3.find_briggs";
        public const string ReachSafeRoom = "day3.reach_safe_room";
        public const string RescueGirl = "day3.rescue_girl";
    }

    public static class Day4
    {
        public const string SecondCupMorning = "day4.second_cup_morning";
        public const string WatchBriggsRecord = "day4.watch_briggs_record";
        public const string TellGirlTruth = "day4.tell_girl_truth";
        public const string ActivateEmergencyBeacon = "day4.activate_emergency_beacon";
        public const string Ending = "day4.ending";
    }

    public static readonly IReadOnlyList<string> All = new[]
    {
        Day0.ArriveIsland,
        Day0.ReachLighthouse,
        Day0.RestoreLighthouse,
        Day0.FirstNight,
        Day1.MorningRoutine,
        Day1.GoToSupplyPoint,
        Day1.FindFootprints,
        Day1.FindBottleDrawing,
        Day1.ReturnToLighthouse,
        Day1.FoodMissing,
        Day1.NightDuty,
        Day1.SeeDistantFigure,
        Day2.FoodMissingAgain,
        Day2.FirstClearSightOfGirl,
        Day2.LeaveFood,
        Day2.ReceiveDrawingAndStone,
        Day2.SilentExchangeMontage,
        Day3.StormWarning,
        Day3.PrepareForStorm,
        Day3.SeeGirlInStorm,
        Day3.FollowGirl,
        Day3.EnterCave,
        Day3.FindBriggs,
        Day3.ReachSafeRoom,
        Day3.RescueGirl,
        Day4.SecondCupMorning,
        Day4.WatchBriggsRecord,
        Day4.TellGirlTruth,
        Day4.ActivateEmergencyBeacon,
        Day4.Ending
    };
}
