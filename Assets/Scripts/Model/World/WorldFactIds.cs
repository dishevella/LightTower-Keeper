using System.Collections.Generic;

public static class WorldFactIds
{
    public const string BridgeCollapsed = "world.bridge_collapsed";
    public const string FirstDrawingFound = "world.first_drawing_found";
    public const string GirlSeen = "world.girl_seen";
    public const string FoodLeftForGirl = "world.food_left_for_girl";
    public const string GirlReplyReceived = "world.girl_reply_received";
    public const string CaveOpened = "world.cave_opened";
    public const string BriggsFound = "world.briggs_found";
    public const string GirlRescued = "world.girl_rescued";
    public const string EmergencyBeaconActivated = "world.emergency_beacon_activated";

    public static readonly IReadOnlyList<string> All = new[]
    {
        BridgeCollapsed,
        FirstDrawingFound,
        GirlSeen,
        FoodLeftForGirl,
        GirlReplyReceived,
        CaveOpened,
        BriggsFound,
        GirlRescued,
        EmergencyBeaconActivated
    };
}
