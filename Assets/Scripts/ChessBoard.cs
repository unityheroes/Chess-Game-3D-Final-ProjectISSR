using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialMove
{
    None = 0,
    EnPassant,  // ref https://www.youtube.com/watch?v=c_KRIH0wnhE
    Castling,  // https://www.youtube.com/watch?v=FcLYgXCkucc
    Promotion // https://www.youtube.com/shorts/Tt8VTZFPFa4
}
public class ChessBoard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] float tileSize = 1.0f;
    [SerializeField] float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    [SerializeField] private float DeathSpacing = 0.3f;
    [SerializeField] private float deathSize = 0.3f;

    [SerializeField] private float dragOffset = 1f; // to fix magrge while two piece while moves and that value controle y to make it up chess board
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs\" Please look ChessPieces.cs\" && Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;


    //LOGIC

    private const int TITLE_COUNT_X = 8;
    private const int TITLE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging; // for method drag mouse
    private List<ChessPiece> DeadWhite = new List<ChessPiece>(); //remove dead White pieces
    private List<ChessPiece> DeadBlack = new List<ChessPiece>(); //remove dead black pieces
    private Vector3 bounds;



    private bool isWhiteTurn; //  turn mechanic to  make white play firast and swaping between 2 player white and black

    private List<Vector2Int> availableMoves = new List<Vector2Int>(); ////to make hightlight tiles

    private List<Vector2Int[]> movelist = new List<Vector2Int[]>();         //to save movement for each pieces "for special moves"
    private SpecialMove specialMove;
    // MultiPlayer Logic
    private int playerCount = -1; // for server
    private int currentTeam = -1;  // for server and client
    private bool localGame = true; // To local Game Fix
    private bool[] PlayerRematch = new bool[2];

    private void Start()
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TITLE_COUNT_X, TITLE_COUNT_Y);
        SpwanAllPieces();
        PositionAllPieces();
        RegisterEvents();  // Listening for net Welcome Massage
    }
    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {    // Get the index of the tile i have hit    
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover == -Vector2Int.one)
            {
                // First time Hovering
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovering a tile ,change the prevous one 
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // drag mouse pics  "if we down on mouse "
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //is it out turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam ==0) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam ==1))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //to make hightlight tiles
                        // Get aList of where i can go and hightlight tiles
                        availableMoves = currentlyDragging.GetAvailableMove(ref chessPieces, TITLE_COUNT_X, TITLE_COUNT_Y);
                        // Get a List Special moves
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref movelist, ref availableMoves);
                        PreventCheck(); // Lock if CheckMate! 
                        HighlightTiles();
                    }
                }

            }

            //if we releasing the mouse button and draging
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                if (ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y)))
                {
                  MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y); // check if can move
                    // Net Implementation
                    NetMakeMove makemove = new NetMakeMove();
                    makemove.originalX = previousPosition.x;
                    makemove.originalY = previousPosition.y;
                    makemove.destinationX = hitPosition.x;
                    makemove.destinationY= hitPosition.y;
                    makemove.teamId = currentTeam;
                    Client.Instance.SendToServer(makemove);
                }
                else
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y)); //back prev postion 
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
               
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0)) //Fix Bug Dropping reference on release
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        // we are dragging a piece 
        if (currentlyDragging)
        {
            Plane horizonatalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizonatalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            // fix magrge between two pieces and make movment whith incressing in y-axis 0.9float
            // postion change while dragging
        }
    }



    // Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);




    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}", "Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();
        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();
        return tileObject;

    }
    // Spwan of the Pieces
    private void SpwanAllPieces()
    {
        chessPieces = new ChessPiece[TITLE_COUNT_X, TITLE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;
        // white team using ref in 2nd day in google drive image 
        chessPieces[0, 0] = SpwanSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpwanSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpwanSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpwanSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpwanSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpwanSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpwanSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpwanSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TITLE_COUNT_X; i++) // spwan pwan white team
        {
            chessPieces[i, 1] = SpwanSinglePiece(ChessPieceType.pawn, whiteTeam);
        }
        // black team using ref in 2nd day in google drive image 
        chessPieces[0, 7] = SpwanSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpwanSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpwanSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpwanSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpwanSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpwanSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpwanSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpwanSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TITLE_COUNT_X; i++) // spwan pwan black team
        {
            chessPieces[i, 6] = SpwanSinglePiece(ChessPieceType.pawn, blackTeam);
        }
    }
    private ChessPiece SpwanSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); // to spwan piecs from chessPieces
        cp.type = type;
        cp.team = team;
        // note if team 0 i add 0 if team black i add 6  
        cp.GetComponent<MeshRenderer>().material = teamMaterials[((team == 0) ? 0 : 6) + ((int)type - 1)]; // to adding material in GUI Unity for every piecs 
        return cp;
    }


    // Positioning of spwaned pieces
    private void PositionAllPieces() // to set position for all pieces 
    {
        for (int x = 0; x < TITLE_COUNT_X; x++)
            for (int y = 0; y < TITLE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);


    }
    private void PositionSinglePiece(int x, int y, bool force = false)  // to use it in for loop in method positionAllPieces to set position for pieces
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //HighLight Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear(); //to clear list 
    }
    // CheckMate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam) // to display who win
    {
        victoryScreen.SetActive(true); // to show Vectory Screen UI game opject
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true); // to show text who win i do it in gui by adding text
    }

    public void OnRematchButton()
    {
        if (localGame)
        {
            NetRematch whiterematch = new NetRematch();
            whiterematch.teamId = 0;
            whiterematch.WantRematch = 1;
            Client.Instance.SendToServer(whiterematch);

            NetRematch blackrematch = new NetRematch();
            blackrematch.teamId = 1;
            blackrematch.WantRematch = 1;
            Client.Instance.SendToServer(blackrematch);
        }
        else
        {
            NetRematch rematch = new NetRematch();
            rematch.teamId = currentTeam;
            rematch.WantRematch = 1;
            Client.Instance.SendToServer(rematch);
            
        }
    }
    public void GameReset()
    {
        //UI Canvas
        rematchButton.interactable = true;
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false); // to hide text if winner team white
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false); // to hide text if winner team white
        victoryScreen.SetActive(false); // to hide VectoryScreen "Gameobject"
        //Field Reset
        currentlyDragging = null;
        availableMoves.Clear();
        movelist.Clear(); // updated to clear move list
        PlayerRematch[0] = PlayerRematch[1] = false;
        // Cleen up
        for (int x = 0; x < TITLE_COUNT_X; x++)
        {
            for (int y = 0; y < TITLE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                { Destroy(chessPieces[x, y].gameObject); }
                chessPieces[x, y] = null;
            }
        }
        for (int i = 0; i < DeadWhite.Count; i++)
        {
            Destroy(DeadWhite[i].gameObject);
        }
        for (int i = 0; i < DeadBlack.Count; i++)
        {
            Destroy(DeadBlack[i].gameObject);
        }
        DeadWhite.Clear(); DeadBlack.Clear();
        // spwan all pieces 

        SpwanAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;

    }
    public void OnMenuButton()
    {
        NetRematch rematch = new NetRematch();
        rematch.teamId = currentTeam;
        rematch.WantRematch = 0;
        Client.Instance.SendToServer(rematch);
        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();
        Invoke("ShutdownRelay", 1.0f);

         //reset Some Values
         playerCount = -1;
        currentTeam = -1;
    }

    //Special Moves
    //to check and and destroy enemy  
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = movelist[movelist.Count - 1];                // keep track position
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var TargetPawnPosition = movelist[movelist.Count - 2];     // keep track position 
            ChessPiece enemyPwan = chessPieces[TargetPawnPosition[1].x, TargetPawnPosition[1].y];
            if (myPawn.currentX == enemyPwan.currentX)
            {
                if (myPawn.currentY == enemyPwan.currentY - 1 || myPawn.currentY == enemyPwan.currentY + 1)
                {
                    if (enemyPwan.team == 0)
                    {
                        DeadWhite.Add(enemyPwan);
                        DeadWhite.Add(enemyPwan);
                        enemyPwan.SetScale(Vector3.one * deathSize); // to Decrease he scale
                        enemyPwan.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * DeathSpacing) * DeadWhite.Count); // change postion dead white outside 

                    }
                    else
                    {

                        DeadBlack.Add(enemyPwan);
                        DeadBlack.Add(enemyPwan);
                        enemyPwan.SetScale(Vector3.one * deathSize); // to Decrease he scale
                        enemyPwan.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * DeathSpacing) * DeadBlack.Count); // change postion dead black outside 


                    }
                    chessPieces[enemyPwan.currentX, enemyPwan.currentY] = null;
                }
            }
        }
        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastmove = movelist[movelist.Count - 1];
            ChessPiece TargetPwan = chessPieces[lastmove[1].x, lastmove[1].y];
            if(TargetPwan.type == ChessPieceType.pawn)
            {
                if(TargetPwan.team == 0 && lastmove[1].y == 7) // white team
                {
                    ChessPiece newQueen = SpwanSinglePiece(ChessPieceType.Queen, 0); // spwan gameobject queen and material white 
                    newQueen.transform.position = chessPieces[lastmove[1].x, lastmove[1].y].transform.position;
                    Destroy(chessPieces[lastmove[1].x, lastmove[1].y].gameObject); // destroy pwan
                    chessPieces[lastmove[1].x, lastmove[1].y] = newQueen;
                    PositionSinglePiece(lastmove[1].x, lastmove[1].y); // set new position for queen  
                }
                if (TargetPwan.team == 1 && lastmove[1].y == 0) // black team
                {
                    ChessPiece newQueen = SpwanSinglePiece(ChessPieceType.Queen, 1); // spwan gameobject queen and material black 
                    newQueen.transform.position = chessPieces[lastmove[1].x, lastmove[1].y].transform.position;
                    Destroy(chessPieces[lastmove[1].x, lastmove[1].y].gameObject); // destroy pwan
                    chessPieces[lastmove[1].x, lastmove[1].y] = newQueen;
                    PositionSinglePiece(lastmove[1].x, lastmove[1].y); // set new position for queen  
                }
            }

        }
        if (specialMove == SpecialMove.Castling)
        {
            var lastMove = movelist[movelist.Count - 1];

            //left rook
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // white team 
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;


                }
                else if (lastMove[1].y == 7) // black team
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            //right rook
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // white team 
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;


                }
                else if (lastMove[1].y == 7) // black team
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }

    }
    private void PreventCheck() // to check if prevent "Before Move"
    {
        ChessPiece TargetKing = null;
        for (int x = 0; x < TITLE_COUNT_X; x++)
        {
            for (int y = 0; y < TITLE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null) 
                    if (chessPieces[x,y].type == ChessPieceType.King)
                  {
                    if (chessPieces[x, y].team == currentlyDragging.team)
                        TargetKing = chessPieces[x, y];
                  }

            }
            // Since we sending ref availablemoves , we will be deleting moves that are putting us  in check
           
        }
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, TargetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp , ref List<Vector2Int>moves,ChessPiece targetKing ) // to Simulate move
    {
        // Save the current values  , to reset after function call 
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();
        // going through all the moves , simualte them and check if we are in check
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;
            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // Did we simulate the king move 
            if(cp.type == ChessPieceType.King)
            {
                kingPositionThisSim = new Vector2Int(simX, simY);
            }
            // copy the [][]array and not reference
            ChessPiece[,] simulation = new ChessPiece[TITLE_COUNT_X,TITLE_COUNT_Y]; // to simulate The board
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>(); // another team 
            for (int x = 0; x < TITLE_COUNT_X; x++)
            {
                for (int y = 0; y < TITLE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].team != cp.team) 
                        { 
                            simAttackingPieces.Add(simulation[x, y]);
                        }
                    }
                }
            }
            //Simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;
            // Did one of the piece got taken down during our simulation
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY); //lambda expression 
            if (deadPiece != null)
            {
                simAttackingPieces.Remove(deadPiece);
            }
            // Get All Simulated Attacking pieces Moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetAvailableMove(ref simulation, TITLE_COUNT_X, TITLE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simMoves.Add(pieceMoves[b]);                
            }
            // If king is trouble ? then remove the move 
            if(ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }
            // Restore the actual chesspiecs "cp " data 
            cp.currentX = actualX;
            cp.currentY = actualY;
        }
        // Remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }

    }
    //
    private bool CheckForCheckMate() // after move
    {
        var lastMove = movelist[movelist.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece TargetKing = null;
        for (int x = 0; x < TITLE_COUNT_X; x++)
        {
            for (int y = 0; y < TITLE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                        {
                            TargetKing = chessPieces[x, y];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }

            }
        }
        // is the king  attacked rightnow ?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMove(ref chessPieces, TITLE_COUNT_X, TITLE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
                currentAvailableMoves.Add(pieceMoves[b]);
        }
        // Are we in check right now ?
        if(ContainsValidMove(ref currentAvailableMoves , new Vector2Int(TargetKing.currentX , TargetKing.currentY)))
        {
            //King in under attack , can we move something to help thim ?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMove(ref chessPieces,TITLE_COUNT_X,TITLE_COUNT_Y);
                //Since we are sending ref AvailableMoves , we will be deleting moves that are putting us in check
                SimulateMoveForSinglePiece(defendingPieces[i],ref defendingMoves , TargetKing);
                if (defendingMoves.Count != 0)
                {
                    return false;
                }
            }
            return true; // CheckMate exit
        }
        return false;
    }
    //Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int position)
        {
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].x == position.x && moves[i].y == position.y)
                    return true;
            return false;
        }
        private void MoveTo(int originalX, int originalY, int x, int y)
        {
           
            ChessPiece cp = chessPieces[originalX, originalY];
            Vector2Int previousPosition = new Vector2Int(originalX, originalY);
            // Is There another piece on the target position
            if (chessPieces[x, y] != null)
            {
                ChessPiece otherCp = chessPieces[x, y];
                if (cp.team == otherCp.team)
                    return ;
                //if its the enemy team
                if (otherCp.team == 0) // white team 
                {
                    if (otherCp.type == ChessPieceType.King) // Win Condition
                    {
                        CheckMate(1); // black team
                    }
                    DeadWhite.Add(otherCp);
                    otherCp.SetScale(Vector3.one * deathSize); // to Decrease he scale
                    otherCp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                        - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * DeathSpacing) * DeadWhite.Count); // change postion dead white outside
                }
                else if (otherCp.team == 1) // black team 
                {
                    if (otherCp.type == ChessPieceType.King) // Win Condition
                    {
                        CheckMate(0); // White team
                    }
                    DeadBlack.Add(otherCp);
                    otherCp.SetScale(Vector3.one * deathSize); // to Decrease he scale
                    otherCp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                      - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * DeathSpacing) * DeadBlack.Count); // change postion dead black outside
                }
            }

            chessPieces[x, y] = cp;
            chessPieces[previousPosition.x, previousPosition.y] = null;
            PositionSinglePiece(x, y);
            isWhiteTurn = !isWhiteTurn;
        if (localGame)
        {
            currentTeam = (currentTeam == 0) ? 1 : 0; // fix local game to swaping between 0,1;
        }               
            movelist.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) }); // store prev postion and new positon to make special moves
            ProcessSpecialMove();  // do check and do to speacial moves
        if (currentlyDragging)
        {
            currentlyDragging = null;
        }
            RemoveHighlightTiles();
        if (CheckForCheckMate())
        {
            CheckMate(cp.team); // cp Current team 
        }
            return ;
        }
        private Vector2Int LookupTileIndex(GameObject hitInfo)
        {
            for (int x = 0; x < TITLE_COUNT_X; x++)
                for (int y = 0; y < TITLE_COUNT_Y; y++)
                    if (tiles[x, y] == hitInfo)
                        return new Vector2Int(x, y);

            return -Vector2Int.one; //-1-1 Invalid


        }
    // Networking 
    #region
   private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SetlocalGame += OnSetLocalGame;

    }


    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_WELCOME -= OnWelcomeClient;        
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_REMATCH -= OnRematchClient;
        GameUI.Instance.SetlocalGame -= OnSetLocalGame;
    }
    //Server
    private void OnWelcomeServer(NetMessages msg, NetworkConnection cnn)
    {
        // Client Has Connected  , Assign The Team and return the message back to him
        NetWelcome nw = msg as NetWelcome;
        //Assign Team
        nw.AssignedTeam = ++playerCount;
        //Return Back to The Client
        Server.Instance.SendToClient(cnn,nw);
        //If Full, start The Game 
        if(playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame()); 
        }
    }
    //Client
    private void OnWelcomeClient(NetMessages msg)
    {
        // Receive the Connection Message
        NetWelcome nw = msg as NetWelcome;
        //Assign Team Team
        currentTeam = nw.AssignedTeam;
        Debug.Log($"My Assign Team is {nw.AssignedTeam}");
        if(localGame && currentTeam == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void OnStartGameClient(NetMessages msg)
    {
        //Change The Camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? cameraAngle.whiteTeam : cameraAngle.blackTeam);      
        }
    private void OnMakeMoveServer(NetMessages msg, NetworkConnection cnn)
    {
        // Receive the message , broadcast it back
        NetMakeMove makemove = msg as NetMakeMove;
        //This is Where you could do some validation check later 
        //-- code
        

        // Receive , and just broadcast  it back
        Server.Instance.Broadcast(makemove);
        
    }
    private void OnRematchServer(NetMessages msg, NetworkConnection cnn)
    {
        
        
        Server.Instance.Broadcast(msg);

    }
    private void OnMakeMoveClient(NetMessages msg)
    {
        NetMakeMove makemove = msg as NetMakeMove;
        Debug.Log($"makemove : {makemove.teamId}  {makemove.originalX}:{makemove.originalY} --->{makemove.destinationX} {makemove.destinationY}");
        if (makemove.teamId != currentTeam)
        {
            ChessPiece target = chessPieces[makemove.originalX, makemove.originalY];
            availableMoves = target.GetAvailableMove(ref chessPieces,TITLE_COUNT_X,TITLE_COUNT_Y);
            specialMove = target.GetSpecialMoves(ref chessPieces, ref movelist, ref availableMoves);
            MoveTo(makemove.originalX, makemove.originalY, makemove.destinationX, makemove.destinationY);
        }


    }
    private void OnRematchClient(NetMessages msg)
    {
        // Receive the connection message
        NetRematch rematch = msg as NetRematch;
        // Set the boolean for rematch
        PlayerRematch[rematch.teamId] = rematch.WantRematch == 1;
        // Active the piece of UI
        if(rematch.teamId != currentTeam)
        {
            rematchIndicator.transform.GetChild((rematch.WantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if(rematch.WantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }
        // if both wants to rematch
        if (PlayerRematch[0] && PlayerRematch[1])
        {
            GameReset();
        }
    }
    // 
    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }
        private void OnSetLocalGame(bool value)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = value;
    }
    #endregion


}
