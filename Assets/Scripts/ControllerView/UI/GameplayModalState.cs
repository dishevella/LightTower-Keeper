public static class GameplayModalState
{
    private static int openModalCount;

    public static bool HasOpenModal => openModalCount > 0;

    public static bool TryOpen()
    {
        if (openModalCount > 0)
        {
            return false;
        }

        openModalCount++;
        return true;
    }

    public static void CloseOne()
    {
        if (openModalCount <= 0)
        {
            openModalCount = 0;
            return;
        }

        openModalCount--;
    }
}
