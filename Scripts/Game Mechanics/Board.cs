using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public static Board Instance { set; get; }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    private Piece selectedPiece;
    private List<Piece> forcedPieces;

    private Vector2 cursor;
    private Vector2 startDestination;
    private Vector2 endDestination;

    public bool isWhite;
    private bool isWhitesTurn;
    private bool hasKilledPiece;

    private Client client;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        client = FindObjectOfType<Client>();
        isWhite = client.isHost;

        isWhitesTurn = true;
        forcedPieces = new List<Piece>();
        MakeBoard();
    }

    private void Update()
    {
        UpdateCursor();

        //Player Turn Update
        if((isWhite)?isWhitesTurn:!isWhitesTurn)
        {
            int x = (int)cursor.x;
            int y = (int)cursor.y;

            if (selectedPiece != null)
                UpdatePieceMovement(selectedPiece);

            if (Input.GetMouseButtonDown(0))
            {
                SelectPiece(x,y);
            }
            if (Input.GetMouseButtonUp(0))
            {
                TryMove((int)startDestination.x, (int)startDestination.y, x, y);
            }
        }

    }

    private void UpdateCursor()
    {
        //White's Turn
        if (!Camera.main)
        {
            Debug.Log("Main Cam wasn't found");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
            out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            cursor.x = (int)hit.point.x - boardOffset.x;
            cursor.y = (int)hit.point.z - boardOffset.z;
        }
        else
        {
            cursor.x = -1;
            cursor.y = -1;
        }
    }

    private void UpdatePieceMovement(Piece p)
    {
        //White's Turn
        if (!Camera.main)
        {
            Debug.Log("Main Cam wasn't found");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
            out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        //if Player is out of bounds
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
        if (p != null && p.isWhite == isWhite)
        {
            if (forcedPieces.Count == 0)
            {
                selectedPiece = p;
                startDestination = cursor;
            }
            else
            {
                if (forcedPieces.Find(fp => fp == p) == null)
                    return;

                selectedPiece = p;
                startDestination = cursor;
            }


        }
    }

    public void TryMove(int x1, int y1, int x2, int y2)
    {
        forcedPieces = PossibleMove();

        //For Multiplayer
        startDestination = new Vector2(x1, y1);
        endDestination = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];


        //Check for Out of Bounds
        if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {
            if (selectedPiece != null)
                MovePiece(selectedPiece, x1, y1);

            startDestination = Vector2.zero;
            selectedPiece = null;
            return;
        }

        //Piece Selected?
        if (selectedPiece != null)
        {
            //if it has not moved
            if (endDestination == startDestination)
            {
                MovePiece(selectedPiece, x1, y1);
                startDestination = Vector2.zero;
                selectedPiece = null;
                return;
            }

            //Check if its valid move
            if (selectedPiece.ValidMove(pieces, x1, y1, x2, y2))
            {
                if (Mathf.Abs(x2 - x1) == 2)
                {
                    Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null)
                    {
                        pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                        DestroyImmediate(p.gameObject);
                        hasKilledPiece = true;
                    }
                }
                //Kill Piece?
                if (forcedPieces.Count != 0 && !hasKilledPiece)
                {
                    MovePiece(selectedPiece, x1, y1);
                    startDestination = Vector2.zero;
                    selectedPiece = null;
                    return;
                }

                pieces[x2, y2] = selectedPiece;
                pieces[x1, y1] = null;
                MovePiece(selectedPiece, x2, y2);

                EndTurn();
            }
            else
            {
                MovePiece(selectedPiece, x1, y1);
                startDestination = Vector2.zero;
                selectedPiece = null;
                return;
            }
        }
    }

    private void MakeBoard()
    {
        //For White player
        for (int y = 0; y < 3; y++)
        {
            bool whiteSqs = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                CreatePiece((whiteSqs)? x : x+1, y);
            }
        }

        //For Black player
        for (int y = 7; y > 4; y--)
        {
            bool whiteSqs = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                CreatePiece((whiteSqs) ? x : x + 1, y);
            }
        }
    }

    private void CreatePiece(int x, int y)
    {
        bool isPieceWhite = (y > 3) ? false : true;
        GameObject go = Instantiate((isPieceWhite)?whitePiecePrefab:blackPiecePrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }

    private void MovePiece(Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
    }

    private void EndTurn()
    {
        int x = (int)endDestination.x;
        int y = (int)endDestination.y;

        //Became a king?
        if (selectedPiece != null)
        {
            if (selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
            else if (!selectedPiece.isWhite && !selectedPiece.isKing && y == 0)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
        }

        string msg = "Movement|";
        msg += startDestination.x.ToString() + "|";
        msg += startDestination.y.ToString() + "|";
        msg += endDestination.x.ToString() + "|";
        msg += endDestination.y.ToString();

        client.Send(msg);

        selectedPiece = null;
        startDestination = Vector2.zero;

        if (PossibleSecondMove(selectedPiece, x, y).Count != 0 && hasKilledPiece)
            return;

        isWhitesTurn = !isWhitesTurn;
        hasKilledPiece = false;
        CheckWin();
    }

    private void CheckWin()
    {
        var ps = FindObjectsOfType<Piece>();
        bool hasWhite = false, hasBlack = false;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].isWhite)
                hasWhite = true;
            else
                hasBlack = true;
        }
        if (!hasWhite)
            Win(false);
        if (!hasBlack)
            Win(true);

    }
    private void Win(bool isWhite)
    {
        if (isWhite)
            Debug.Log("Player 1 Wins");
        else
            Debug.Log("Player 2 Wins");
    }

    private List<Piece> PossibleMove()
    {
        forcedPieces = new List<Piece>();

        //Check all Pieces
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (pieces[i, j] != null && pieces[i, j].isWhite == isWhitesTurn)
                    if (pieces[i, j].ForcedMove(pieces, i, j))
                        forcedPieces.Add(pieces[i, j]);

        return forcedPieces;
    }
    private List<Piece> PossibleSecondMove(Piece p, int x, int y)
    {
        forcedPieces = new List<Piece>();

        if (pieces[x, y].ForcedMove(pieces,x,y))
            forcedPieces.Add(pieces[x, y]);


        return forcedPieces;
    }
}
