using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameForest_Test_Task
{
    struct MoveAnimation
    {
        public float timeElapsed;
        public float duration;
        public GameField.BlockType type;
        public Vector2 start;
        public Vector2 shift;
        public TableCoords destination;

        public MoveAnimation(Vector2 _start, Vector2 _shift, float _duration, GameField.BlockType _type, TableCoords _destination)
        {
            start = _start;
            timeElapsed = 0;
            shift = _shift;
            duration = _duration;
            destination = _destination;
            type = _type;
        }
    }
}
