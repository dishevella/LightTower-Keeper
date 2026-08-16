using System.Collections.Generic;

public static class LegacyStoryPhaseMap
{
    private readonly struct Mapping
    {
        public Mapping(StoryPhase phase, StoryChapter chapter, string beatId)
        {
            Phase = phase;
            Chapter = chapter;
            BeatId = beatId;
        }

        public StoryPhase Phase { get; }
        public StoryChapter Chapter { get; }
        public string BeatId { get; }
    }

    private static readonly Mapping[] Mappings =
    {
        new(StoryPhase.Day0_BoatIntro, StoryChapter.Day0, StoryBeatIds.Day0.ArriveIsland),
        new(StoryPhase.Day0_Forest, StoryChapter.Day0, StoryBeatIds.Day0.ReachLighthouse),
        new(StoryPhase.Day0_ApproachLighthouse, StoryChapter.Day0, StoryBeatIds.Day0.RestoreLighthouse),
        new(StoryPhase.Day0_Rest, StoryChapter.Day0, StoryBeatIds.Day0.FirstNight),
        new(StoryPhase.Day1_Start, StoryChapter.Day1, StoryBeatIds.Day1.MorningRoutine),
        new(StoryPhase.Day1_InspectInside, StoryChapter.Day1, StoryBeatIds.Day1.GoToSupplyPoint),
        new(StoryPhase.Day1_GoDock, StoryChapter.Day1, StoryBeatIds.Day1.FindFootprints),
        new(StoryPhase.Day1_ReturnRoute, StoryChapter.Day1, StoryBeatIds.Day1.ReturnToLighthouse),
        new(StoryPhase.Day1_NightDuty, StoryChapter.Day1, StoryBeatIds.Day1.NightDuty),
        new(StoryPhase.Day1_Complete, StoryChapter.Day1, StoryBeatIds.Day1.SeeDistantFigure)
    };

    private static readonly Dictionary<StoryPhase, Mapping> ByPhase = BuildByPhase();
    private static readonly Dictionary<string, Mapping> ByBeat = BuildByBeat();

    public static bool TryGetBeat(StoryPhase phase, out StoryChapter chapter, out string beatId)
    {
        if (ByPhase.TryGetValue(phase, out Mapping mapping))
        {
            chapter = mapping.Chapter;
            beatId = mapping.BeatId;
            return true;
        }

        chapter = StoryChapter.None;
        beatId = string.Empty;
        return false;
    }

    public static bool TryGetPhase(string beatId, out StoryPhase phase)
    {
        if (!string.IsNullOrWhiteSpace(beatId) &&
            ByBeat.TryGetValue(beatId.Trim(), out Mapping mapping))
        {
            phase = mapping.Phase;
            return true;
        }

        phase = StoryPhase.None;
        return false;
    }

    private static Dictionary<StoryPhase, Mapping> BuildByPhase()
    {
        Dictionary<StoryPhase, Mapping> result = new();
        foreach (Mapping mapping in Mappings)
        {
            result[mapping.Phase] = mapping;
        }

        return result;
    }

    private static Dictionary<string, Mapping> BuildByBeat()
    {
        Dictionary<string, Mapping> result = new(System.StringComparer.Ordinal);
        foreach (Mapping mapping in Mappings)
        {
            result[mapping.BeatId] = mapping;
        }

        return result;
    }
}
