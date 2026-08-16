public sealed class SaveCheckpointCommand : CommandAbstract
{
    public SaveCheckpointCommand(string checkpointId)
    {
        CheckpointId = checkpointId;
    }

    public string CheckpointId { get; }

    protected override void OnExecute()
    {
        this.GetSystem<SaveSystem>()?.SaveCheckpoint(CheckpointId);
    }
}

public sealed class ContinueGameCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<SaveSystem>()?.TryLoadDefaultSlot();
    }
}

public sealed class DeleteDefaultSaveCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<SaveSystem>()?.DeleteDefaultSave();
    }
}
