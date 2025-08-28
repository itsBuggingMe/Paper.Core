using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Paper.Core;

public class Time
{
    public float FrameDeltaTime { get; set; }

    public void SetValues(GameTime gameTime)
    {
        FrameDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds / (1 / 60f);
    }
}