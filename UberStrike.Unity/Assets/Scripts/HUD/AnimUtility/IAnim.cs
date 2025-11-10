public interface IUpdatable
{
    void Update();
}

public interface IDrawable
{
    bool Visible { get; set; }
    void Draw();
}

public interface IAnim : IUpdatable
{
    bool IsAnimating { get; set; }
    float Duration { get; set; }
    float StartTime { get; set; }
    void Start();
    void Stop();
}
