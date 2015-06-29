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
        };

        public BlockTypeE[] field;

        public GameField(int size)
        {
            field = new BlockTypeE[size * size];
        }

        public void Init()
        {
            Random gen = new Random();

            for (int i = 0; i < field.Length; ++i)
            {
                field[i] = (BlockTypeE)gen.Next(0, (int)BlockTypeE.BlocksCount - 1);
            }
        }

        public void Swap(int idx1, int idx2)
        {
            BlockTypeE tmp = field[idx1];
            field[idx1] = field[idx2];
            field[idx2] = tmp;
        }
    }
}
