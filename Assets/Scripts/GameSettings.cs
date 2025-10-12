using UnityEngine;

public static class GameSettings
{
    public static int SelectedStage { get; private set; } = 1;

    public enum DialogueType { Intro, Outro }

    public static DialogueType CurrentDialogueType = DialogueType.Intro;

    public static void SetSelectedStage(int stage)
    {
        SelectedStage = stage;
    }

    public static void SetDialogueType(DialogueType type)
    {
        CurrentDialogueType = type;
    }
}