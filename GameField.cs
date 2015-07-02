using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameForest_Test_Task
{
    struct GameField
    {
        public enum BlockTypeE
        { 
            Cirle,
            Diamond,
            Hex,
            Square,
            Star,
            Triangle,

            BlocksCount,
            Empty,
        };

        private static Random gen = new Random();

        public static BlockTypeE RandomType()
        {
            return (BlockTypeE)gen.Next(0, (int)BlockTypeE.BlocksCount - 1);
        }

        public BlockTypeE[] field;

        public GameField(int size)
        {
            field = new BlockTypeE[size * size];
        }

        public void Init()
        {
            for (int i = 0; i < field.Length; ++i)
            {
                field[i] = RandomType();
            }
        }

        public void Swap(int idx1, int idx2)
        {
            BlockTypeE tmp = field[idx1];
            field[idx1] = field[idx2];
            field[idx2] = tmp;
        }

        public BlockTypeE Get(int idx)
        {
            return OutOfBounds(idx) ? BlockTypeE.Empty : field[idx];
        }

        public bool Set(int idx, BlockTypeE type)
        {
            if (OutOfBounds(idx)) return false;

            field[idx] = type;

            return true;
        }

        public void SetEmpty(int idx)
        {
            if (!OutOfBounds(idx))
            {
                field[idx] = BlockTypeE.Empty;
            }
        }

        public bool OutOfBounds(int idx)
        {
            return idx < 0 || idx >= field.Length;
        }

        public bool IsEmpty(int idx)
        {
            return Get(idx) == BlockTypeE.Empty;
        }
    }
}
