using Microsoft.Xna.Framework;
using System;

namespace Paper.Core;

public interface IScreen
{
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
    void OnEnter(IScreen previous, object args);
    object OnExit(IScreen next);
}