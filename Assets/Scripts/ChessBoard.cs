using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class ChessBoard : MonoBehaviour
{
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private const string HOVER_LAYER = "Hover";
    private const string TILE_LAYER = "Tile";
    private const string HIGHLIGHT_LAYER = "Highlight";

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject staticButtonExit;
    [SerializeField] private TMP_Text text;

    [Header("Material")]
    [SerializeField] private Material tileMaterial;

    [Header("Board And Pieces Setting")]
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = .2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = .3f;
    [SerializeField] private float deathSpacing = .3f;
    [SerializeField] private float dragOffset = .3f;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] blackPieces;
    [SerializeField] private GameObject[] whitePieces;

    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private ChessPiece[,] chessPieces;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private bool isGameFinished = false;

    void Awake()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }
 
    // Update is called once per frame
    void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        if (isGameFinished) return;

        RaycastHit hit;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit, 100, LayerMask.GetMask(TILE_LAYER, HOVER_LAYER, HIGHLIGHT_LAYER)))
        {
            // Get index hit's tile
            var hitPosition = LookupTileIndex(hit.transform.gameObject);

            // first time hovering
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer(HOVER_LAYER);
            }

            // we have already hovering a tile and change the previous tile too
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? LayerMask.NameToLayer(HIGHLIGHT_LAYER) : LayerMask.NameToLayer(TILE_LAYER);
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer(HOVER_LAYER);
            }

            if (Input.GetMouseButtonDown(0))
            {
                var cp = chessPieces[hitPosition.x, hitPosition.y];

                if (cp != null)
                {
                    // is my turn
                    if (cp.team == 0)
                    {
                        currentlyDragging = cp;

                        // Get list of place that i can go, and highlight it
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        // Get list of special moves
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();
                        HighlightTiles();
                    }
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool isValidMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);

                if (!isValidMove)
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? LayerMask.NameToLayer(HIGHLIGHT_LAYER) : LayerMask.NameToLayer(TILE_LAYER);
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        // if we are dragging
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0f;
            if(horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    private void SetDeathPositionAndScalePiece(ChessPiece chessPiece, bool isWhiteKilled)
    {
        if (isWhiteKilled)
        {
            deadWhites.Add(chessPiece);
            chessPiece.SetScale(Vector3.one * deathSize);
            chessPiece.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else
        {
            deadBlacks.Add(chessPiece);
            chessPiece.SetScale(Vector3.one * deathSize);
            chessPiece.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }
    }
    private void CheckMate(int winningTeam)
    {
        isGameFinished = true;
        ShowWinPanel(winningTeam);
    }

    // Highlight Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer(HIGHLIGHT_LAYER);
        }
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer(TILE_LAYER);
        }

        availableMoves.Clear();
    }

    // Generate Board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        var tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
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

        tileObject.layer = LayerMask.NameToLayer(TILE_LAYER);
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spawning the pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        // White team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        // Black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        var pieces = team == 0 ? whitePieces : blackPieces;

        ChessPiece cp = Instantiate(pieces[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;

        return cp;
    }

    // Special Moves
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            var myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            var enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if(enemyPawn.team == 0)
                        SetDeathPositionAndScalePiece(enemyPawn, isWhiteKilled: false);
                    else
                        SetDeathPositionAndScalePiece(enemyPawn, isWhiteKilled: true);
                }

                chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
            }
        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                // White team
                if (targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }

                // Black team
                if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }

        if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            // Left Rook
            if(lastMove[1].x == 2)
            {
                if(lastMove[1].y == 0) // White team
                {
                    var rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if(lastMove[1].y == 7) // Black team
                {
                    var rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            // Left Rook
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // White team
                {
                    var rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // Black team
                {
                    var rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null && chessPieces[x, y].type == ChessPieceType.King && chessPieces[x, y].team == currentlyDragging.team)
                    targetKing = chessPieces[x, y];
            }
        }

        // since we are sending ref availableMoves, we will be deleting moves that are putting us in check
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save the current values, to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Going through all the moves, simulate them and check if we are in check
        for(int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSimulation = new Vector2Int(targetKing.currentX, targetKing.currentY);

            // Did we simulate the king's move
            if (cp.type == ChessPieceType.King)
                kingPositionThisSimulation = new Vector2Int(simX, simY);

            // Copy the [,] and not a reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simulationAttackingPieces = new List<ChessPiece>();

            for(int x = 0; x < TILE_COUNT_X; x++)
            {
                for(int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].team != cp.team)
                            simulationAttackingPieces.Add(simulation[x, y]);
                    }
                }
            }

            // Simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            // Did one of the piece got taken down during our simulation
            var deadPiece = simulationAttackingPieces.Find(x => x.currentX == simX && x.currentY == simY);
            if (deadPiece != null)
                simulationAttackingPieces.Remove(deadPiece);

            // Get all the simulated attacking pieces moves
            List<Vector2Int> simulationMoves = new List<Vector2Int>();
            for(int a = 0; a < simulationAttackingPieces.Count; a++)
            {
                var pieceMoves = simulationAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for(int b = 0; b < pieceMoves.Count; b++)
                {
                    simulationMoves.Add(pieceMoves[b]);
                }
            }

            // Is the king in trouble? if yes, remove the move
            if(ContainsValidMove(ref simulationMoves, kingPositionThisSimulation))
            {
                movesToRemove.Add(moves[i]);
            }

            // Restore the actual CP data
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        // Remove from the current available move list
        for(int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private bool CheckForCheckMate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = chessPieces[lastMove[1].x, lastMove[1].y].team == 0 ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if(chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }
            }
        }

        // Is the king attacked ?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int j = 0; j < pieceMoves.Count; j++)
            {
                currentAvailableMoves.Add(pieceMoves[j]);
            }
        }

        // Are we in check right now?
        if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            // King is under attack, can we move something to help him?
            for(int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                // since we are sending ref availableMoves, we will be deleting moves that are putting us in check
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return false;
            }

            return true; // Checkmate
        }

        return false;
    }

    // AI
    private void MoveAI()
    {
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        ChessPiece defendingKing = null;
        ChessPiece attackingKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == 0)
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            attackingKing = chessPieces[x, y];
                    }
                    else
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            defendingKing = chessPieces[x, y];
                    }
                }
            }
        }

        // Is the king attacked ?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

            if(pieceMoves.Count > 0)
            {
                for (int j = 0; j < pieceMoves.Count; j++)
                {
                    currentAvailableMoves.Add(pieceMoves[j]);
                }
            }
        }

        // Are we in check right now?
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(defendingKing.currentX, defendingKing.currentY)))
        {
            bool isCheckMate = true;
            // King is under attack, can we move something to help him?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, defendingKing);

                if (defendingMoves.Count > 0)
                {
                    isCheckMate = false;
                    break;
                }
            }

            if(isCheckMate)
                CheckMate(0); // Checkmate, white team win
        }

        // Collect defending pieces that contains available moves
        Dictionary<ChessPiece, List<Vector2Int>> defendingPiecesContainsAvailableMoves = new Dictionary<ChessPiece, List<Vector2Int>>();
        Dictionary<ChessPiece, Vector2Int> defendingPiecesContainsAvailableMoveAndKill = new Dictionary<ChessPiece, Vector2Int>();
        for (int i = 0; i < defendingPieces.Count; i++)
        {
            List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

            SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, defendingKing);

            if (defendingMoves.Count > 0)
            {
                for (int j = 0; j < defendingMoves.Count; j++)
                {
                    var pos = defendingMoves[j];
                    bool canKill = false;

                    for(int k = 0; k < attackingPieces.Count; k++)
                    {
                        if(attackingPieces[k].currentX == pos.x && attackingPieces[k].currentY == pos.y)
                        {
                            if(!defendingPiecesContainsAvailableMoveAndKill.ContainsKey(defendingPieces[i]))
                                defendingPiecesContainsAvailableMoveAndKill.Add(defendingPieces[i], defendingMoves[j]);

                            canKill = true;
                            break;
                        }
                    }

                    if(!canKill && !defendingPiecesContainsAvailableMoves.ContainsKey(defendingPieces[i]))
                        defendingPiecesContainsAvailableMoves.Add(defendingPieces[i], defendingMoves);
                }
            }
        }

        // Move to kill
        if(defendingPiecesContainsAvailableMoveAndKill.Count > 0)
        {
            int rand = UnityEngine.Random.Range(0, defendingPiecesContainsAvailableMoveAndKill.Count - 1);
            var cp = defendingPiecesContainsAvailableMoveAndKill.Keys.ElementAt(rand);
            var move = defendingPiecesContainsAvailableMoveAndKill[cp];

            MoveTo(cp, move.x, move.y);
        }
        // Free to move
        else
        {
            if(defendingPiecesContainsAvailableMoves.Count > 0)
            {
                int rand = UnityEngine.Random.Range(0, defendingPiecesContainsAvailableMoves.Count - 1);
                var cp = defendingPiecesContainsAvailableMoves.Keys.ElementAt(rand);
                var moves = defendingPiecesContainsAvailableMoves[cp];

                MoveTo(cp, moves[0].x, moves[0].y);
            }
            else
            {
                CheckMate(0);
            }
        }

        // Check mate to white team
        bool checkMate = true;
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

            SimulateMoveForSinglePiece(attackingPieces[i], ref pieceMoves, attackingKing);

            if (pieceMoves.Count != 0)
            {
                checkMate = false;
                break;
            }
        }

        if (checkMate) CheckMate(1);
    }

    // Operations
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)) && cp.team == 0)
            return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Is there another piece on the target position?
        if (chessPieces[x, y] != null)
        {
            ChessPiece otherCp = chessPieces[x, y];

            // If mine
            if (cp.team == otherCp.team) return false;

            // If opponent
            if (otherCp.team == 0)
            {
                if (otherCp.type == ChessPieceType.King)
                {
                    CheckMate(1);
                }
                    
                SetDeathPositionAndScalePiece(otherCp, isWhiteKilled: false);
            }
            else
            {
                if (otherCp.type == ChessPieceType.King)
                {
                    CheckMate(0);
                }

                SetDeathPositionAndScalePiece(otherCp, isWhiteKilled: true);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        if (!isWhiteTurn && !isGameFinished) MoveAI();

        ProcessSpecialMove();

        if (!isGameFinished)
            if (CheckForCheckMate())
                CheckMate(cp.team);

        return true;
    }

    // Positioning the pieces
    private void PositionAllPieces()
    {
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    // Helpers
    private Vector2Int LookupTileIndex(GameObject hitObject)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(tiles[x,y] == hitObject)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return -Vector2Int.one;
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for(int i = 0; i < moves.Count; i++)
        {
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        }

        return false;
    }

    // UI
    private void ShowWinPanel(int winningTeam)
    {
        if (winningTeam == 0)
            text.text = "White team wins!";
        else
            text.text = "Black team wins!";

        panel.SetActive(true);
        staticButtonExit.SetActive(false);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
