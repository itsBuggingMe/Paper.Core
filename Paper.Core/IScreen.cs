namespace Paper.Core;

public interface IScreen
{
    void Update(Time gameTime);
    void Draw(Time gameTime);
    void OnEnter(IScreen previous, object? args);
    object? OnExit();
}