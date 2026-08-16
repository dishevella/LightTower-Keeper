using System;
using System.Collections.Generic;

[Serializable]
public sealed class CollectibleStateModel : ModelAbstract
{
    private readonly HashSet<string> collectedIds = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CollectedIds => collectedIds;

    protected override void OnInit()
    {
        collectedIds.Clear();
    }

    public bool Collect(string collectibleId)
    {
        return !string.IsNullOrWhiteSpace(collectibleId) && collectedIds.Add(collectibleId.Trim());
    }

    public bool IsCollected(string collectibleId)
    {
        return !string.IsNullOrWhiteSpace(collectibleId) && collectedIds.Contains(collectibleId.Trim());
    }

    public void Restore(IEnumerable<string> ids)
    {
        collectedIds.Clear();
        if (ids == null)
        {
            return;
        }

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                collectedIds.Add(id.Trim());
            }
        }
    }
}
