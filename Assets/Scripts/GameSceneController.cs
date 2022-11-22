using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneController : MonoBehaviour
{
    public int BoardWidth = 6;
    public int BoardHeight = 6;
    public float PuzzlePieceSpacing = 1.5f;

    public Camera GameCamera;
    public Transform Level;

    public GameObject PuzzlePiecePrefab;
    private PuzzlePieceController[,] board;
    private PuzzlePieceController selectedPuzzlePiece;

    private int score;
    private bool gameOver;

    // Start is called before the first frame update
    void Start()
    {
        BuildBoard();
    }

    private void BuildBoard()
    {
        board = new PuzzlePieceController[BoardWidth, BoardHeight];

        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                GameObject puzzlePieceInstance = Instantiate(PuzzlePiecePrefab);
                puzzlePieceInstance.transform.SetParent(Level);
                puzzlePieceInstance.transform.localPosition = new Vector3(
                        (-BoardWidth * PuzzlePieceSpacing) / 2f + (PuzzlePieceSpacing / 2f) + x * PuzzlePieceSpacing,
                        (-BoardHeight * PuzzlePieceSpacing) / 2f + (PuzzlePieceSpacing / 2f) + y * PuzzlePieceSpacing,
                        0f
                    );

                PuzzlePieceController puzzlePiece = puzzlePieceInstance.GetComponent<PuzzlePieceController>();
                puzzlePiece.BoardLocation = new Vector2(x, y);
                board[x, y] = puzzlePiece;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();
    }

    private void ProcessInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = GameCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

            if (hitCollider != null && hitCollider.gameObject.GetComponent<PuzzlePieceController>() != null)
            {
                PuzzlePieceController hitPuzzlePiece = hitCollider.gameObject.GetComponent<PuzzlePieceController>();

                if (selectedPuzzlePiece == null)
                {
                    selectedPuzzlePiece = hitPuzzlePiece;

                    iTween.ScaleTo(selectedPuzzlePiece.gameObject, iTween.Hash(
                        "scale", Vector3.one * 0.8f,
                        "time", 0.2f
                    ));
                }
                else
                {
                    if (hitPuzzlePiece == selectedPuzzlePiece)
                    {
                        iTween.ScaleTo(selectedPuzzlePiece.gameObject, iTween.Hash(
                            "scale", Vector3.one,
                            "time", 0.2f
                        ));
                        selectedPuzzlePiece = null;
                    }
                    else if (hitPuzzlePiece.IsNeighbour(selectedPuzzlePiece))
                    {
                        StartCoroutine(AttemptMatchRoutine(selectedPuzzlePiece, hitPuzzlePiece));
                        selectedPuzzlePiece = null;
                    }
                }
            }
        }
    }

    private IEnumerator AttemptMatchRoutine(PuzzlePieceController puzzlePiece1, PuzzlePieceController puzzlePiece2)
    {
        iTween.Stop(puzzlePiece1.gameObject);
        iTween.Stop(puzzlePiece2.gameObject);

        puzzlePiece1.transform.localScale = Vector3.one;
        puzzlePiece2.transform.localScale = Vector3.one;

        Vector2 boardLocation1 = puzzlePiece1.BoardLocation;
        Vector2 boardLocation2 = puzzlePiece2.BoardLocation;

        Vector3 position1 = puzzlePiece1.transform.position;
        Vector3 position2 = puzzlePiece2.transform.position;

        iTween.MoveTo(puzzlePiece1.gameObject, iTween.Hash(
            "position", position2,
            "time", 0.5f
        ));

        iTween.MoveTo(puzzlePiece2.gameObject, iTween.Hash(
            "position", position1,
            "time", 0.5f
        ));

        puzzlePiece1.BoardLocation = boardLocation2;
        puzzlePiece2.BoardLocation = boardLocation1;

        board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
        board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;

        yield return new WaitForSeconds(0.5f);


        List<PuzzlePieceController> matchingPieces = CheckMatch(puzzlePiece1);

        if (matchingPieces.Count == 0)
        {
            matchingPieces = CheckMatch(puzzlePiece2);
        }

        if (matchingPieces.Count < 3) //If not a match then move pieces back
        {
            iTween.MoveTo(puzzlePiece1.gameObject, iTween.Hash(
               "position", position1,
               "time", 0.5f
           ));

            iTween.MoveTo(puzzlePiece2.gameObject, iTween.Hash(
                "position", position2,
                "time", 0.5f
            ));

            puzzlePiece1.BoardLocation = boardLocation1;
            puzzlePiece2.BoardLocation = boardLocation2;

            board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
            board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;

            yield return new WaitForSeconds(0.5f);

            CheckGameOver();

        }
        else
        {
            foreach (PuzzlePieceController puzzlePiece in matchingPieces)
            {
                puzzlePiece.Destroyed = true;
                score += 100;
                iTween.ScaleTo(puzzlePiece.gameObject.gameObject, iTween.Hash(
                    "scale", Vector3.zero,
                    "time", 0.3f
                ));
            }

            yield return new WaitForSeconds(0.3f);

            //DropPuzzlePieces();
            //AddPuzzlePieces();

            yield return new WaitForSeconds(1.0f);

            CheckGameOver();
        }
    }


    private void CheckGameOver()
    { }

    private List<PuzzlePieceController> CheckMatch(PuzzlePieceController puzzlePiece)
    {
        List<PuzzlePieceController> matchingNeighbours = new List<PuzzlePieceController>();

        //Horizontal Matching Logic
        int x = 0;
        int y = (int)puzzlePiece.BoardLocation.y;
        bool reachedPuzzlePiece = false;

        while (x < BoardWidth)
        {
            if (board[x, y].Destroyed == false && board[x, y].ID == puzzlePiece.ID) //Is it a valid pieces with the same colour as the one we swapped?
            {
                matchingNeighbours.Add(board[x, y]);
                if (board[x, y] == puzzlePiece) //Have we reached the piece we swapped?
                {
                    reachedPuzzlePiece = true;
                }
            }
            else
            {
                if (reachedPuzzlePiece == false)
                {
                    //Didn't reach the selected piece
                    matchingNeighbours.Clear();
                }
                else if (matchingNeighbours.Count >= 3)
                {
                    //Reached a match of 3 or more piences
                    return matchingNeighbours;
                }
                else //Didn't match at least 3 pieces
                {
                    matchingNeighbours.Clear();
                }
            }
            x++;
        }

        if (matchingNeighbours.Count >= 3)
        {
            return matchingNeighbours;
        }

        //Vertical Matching Logic
        x = (int)puzzlePiece.BoardLocation.x;
        y = 0;
        reachedPuzzlePiece = false;
        matchingNeighbours.Clear();

        while (y < BoardHeight)
        {
            if (board[x, y].Destroyed == false && board[x, y].ID == puzzlePiece.ID)
            {
                matchingNeighbours.Add(board[x, y]);
                if (board[x, y] == puzzlePiece)
                {
                    reachedPuzzlePiece = true;
                }
            }
            else
            {
                if (reachedPuzzlePiece == false)
                {
                    matchingNeighbours.Clear();
                }
                else if (matchingNeighbours.Count >= 3)
                {
                    return matchingNeighbours;
                }
                else
                {
                    matchingNeighbours.Clear();
                }
            }
            y++;
        }

        return matchingNeighbours;
    }

}