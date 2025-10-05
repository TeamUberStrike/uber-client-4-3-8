
using Cmune.DataCenter.Common.Entities;

public class InboxMessage
{

    public InboxMessage(PrivateMessageView view, string senderName)
    {
        MessageView = view;
        SenderName = senderName;
    }

    #region Properties

    public bool IsMine
    {
        get { return MessageView.FromCmid == PlayerDataManager.Cmid; }
    }

    public bool IsAdmin
    {
        get { return MessageView.FromCmid == InboxThread.AdminCmid; }
    }

    public string Content
    {
        get { return MessageView.ContentText; }
    }

    public string SentDateString
    {
        get { return MessageView.DateSent.ToString("MMM") + " " + MessageView.DateSent.Day.ToString() + " at " + MessageView.DateSent.ToShortTimeString(); }
    }

    public string SenderName { get; private set; }

    public PrivateMessageView MessageView { get; private set; }

    #endregion
}