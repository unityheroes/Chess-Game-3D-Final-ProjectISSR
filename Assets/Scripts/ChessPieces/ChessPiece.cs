using System.Collections.Generic;
using UnityEngine;


public enum ChessPieceType
{
    none = 0,
    pawn = 1,
    Rook =2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6,
}
public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one; // vector3 defuat value =0 we change it to 1

    private void Start()
    {
        // to fix black Knight rotation in start game while he spwaning
        //     white team no change       black team rotation 180 dagree
        transform.rotation = Quaternion.Euler((team == 0) ? Vector3.zero : new Vector3(0, 180, 0));
    }
  
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMove(ref ChessPiece[,] board ,int tileCountX,int tileCountY)//to make hightlight tiles
        
    {
        List<Vector2Int> r = new List<Vector2Int>();

        //test hightlight {center postion } and change it later 
        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));
        return r;
    }

    public virtual SpecialMove GetSpecialMoves(ref ChessPiece[,] board , ref List<Vector2Int[]> movelist , ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }

    public virtual void SetPosition(Vector3 position , bool force = false) // to movement Smooth 
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false) // to movement Smooth 
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;

    }


}
