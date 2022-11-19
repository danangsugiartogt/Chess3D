using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Right
        if(currentX + 1 < tileCountX)
        {
            // Right
            if (board[currentX + 1, currentY] == null)
                r.Add(new Vector2Int(currentX + 1, currentY));
            else if (board[currentX + 1, currentY].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY));

            // Top right
            if (currentY + 1 < tileCountY)
            {
                if (board[currentX + 1, currentY + 1] == null)
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
                else if (board[currentX + 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
            }

            // Bottom right
            if (currentY - 1 >= 0)
            {
                if (board[currentX + 1, currentY - 1] == null)
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
                else if (board[currentX + 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
            }
        }

        // Left
        if (currentX - 1 >= 0)
        {
            // Right
            if (board[currentX - 1, currentY] == null)
                r.Add(new Vector2Int(currentX - 1, currentY));
            else if (board[currentX - 1, currentY].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY));

            // Top right
            if (currentY + 1 < tileCountY)
            {
                if (board[currentX - 1, currentY + 1] == null)
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
                else if (board[currentX - 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
            }

            // Bottom right
            if (currentY - 1 >= 0)
            {
                if (board[currentX - 1, currentY - 1] == null)
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
                else if (board[currentX - 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
            }
        }

        // Up
        if(currentY + 1 < tileCountY)
        {
            if (board[currentX, currentY + 1] == null || board[currentX, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX, currentY + 1));
        }

        // Down
        if (currentY - 1 >= 0)
        {
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX, currentY - 1));
        }

        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove r = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        if (kingMove == null && currentX == 4)
        {
            // White team
            if (team == 0)
            {
                // Left Rook
                if (leftRook == null && board[0, 0].type == ChessPieceType.Rook)
                {
                    // Make sure there is no obstraction in left
                    if (board[0, 0].team == 0 && board[3, 0] == null && board[2, 0] == null && board[1, 0] == null)
                    {
                        availableMoves.Add(new Vector2Int(2, 0));
                        r = SpecialMove.Castling;
                    }
                }

                // Right Rook
                if (rightRook == null && board[7, 0].type == ChessPieceType.Rook)
                {
                    // Make sure there is no obstraction in right
                    if (board[7, 0].team == 0 && board[5, 0] == null && board[6, 0] == null)
                    {
                        availableMoves.Add(new Vector2Int(6, 0));
                        r = SpecialMove.Castling;
                    }
                }
            }
            else
            {
                // Left Rook
                if (leftRook == null && board[0, 7].type == ChessPieceType.Rook)
                {
                    // Make sure there is no obstraction in left
                    if (board[0, 7].team == 1 && board[3, 7] == null && board[2, 7] == null && board[1, 7] == null)
                    {
                        availableMoves.Add(new Vector2Int(2, 7));
                        r = SpecialMove.Castling;
                    }
                }

                // Right Rook
                if (rightRook == null && board[7, 7].type == ChessPieceType.Rook)
                {
                    // Make sure there is no obstraction in right
                    if (board[7, 7].team == 1 && board[5, 7] == null && board[6, 7] == null)
                    {
                        availableMoves.Add(new Vector2Int(6, 7));
                        r = SpecialMove.Castling;
                    }
                }
            }
        }

        return r;
    }
}
