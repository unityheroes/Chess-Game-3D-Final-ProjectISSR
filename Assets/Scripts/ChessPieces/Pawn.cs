using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    
    public override List<Vector2Int> GetAvailableMove(ref ChessPiece[,] board , int tileCountX, int tileCountY)
    {
        // Rule for Pawn
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;  // One in Front
        // one step in front
        if (board[currentX, currentY + direction] == null)
            r.Add(new Vector2Int(currentX, currentY + direction)); // to move one step 
        // two steps in front if pwan is 1st movement
        if (board[currentX, currentY + direction] == null)
        {   
            //WhiteTeam                                0        Y      +  1*2 
            if(team == 0 && currentY == 1 && board[currentX,currentY+direction * 2] == null)
                    r.Add(new Vector2Int(currentX, currentY + direction *2));

            //WhiteTeam                                0        Y      +  1*2            empty
            if (team == 1 && currentY == 6 && board[currentX, currentY + direction * 2] == null)
                r.Add(new Vector2Int(currentX, currentY + direction * 2));

        }
        //Kill Move
        if(currentX !=tileCountX-1)
        {      //        x+1      y        1                                  //check if enemy team
            if(board[currentX+1,currentY+direction]!=null && board[currentX + 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        }
        if (currentX != 0)
        {      //        x-1             y        1                           //check if enemy team
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
            }
        }

        return r;

    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;
        if((team ==0 &&currentY==6)|| (team == 1 && currentY == 1)) // check if pawn if need promotion
        {
            return SpecialMove.Promotion;
        }

        //En Passant movement 

        if (movelist.Count > 0)
        {
            Vector2Int[] lasttMove = movelist[movelist.Count - 1];

            // if its 1st move and type pwan
            if(board[ lasttMove[1].x,lasttMove[1].y ].type == ChessPieceType.pawn) // if the last piece moved is pwan
            {
                if (Mathf.Abs(lasttMove[0].y - lasttMove[1].y ) == 2) // if the last move was a +2 in either direction
                {
                    if(board[lasttMove[1].x, lasttMove[1].y].team != team) // if the move was from another team 
                    {
                        if (lasttMove[1].y == currentY)  // if both pawns are one in same Y-axis 
                        {
                            if (lasttMove[1].x == currentX - 1) // landed left 
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                            if (lasttMove[1].x == currentX + 1) // landed right 
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
}
