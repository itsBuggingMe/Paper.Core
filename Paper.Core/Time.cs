using Microsoft.Xna.Framework;

namespace Paper.Core;

public class Time
{
    public float FrameDeltaTime { get; private set; }

    public GameTime GameTime { get; private set; }


    public void SetValues(GameTime gameTime)
    {
        FrameDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds / (1 / 60f);
        GameTime = gameTime;
    }
}