using UnityEngine;
using System.Collections;

public class JoinTrainingGameGUI : BaseJoinGameGUI
{
    private TrainingFpsMode _gameMode;

    public JoinTrainingGameGUI(TrainingFpsMode gameMode)
    {
        _gameMode = gameMode;
    }

    public override void Draw(Rect rect)
    {
        float marginLeft = 0;

        if (GUITools.Button(new Rect(marginLeft, 45, 130, 130), GUIContent.none, StormFront.ButtonJoinGray))
        {
            GamePageManager.Instance.UnloadCurrentPage();
            _gameMode.InitializeMode();
        }
    }
}