using System.Collections.Generic;
using UnityEngine;


public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMove(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // MOVEMENT RIGHT
        if (currentX + 1 < tileCountX)
        {
            //RIGHT
            if (board[currentX + 1, currentY] == null)
            {
                r.Add(new Vector2Int(currentX + 1, currentY));
            }
            else if (board[currentX + 1, currentY].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY));
            }
            //TOP RIGHT
            if (currentY + 1 < tileCountY)
            {
                //RIGHT
                if (board[currentX + 1, currentY + 1] == null)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
                }
                else if (board[currentX + 1, currentY + 1].team != team)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
                }
            }
            //Bottom RIGHT
            if (currentY - 1 > 0)
            {
                //RIGHT
                if (board[currentX + 1, currentY - 1] == null)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
                }
                else if (board[currentX + 1, currentY - 1].team != team)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
                }
            }

        }




        // MOVEMENT LEFT
        if (currentX - 1 >= 0)
        {
            //LEFT
            if (board[currentX - 1, currentY] == null)
            {
                r.Add(new Vector2Int(currentX - 1, currentY));
            }
            else if (board[currentX - 1, currentY].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY));
            }
            //TOP LEFT
            if (currentY + 1 < tileCountY)
            {
                
                if (board[currentX - 1, currentY + 1] == null)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
                }
                else if (board[currentX - 1, currentY + 1].team != team)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
                }
            }
            //Bottom LEFT
            if (currentY - 1 > 0)
            {
                //Left
                if (board[currentX - 1, currentY - 1] == null)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
                }
                else if (board[currentX - 1, currentY - 1].team != team)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
                }
            }

        }


        // MOVEMENT UP
        if (currentY + 1 < tileCountY)
            if (board[currentX, currentY + 1] == null || board[currentX,currentY+1].team != team)
            {
                r.Add(new Vector2Int(currentX, currentY + 1));
            }

        // MOVEMENT DOWN
        if (currentY - 1 >= 0)
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
            {
                r.Add(new Vector2Int(currentX, currentY - 1));
            }
        return r;
    } // Movement King

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        SpecialMove r = SpecialMove.None;
        var kingMove = movelist.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));   // 4 x-axis king  If White  y-axis = 0 else = 7 //lambda expression to check movement
        var LeftRook = movelist.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));   // 0 x-axis rook If White  y-axis = 0 else = 7 
        var RightRook = movelist.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));   // 7 x-axis rook If White  y-axis = 0 else = 7 
        if (kingMove == null && currentX == 4)
        {
            //white team 
            if(team == 0)
            {
                //left rook
                if (LeftRook == null) 
                    if(board[0,0].type == ChessPieceType.Rook) 
                        if(board[0,0].team==0)
                            if(board[3,0]==null)
                                if(board[2,0]==null)
                                    if (board[1, 0] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 0)); // movment king 
                                        r = SpecialMove.Castling;
                                    }
                //left rook
                if (RightRook == null)
                    if (board[7, 0].type == ChessPieceType.Rook)
                        if (board[7, 0].team == 0)
                            if (board[5, 0] == null)
                                if (board[6, 0] == null)                                    
                                {
                                    availableMoves.Add(new Vector2Int(6, 0)); // movment king 
                                    r = SpecialMove.Castling;
                                }
            }
            else
            {
                //left rook
                if (LeftRook == null)
                    if (board[0, 7].type == ChessPieceType.Rook)
                        if (board[0, 7].team == 1)
                            if (board[3, 7] == null)
                                if (board[2, 7] == null)
                                    if (board[1, 7] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 7)); // movment king 
                                        r = SpecialMove.Castling;
                                    }
                //left rook
                if (RightRook == null)
                    if (board[7, 7].type == ChessPieceType.Rook)
                        if (board[7, 7].team == 1)
                            if (board[5, 7] == null)
                                if (board[6, 7] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 7)); // movment king 
                                    r = SpecialMove.Castling;
                                }
            }
        }


        return r;
    }
}
