using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;


namespace GameForest_Test_Task
{
    struct TableCoords
    {
        public int col;
        public int row;

        public TableCoords(int _col, int _row)
        {
            col = _col;
            row = _row;
        }

        public static TableCoords operator +(TableCoords lhs, TableCoords rhs)
        {
            return new TableCoords(lhs.col + rhs.col, lhs.row + rhs.row);
        }

        public static bool operator ==(TableCoords lhs, TableCoords rhs)
        {
            return lhs.col == rhs.col && lhs.row == rhs.row;
        }

        public static bool operator !=(TableCoords lhs, TableCoords rhs)
        {
            return !(lhs == rhs);
        }
    };

    struct Turn
    {
        public TableCoords block1;
        public TableCoords block2;

        public Turn(TableCoords _block1, TableCoords _block2)
        {
            block1 = _block1;
            block2 = _block2;
        }
    };

    struct GameInfo
    {
        public int score;
        public int gameTimeMillisecondsElapsed;
        public Turn? previousTurn;
        public TableCoords? curSelectedBlock;

        public void dropSelection()
        {
            curSelectedBlock = null;
        }

        public bool blockSelected()
        {
            return curSelectedBlock != null;
        }

        public bool turnMade()
        {
            return previousTurn != null;
        }
    };
}
