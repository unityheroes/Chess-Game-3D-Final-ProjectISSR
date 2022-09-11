using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMove(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // TOP RIGHT MOVEMENT
        for (int x = currentX+1 , y = currentY + 1; x < tileCountX && y <tileCountY; x++,y++) 
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    r.Add(new Vector2Int(x, y));
                }
             break;
            }         
        }

        // TOP LEFT MOVEMENT
        for (int x = currentX - 1, y = currentY + 1; x >= 0 && y < tileCountY; x--, y++)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    r.Add(new Vector2Int(x, y));
                }
             break;
            }
        }
        // Bottom Right MOVEMENT
        for (int x = currentX + 1, y = currentY - 1; x < tileCountX && y  >= 0; x++, y--)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    r.Add(new Vector2Int(x, y));
                }
            break;
            }
        }
        // Bottom LEFT MOVEMENT
        for (int x = currentX - 1, y = currentY - 1; x >= 0 && y >= 0; x--, y--)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    r.Add(new Vector2Int(x, y));
                }
             break;
            }
        }

        return r;

    }
}
