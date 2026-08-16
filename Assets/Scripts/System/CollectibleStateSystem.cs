using System.Collections.Generic;

public sealed class CollectibleStateSystem : SystemAbstract
{
    private CollectibleStateModel model;

    protected override void OnInit()
    {
        model = this.GetModel<CollectibleStateModel>();
    }

    public bool IsCollected(string collectibleId)
    {
        return model != null && model.IsCollected(collectibleId);
    }

    public bool Collect(string collectibleId)
    {
        if (model == null || !model.Collect(collectibleId))
        {
            return false;
        }

        this.GetEvent().Send(new CollectibleStateChangedEvent(collectibleId.Trim(), true));
        return true;
    }

    public void Restore(IEnumerable<string> collectibleIds)
    {
        model?.Restore(collectibleIds);
    }
}
