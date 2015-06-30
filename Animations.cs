using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameForest_Test_Task
{
    struct MoveAnimation
    {
        public int blockId;
        public float timeElapsed;
        public float duration;
        public Vector2 shift;
        public GameField.BlockTypeE type;
        public Vector2 destination;

        public MoveAnimation(int id, Vector2 sh, float d, GameField.BlockTypeE t, Vector2 dest)
        {
            blockId = id;
            timeElapsed = 0;
            shift = sh;
            duration = d;
            destination = dest;
            type = t;
        }
    }
}
