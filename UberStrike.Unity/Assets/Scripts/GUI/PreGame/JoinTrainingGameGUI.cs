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
        float marginLeft = (rect.width - 130) / 2;
        float marginTop = rect.height * 0.15f;

        if (GUITools.Button(new Rect(marginLeft, marginTop, 130, 130), GUIContent.none, StormFront.ButtonJoinGray))
        {
            GamePageManager.Instance.UnloadCurrentPage();
            _gameMode.InitializeMode();
        }
    }
}