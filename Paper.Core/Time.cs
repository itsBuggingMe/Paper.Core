using Microsoft.Xna.Framework;

namespace Paper.Core;

public class Time
{
    public float FrameDeltaTime { get; private set; }
    public float DeltaTime { get; private set; }

    public GameTime GameTime { get; private set; } = null!;


    public void SetValues(GameTime gameTime)
    {
        FrameDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds / (1 / 60f);
        DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        GameTime = gameTime;
    }
}