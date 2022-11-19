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

    private bool WhiteInTheInitialPosition()
    {
        return (team == 0 && currentY == 1);
    }

    private bool BlackInTheInitialPosition()
    {
        return (team == 1 && currentY == 6);
    }
}
