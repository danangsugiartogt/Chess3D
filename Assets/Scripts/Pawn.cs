using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        if ((currentY + direction) > 7 || (currentY + direction) < 0)
        {
            r.Add(new Vector2Int(currentX, currentY));
            return r;
        }

        // One in front
        if (board[currentX, currentY + direction] == null)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));
        }

        // Two in front
        if (board[currentX, currentY + direction] == null)
        {
            // for white team
            if (WhiteInTheInitialPosition() && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));

            // for black team
            if (BlackInTheInitialPosition() && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
        }

        // kill move
        if(currentX != tileCountX - 1)
        {
            var cp = board[currentX + 1, currentY + direction];
            if (cp != null && cp.team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
        }

        if (currentX != 0)
        {
            var cp = board[currentX - 1, currentY + direction];
            if (cp != null && cp.team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        }

        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;

        // En Passant
        if(moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if(board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn) // Make sure last moved piece was Pawn
            {
                if(Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2) // Make sure the last move was a +2 in either direction
                {
                   if(board[lastMove[1].x, lastMove[1].y].team != team) // Make sure the move is from another team
                    {
                        if(lastMove[1].y == currentY) // Make sure both pawn are on the same Y
                        {
                            if(lastMove[1].x == currentX - 1) // Landed on left
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }

                            if(lastMove[1].x == currentX + 1) // Landed on right
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }
                    }
                }
            }
        }

        return SpecialMove.None;
    }

    private bool WhiteInTheInitialPosition()
    {
        return (team == 0 && currentY == 1);
    }

    private bool BlackInTheInitialPosition()
    {
        return (team == 1 && currentY == 6);
    }
}
