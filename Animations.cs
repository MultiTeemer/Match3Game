using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameForest_Test_Task
{
    struct SwapAnimation
    {
        public const float DURATION = 200; // in milliseconds

        public int block1Idx;
        public int block2Idx;
        public double elapsed;
        public Vector2 shift;

        public SwapAnimation(int id1, int id2, Vector2 sh)
        {
            block1Idx = id1;
            block2Idx = id2;
            shift = sh;
            elapsed = 0;
        }
    }
}
