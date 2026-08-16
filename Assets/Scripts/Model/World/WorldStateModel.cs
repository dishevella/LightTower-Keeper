using System;
using System.Collections.Generic;

[Serializable]
public sealed class WorldStateModel : ModelAbstract
{
    private readonly HashSet<string> facts = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Facts => facts;

    protected override void OnInit()
    {
        facts.Clear();
    }

    public bool HasFact(string factId)
    {
        return facts.Contains(NormalizeId(factId));
    }

    public bool SetFact(string factId, bool enabled = true)
    {
        string stableId = NormalizeId(factId);
        if (stableId.Length == 0)
        {
            return false;
        }

        return enabled ? facts.Add(stableId) : facts.Remove(stableId);
    }

    public void Restore(IEnumerable<string> restoredFacts)
    {
        facts.Clear();
        if (restoredFacts == null)
        {
            return;
        }

        foreach (string fact in restoredFacts)
        {
            string stableId = NormalizeId(fact);
            if (stableId.Length > 0)
            {
                facts.Add(stableId);
            }
        }
    }

    public void Clear()
    {
        facts.Clear();
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
