using System;
using System.Text;
using Discord;
using Newtonsoft.Json;

namespace ConnectBot.Templates
{
    public enum BoardStatus {Invalid, Full, Success}
    
    public class Board
    {
        /// <summary>
        /// The board itself. Note that it is 0-X, 0-Y, however is like a coordinate plane.
        /// Ex.
        /// (0, 2) (1, 2) (2, 2)
        /// (0, 1) (1, 1) (2, 1)
        /// (0, 0) (1, 0) (2, 0)
        /// </summary>
        [JsonProperty]
        private readonly int[,] _board;

        [JsonProperty]
        private readonly int _connect;

        [JsonIgnore] public int Width => _board.GetLength(0);
        
        [JsonIgnore] public int Height => _board.GetLength(1);

        public Board(int width = 7, int height = 6, int connect = 4)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Parameters cannot be negative!");
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Parameters cannot be negative!");
            if (connect < 0)
                throw new ArgumentOutOfRangeException(nameof(connect), "Parameters cannot be negative!");
            if (connect > width || connect > height || connect > Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2)))
                throw new ArgumentOutOfRangeException(nameof(connect), "Connect length must fit in board!");
            
            _board = new int[width, height];
            _connect = connect;
        }

        /// <summary>
        /// Drop a piece. Note that this starts from 1 because people are not programmers.
        /// </summary>
        /// <param name="width">A 1-indexed column.</param>
        /// <param name="value">The item to place.</param>
        /// <returns></returns>
        public BoardStatus Drop(int width, int value)
        {
            if (width <= 0 || width > _board.GetLength(0))
                return BoardStatus.Invalid;
            for (var i = 0; i < _board.GetLength(1); i++)
            {
                if (_board[width - 1, i] == 0)
                {
                    _board[width - 1, i] = value;
                    return BoardStatus.Success;
                }
            }
            
            return BoardStatus.Full;
        }
        
        public static readonly IndexedDictionary<string, Color> Colors = new IndexedDictionary<string, Color>
        {
            {":red_circle: ", Color.Red},
            {":blue_circle: ", Color.Blue},
            {":green_circle: ", Color.Green},
            {":yellow_circle: ", new Color(253, 203, 88)},
            {":purple_circle: ", Color.Purple},
            {":white_circle: ", new Color(230, 231, 232)},
            {":orange_circle: ", Color.Orange},
            {":brown_circle: ", new Color(193, 105, 79)},
            {":black_circle: ", new Color(49, 55, 61)}
        };

        public string GenerateField()
        {
            var output = new StringBuilder();
            for (var a = 0; a < _board.GetLength(0); a++)
            {
                for (var b = _board.GetLength(1) - 1; b >= 0; b--)
                    output.Append(Colors[_board[a, b]].Key);
                output.Remove(output.Length - 1, 1);
                output.Append('\n');
            }
            output.Remove(output.Length - 1, 1);
            return output.ToString();
        }

        public void Reset()
        {
            for(var a = 0; a < _board.GetLength(0); a++)
            for (var b = 0; b < _board.GetLength(1); b++)
                _board[a, b] = 0;
        }

        public int Winner()
        {
            var width = _board.GetLength(0);
            var height = _board.GetLength(1);
            
            //Horizontal
            for (var i = 0; i <= width - _connect; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var a = _board[i, j];
                    if (a == 0) continue;
                    var same = true;
                    for (var k = 1; k < _connect; k++)
                        same &= a == _board[i + k, j];
                    if (same)
                        return a;
                }
            }

            //Vertical
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j <= height - _connect; j++)
                {
                    var a = _board[i, j];
                    if (a == 0) continue;
                    var same = true;
                    for (var k = 1; k < _connect; k++)
                        same &= a == _board[i, j + k];
                    if (same)
                        return a;
                }
            }

            //Diag left ^> right

            for (var i = 0; i < width - _connect; i++)
            {
                for (var j = 0; j < height - _connect; j++)
                {
                    var a = _board[i, j];
                    if (a == 0) continue;
                    var same = true;
                    for (var k = 1; k < _connect; k++)
                        same &= a == _board[i + k, j + k];
                    if (same)
                        return a;
                }
            }

            //Diag right <^ left

            for (var i = _connect - 1; i < width; i++)
            {
                for (var j = 0; j <= height - _connect; j++)
                {
                    var a = _board[i, j];
                    if (a == 0) continue;
                    var same = true;
                    for (var k = 1; k < _connect; k++)
                        same &= a == _board[i - k, i + k];
                    if (same)
                        return a;
                }
            }

            return 0;
        }
    }
}