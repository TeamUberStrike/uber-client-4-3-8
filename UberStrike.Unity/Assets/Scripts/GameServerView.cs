using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using UnityEngine;

public class GameServerView
{
    public DynamicTexture Flag { get; set; }
    public ServerLoadData Data = ServerLoadData.Empty;
    private ConnectionAddress _address = ConnectionAddress.Empty;
    private PhotonView _view;

    private GameServerView()
    {
        _view = new PhotonView();
        _address.ConnectionString = "0.0.0.0:0";
        Region = "Default";
        Flag = new DynamicTexture(ApplicationDataManager.BaseImageURL + "Flags/" + Region + ".png");
    }

    public GameServerView(string address, PhotonUsageType type)
    {
        _address.ConnectionString = address;

        if (_address.IsValid)
        {
            _view = new PhotonView()
            {
                IP = _address.ServerIP,
                Port = int.Parse(_address.ServerPort),
                UsageType = type,
            };
        }
        else
        {
            _view = new PhotonView();
        }

        Region = "Default";
        Flag = new DynamicTexture(ApplicationDataManager.BaseImageURL + "Flags/" + Region + ".png");
    }

    public GameServerView(PhotonView view)
    {
        _address.ConnectionString = string.Format("{0}:{1}", view.IP, view.Port);

        _view = view;

        int start = _view.Name.IndexOf('[');
        int end = _view.Name.IndexOf(']');
        if (start >= 0 && end > 1 && end > start)
        {
            Region = _view.Name.Substring(start + 1, end - start - 1);
        }
        else
        {
            Region = "Default";
        }

        Flag = new DynamicTexture(ApplicationDataManager.BaseImageURL + "Flags/" + Region + ".png");
        Name = _view.Name;
    }

    public static GameServerView Empty
    {
        get { return new GameServerView(); }
    }

    public int Id { get { return _view.PhotonId; } }

    /// <summary>
    /// The Connection string is following the format x.x.x.x:y, or ipv4:port
    /// </summary>
    public string ConnectionString
    {
        get { return _address.ConnectionString; }
    }
    /// <summary>
    /// Value in percent - indicating how busy the server is.
    /// (Data.PeersConnected + Data.RoomsCreated) / 100
    /// </summary>
    public float ServerLoad
    {
        get { return Mathf.Min(Data.PlayersConnected + Data.RoomsCreated, 100) / 100f; }
    }
    /// <summary>
    /// Latency to the Server in milli seconds
    /// </summary>
    public int Latency
    {
        get { return Data.Latency; }
    }
    /// <summary>
    /// Latency to the Server in milli seconds
    /// </summary>
    public int MinLatency
    {
        get { return _view.MinLatency; }
    }
    /// <summary>
    /// Checking the USageType for unequals PhotonUsageType.None
    /// </summary>
    public bool IsValid
    {
        get { return UsageType != PhotonUsageType.None; }
    }
    /// <summary>
    /// Indicator of the usage type of the server e.g. Lobby Server
    /// </summary>
    public PhotonUsageType UsageType
    {
        get { return _view.UsageType; }
    }
    /// <summary>
    /// Name of the Server, used as decriptor
    /// </summary>
    public string Name
    {
        get;
        private set;
    }
    /// <summary>
    /// Indicator if the Server is in US, Asia or Europe
    /// </summary>
    public string Region { get; private set; }

    public override string ToString()
    {
        return string.Format("Address: {0}\nLatency: {1}\nType: {2}\n{3}", _address.ConnectionString, Latency, UsageType, Data.ToString());
    }

    internal bool LatencyCheck()
    {
        return MinLatency <= 0 || MinLatency > Latency;
    }
}