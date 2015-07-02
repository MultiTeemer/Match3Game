using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace GameForest_Test_Task
{
    struct GameField
    {
        public enum BlockType
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

        public static BlockType RandomType()
        {
            return (BlockType)gen.Next(0, (int)BlockType.BlocksCount - 1);
        }

        private BlockType[] field;
        private int size;

        public GameField(int _size)
        {
            field = new BlockType[_size * _size];
            size = _size;
        }

        public void Init()
        {
            for (int i = 0; i < field.Length; ++i)
            {
                field[i] = RandomType();
            }
        }

        public void Swap(TableCoords pos1, TableCoords pos2)
        {
            int idx1 = posToIdx(pos1);
            int idx2 = posToIdx(pos2);

            BlockType tmp = field[idx1];
            field[idx1] = field[idx2];
            field[idx2] = tmp;
        }

        public BlockType Get(TableCoords pos)
        {
            return OutOfBounds(pos) ? BlockType.Empty : field[posToIdx(pos)];
        }

        public BlockType Get(int col, int row)
        {
            return Get(new TableCoords(col, row));
        }

        public bool Set(TableCoords pos, BlockType type)
        {
            if (OutOfBounds(pos)) return false;

            field[posToIdx(pos)] = type;

            return true;
        }

        public void SetEmpty(TableCoords pos)
        {
            if (!OutOfBounds(pos))
            {
                field[posToIdx(pos)] = BlockType.Empty;
            }
        }

        public bool OutOfBounds(TableCoords pos)
        {
            return outOfSize(pos.col) || outOfSize(pos.row);
        }

        public bool IsEmpty(TableCoords pos)
        {
            return Get(pos) == BlockType.Empty;
        }

        private int posToIdx(TableCoords pos)
        {
            return pos.row * size + pos.col;
        }

        private bool outOfSize(int a)
        {
            return a < 0 || a >= size;
        }
    }
}
