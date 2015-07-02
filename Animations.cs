using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameForest_Test_Task
{
    class Animation
    {
        public float duration;
        public float timeElapsed;
        public GameField.BlockType type;

        protected Animation(float _duration, GameField.BlockType _type)
        {
            duration = _duration;
            type = _type;
            timeElapsed = 0;
        }

        public bool Ended()
        {
            return timeElapsed >= duration;
        }
    };

    class MoveAnimation : Animation
    {
        public float timeElapsed;
        public float duration;
        public Vector2 start;
        public Vector2 shift;
        public TableCoords destination;

        public MoveAnimation(Vector2 _start, Vector2 _shift, float _duration, GameField.BlockType _type, TableCoords _destination) : base(_duration, _type)
        {
            start = _start;
            timeElapsed = 0;
            shift = _shift;
            duration = _duration;
            destination = _destination;
            type = _type;
        }
    };

    class DestroyAnimation : Animation
    {
        public TableCoords block;

        public DestroyAnimation(float _duration, TableCoords _block, GameField.BlockType _type) : base(_duration, _type)
        {
            block = _block;
        }
    };
}
