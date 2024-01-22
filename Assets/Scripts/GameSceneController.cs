using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    public int BoardWidth = 3;
    public int BoardHeight = 6;
    public float PuzzlePieceSpacing = 1.5f;

    public Camera GameCamera;
    public Transform Level;

    public GameObject PuzzlePiecePrefab;
    private PuzzlePieceController[,] board;
    private PuzzlePieceController selectedPuzzlePiece;

    private int score;
    private bool gameOver;

    public Text ScoreText;

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
        if (gameOver)
        {
            // Check for touch input or mouse click
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                // Reload the current scene to restart the game
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }
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
                ScoreText.text = "Score: " + score;
                iTween.ScaleTo(puzzlePiece.gameObject, iTween.Hash(
                    "scale", Vector3.zero,
                    "time", 0.3f
                ));
            }

            yield return new WaitForSeconds(0.3f);

            DropPuzzlePieces();
            AddPuzzlePieces();

            yield return new WaitForSeconds(1.0f);

            CheckGameOver();
        }
    }

    private void DropPuzzlePieces()
    {
        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                if (board[x, y].Destroyed)
                {
                    bool dropped = false;

                    for (int j = y + 1; j < BoardHeight && dropped == false; j++) //Make the piece above the destroyed piece fall
                    {
                        if (board[x, j].Destroyed == false)
                        {
                            Vector2 boardLocation1 = board[x, y].BoardLocation;
                            Vector2 boardLocation2 = board[x, j].BoardLocation;
                            board[x, y].BoardLocation = boardLocation2;
                            board[x, j].BoardLocation = boardLocation1;

                            iTween.MoveTo(board[x, j].gameObject, iTween.Hash(
                                "position", board[x, y].transform.position,
                                "time", 0.3f
                            ));

                            board[x, y].transform.position = board[x, j].transform.position;

                            PuzzlePieceController fallingPiece = board[x, j];

                            board[x, j] = board[x, y];
                            board[x, y] = fallingPiece;

                            dropped = true;
                        }
                    }
                }
            }
        }
    }

    private void AddPuzzlePieces()
    {
        int firstY = -1;
        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                if (board[x, y].Destroyed)
                {
                    if (firstY == -1)
                    {
                        firstY = y; //Replace -y with the y coordinate of the first destroyed piece found
                    }

                    PuzzlePieceController oldPuzzlePiece = board[x, y];
                    GameObject puzzlePieceInstance = Instantiate(PuzzlePiecePrefab);
                    puzzlePieceInstance.transform.SetParent(Level);
                    puzzlePieceInstance.transform.position = new Vector3(
                        oldPuzzlePiece.transform.position.x,
                        10,
                        0
                    );

                    iTween.MoveTo(puzzlePieceInstance.gameObject, iTween.Hash(
                        "position", oldPuzzlePiece.transform.position,
                        "time", 0.3f,
                        "delay", 0.1f * (y - firstY) //Allow peices to fall one after another
                    ));

                    PuzzlePieceController puzzlePiece = puzzlePieceInstance.GetComponent<PuzzlePieceController>();

                    puzzlePiece.BoardLocation = oldPuzzlePiece.BoardLocation;
                    board[x, y] = puzzlePiece;
                    Destroy(oldPuzzlePiece.gameObject);
                }
            }
        }
    }


    private void CheckGameOver()
    {
        int possibleMatches = 0;

        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                PuzzlePieceController puzzlePiece1 = board[x, y];
                Vector2 boardLocation1 = puzzlePiece1.BoardLocation;
                PuzzlePieceController puzzlePiece2;
                Vector2 boardLocation2;

                //Horizontal Swap
                if (x < BoardWidth - 1)
                {
                    puzzlePiece2 = board[x + 1, y];
                    boardLocation2 = puzzlePiece2.BoardLocation;
                    puzzlePiece1.BoardLocation = boardLocation2;
                    puzzlePiece2.BoardLocation = boardLocation1;

                    board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
                    board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;

                    if (CheckMatch(puzzlePiece1).Count >= 3 || CheckMatch(puzzlePiece2).Count >= 3)
                    {
                        possibleMatches++;
                    }

                    //Put things back
                    puzzlePiece1.BoardLocation = boardLocation1;
                    puzzlePiece2.BoardLocation = boardLocation2;

                    board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
                    board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;
                }

                if (y < BoardHeight - 1)
                {
                    puzzlePiece2 = board[x, y + 1];
                    boardLocation2 = puzzlePiece2.BoardLocation;
                    puzzlePiece1.BoardLocation = boardLocation2;
                    puzzlePiece2.BoardLocation = boardLocation1;

                    board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
                    board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;

                    if (CheckMatch(puzzlePiece1).Count >= 3 || CheckMatch(puzzlePiece2).Count >= 3)
                    {
                        possibleMatches++;
                    }

                    //Put things back
                    puzzlePiece1.BoardLocation = boardLocation1;
                    puzzlePiece2.BoardLocation = boardLocation2;

                    board[(int)puzzlePiece1.BoardLocation.x, (int)puzzlePiece1.BoardLocation.y] = puzzlePiece1;
                    board[(int)puzzlePiece2.BoardLocation.x, (int)puzzlePiece2.BoardLocation.y] = puzzlePiece2;
                }

            }
        }
        if (possibleMatches == 0)
        {
            OnGameOver();
        }
    }

    private void OnGameOver()
    {
        Debug.Log("Game Over!");
        if (gameOver == false)
        {
            gameOver = true;
            ScoreText.text = "Score: " + score + " \nNo More Moves!\nTap to restart";

        }
    }

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