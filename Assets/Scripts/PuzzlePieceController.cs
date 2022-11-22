using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePieceController : MonoBehaviour
{
    private Color[]
        colours =
            new Color[6]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.cyan,
                Color.magenta,
                Color.yellow
            };

    //public SpriteRenderer Sprite;
    public int ID;

    public Vector2 BoardLocation;

    public bool Destroyed;

    // Start is called before the first frame update
    void Start()
    {
        ID = Random.Range(0, colours.Length);
        this.GetComponent<SpriteRenderer>().color = colours[ID];
    }

    public bool IsNeighbour(PuzzlePieceController otherPiece)
    {
        return Mathf.Abs(otherPiece.BoardLocation.x - this.BoardLocation.x) +
        Mathf.Abs(otherPiece.BoardLocation.y - this.BoardLocation.y) ==
        1;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
