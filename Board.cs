using System;
using System.Collections.Generic;

namespace RussianCheckers;

public class Board
{
    public const int size = 8;

    public enum Player { White, Black }
    public Player CurrentPlayer { get; private set; } = Player.White;

    public class Piece
    {
        public Player Owner { get; set; }
        public bool IsKing { get; set; } = false;
    }

    private Piece[,] board = new Piece[size, size];

    public Board()
    {
        SetupBoard();
    }

    private void SetupBoard()
    {
        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                if ((row + col) % 2 == 1 && row < size / 2 - 1)
                    board[row, col] = new Piece { Owner = Player.Black };
                else if ((row + col) % 2 == 1 && row >= size / 2 + 1)
                    board[row, col] = new Piece { Owner = Player.White };
            }
    }

    public Piece this[int row, int col]
    {
        get => board[row, col];
        set => board[row, col] = value;
    }

    public bool TryMove(int srcRow, int srcCol, int tarRow, int tarCol)
    {
        var piece = board[srcRow, srcCol];
        if (piece == null) return false;

        bool mustCapture = CanCapture(piece.Owner);

        int dirRow = tarRow - srcRow;
        int dirCol = tarCol - srcCol;

        int absDirRow = Math.Abs(dirRow);
        int absDirCol = Math.Abs(dirCol);

        if (absDirRow != absDirCol) return false;

        if (!piece.IsKing)
        {
            if (absDirRow == 1 && board[tarRow, tarCol] == null)
            {
                if (mustCapture) return false;

                board[tarRow, tarCol] = piece;
                board[srcRow, srcCol] = null;

                PromoteToKing(piece, tarRow);

                SwitchPlayer();

                return true;
            }

            if (absDirRow == 2 && board[tarRow, tarCol] == null)
            {
                int midRow = srcRow + dirRow / 2;
                int midCol = srcCol + dirCol / 2;

                var middle = board[midRow, midCol];

                if (middle != null && middle.Owner != piece.Owner)
                {
                    board[tarRow, tarCol] = piece;
                    board[srcRow, srcCol] = null;
                    board[midRow, midCol] = null;

                    if (CanBasicCapture(tarRow, tarCol))
                        return true;

                    PromoteToKing(piece, tarRow);

                    SwitchPlayer();

                    return true;
                }
            }
        }

        if (piece.IsKing)
        {
            int stepRow = dirRow / absDirRow;
            int stepCol = dirCol / absDirCol;

            int row = srcRow + stepRow;
            int col = srcCol + stepCol;

            Piece captured = null;
            int capturedRow = -1, capturedCol = -1;

            while (row != tarRow && col != tarCol)
            {
                if (board[row, col] != null)
                {
                    if (board[row, col].Owner == piece.Owner) return false;
                    if (captured != null) return false;

                    captured = board[row, col];
                    capturedRow = row; capturedCol = col;
                }

                row += stepRow; col += stepCol;
            }

            if (board[tarRow, tarCol] != null) return false;
            if (mustCapture && captured == null) return false;

            board[tarRow, tarCol] = piece;
            board[srcRow, srcCol] = null;

            if (captured != null) board[capturedRow, capturedCol] = null;

            if (captured != null && CanKingCapture(tarRow, tarCol))
                return true;

            SwitchPlayer();

            return true;
        }

        return false;
    }

    private void PromoteToKing(Piece piece, int row)
    {
        if (piece.Owner == Player.White && row == 0) piece.IsKing = true;
        if (piece.Owner == Player.Black && row == size - 1) piece.IsKing = true;
    }

    public void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == Player.White ? Player.Black : Player.White;
    }

    public bool CheckWin(out Player winner)
    {
        bool white = false, black = false;

        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                var checker = board[row, col];
                if (checker != null)
                {
                    if (checker.Owner == Player.White) white = true;
                    if (checker.Owner == Player.Black) black = true;
                }
            }

        if (!white) { winner = Player.Black; return true; }
        if (!black) { winner = Player.White; return true; }

        winner = default;
        return false;
    }

    public bool CanCapture(Player player)
    {
        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                var checker = board[row, col];
                if (checker != null && checker.Owner == player)
                {
                    if (!checker.IsKing && CanBasicCapture(row, col)) return true;
                    if (checker.IsKing && CanKingCapture(row, col)) return true;
                }
            }

        return false;
    }

    private bool CanBasicCapture(int row, int col)
    {
        var checker = board[row, col];
        if (checker == null || checker.IsKing) return false;

        int[] dirRow = { -2, -2, 2, 2 };
        int[] dirCol = { -2, 2, -2, 2 };

        for (int i = 0; i < dirRow.Length; i++)
        {
            int tarRow = row + dirRow[i]; 
            int tarCol = col + dirCol[i];
            if (tarRow < 0 || tarRow >= size || tarCol < 0 || tarCol >= size) continue;

            int midRow = row + dirRow[i] / 2;
            int midCol = col + dirCol[i] / 2;

            var middle = board[midRow, midCol];
            if (middle != null && middle.Owner != checker.Owner && board[tarRow, tarCol] == null)
                return true;
        }
        return false;
    }

    private bool CanKingCapture(int row, int col)
    {
        var checker = board[row, col];
        if (checker == null || !checker.IsKing) return false;

        (int dirRow, int dirCol)[] directions =
        {
            (-1, -1), (-1, 1), (1, -1), (1, 1)
        };

        foreach (var (dirRow, dirCol) in directions)
        {
            int curRow = row + dirRow;
            int curCol = col + dirCol;
            bool jumped = false;

            while (curRow >= 0 && curRow < size && curCol >= 0 && curCol < size)
            {
                if (board[curRow, curCol] == null)
                {
                    curRow += dirRow;
                    curCol += dirCol;
                    continue; 
                }

                if (board[curRow, curCol].Owner == checker.Owner) break;

                if (!jumped)
                {
                    jumped = true;
                    curRow += dirRow;
                    curCol += dirCol;
                    continue; 
                }

                break;
            }

            if (jumped && curRow >= 0 && curRow < size && curCol >= 0 && curCol < size)
                return true;
        }

        return false;
    }

    public List<((int, int) from, (int, int) to)> GetAllMoves(Player player)
    {
        var moves = new List<((int, int), (int, int))>();

        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                var checker = board[row, col];
                if (checker != null && checker.Owner == player)
                {
                    var targets = GetMovesForPiece(row, col);

                    foreach (var t in targets)
                        moves.Add(((row, col), t));
                }
            }

        return moves;
    }

    public List<(int, int)> GetMovesForPiece(int row, int col)
    {
        var moves = new List<(int, int)>();

        var checker = board[row, col];
        if (checker == null) return moves;

        (int dirRow, int dirCol)[] directions =
        {
            (-1, -1), (-1, 1), (1, -1), (1, 1)
        };
        
        bool mustCapture = CanCapture(checker.Owner);

        if (!checker.IsKing)
        {
            foreach (var (dirRow, dirCol) in directions)
            {
                int normalRow = row + dirRow;
                int normalCol = col + dirCol;
                
                if (normalRow >= 0 && normalRow < size &&
                    normalCol >= 0 && normalCol < size &&
                    board[normalRow, normalCol] == null &&
                    !mustCapture)

                    moves.Add((normalRow, normalCol));

                int jumpedRow = row + 2 * dirRow;
                int jumpedCol = col + 2 * dirCol;

                if (jumpedRow >= 0 && jumpedRow < size &&
                    jumpedCol >= 0 && jumpedCol < size)
                {
                    var mid = board[row + dirRow, col + dirCol];
                    if (mid != null && mid.Owner != checker.Owner && board[jumpedRow, jumpedCol] == null)
                        moves.Add((jumpedRow, jumpedCol));
                }
            }
        }
        else
        {
            foreach (var (dirRow, dirCol) in directions)
            {
                int curRow = row + dirRow;
                int curCol = col + dirCol;

                bool jumped = false;

                while (curRow >= 0 && curRow < size && curCol >= 0 && curCol < size)
                {
                    if (board[curRow, curCol] == null)
                    {
                        if (!mustCapture || jumped)
                            moves.Add((curRow, curCol));

                        curRow += dirRow;
                        curCol += dirCol; 

                        continue;
                    }
                    if (board[curRow, curCol].Owner == checker.Owner) break;

                    if (!jumped)
                    {
                        jumped = true;
                        
                        curRow += dirRow;
                        curCol += dirCol;

                        continue;
                    }
                    
                    break;
                }
            }
        }

        return moves;
    }
}
