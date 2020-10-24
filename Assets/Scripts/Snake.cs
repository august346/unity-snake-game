using System.Collections.Generic;
using UnityEngine;


public class Snake : MonoBehaviour
{
    private bool pause = false;
    private bool lose = false;
    private bool win = false;

    public int planeScale = 0;
    private int score = 0;
    public int speed = 1;

    private float nextTime = 0;

    private readonly int speedOffset = 2;

    private Head head;
    private Tail tail;
    private Apple apple;

    private IMovable[] movables;

    private GameObject headGO;
    private GameObject tailGO;
    private GameObject appleGO;
    private GameObject restartGO;
    private GameObject winGO;
    private GameObject loseGO;

    void Start()
    {
        headGO = GameObject.Find("Head");
        tailGO = GameObject.Find("Tail");
        appleGO = GameObject.Find("Apple");
        restartGO = GameObject.Find("Restart");
        winGO = GameObject.Find("Win");
        loseGO = GameObject.Find("Lose");

        TogleState();

        head = new Head(headGO);
        tail = new Tail(tailGO, head);
        apple = new Apple(appleGO, head, tail, planeScale);

        movables = new IMovable[]
        {
            head,
            tail
        };

        UpdateNextTime(true);
    }

    public void Update()
    {
        if (pause)
        {
            return;
        }

        if (lose || win)
        {
            TogleState();
        }
        else
        {
            head.CheckKeyDown();

            if (Time.time > nextTime)
            {
                try
                {
                    Move();
                    UpdateNextTime(false);
                }
                catch (System.IndexOutOfRangeException)
                {
                    win = true;
                }
            }
        }
    }

    private void TogleState()
    {
        if (win || lose)
        {
           restartGO.SetActive(true);
            (win ? winGO : loseGO).SetActive(true);
            pause = true;
        }
        else
        {
            restartGO.SetActive(false);
            winGO.SetActive(false);
            loseGO.SetActive(false);
        }
    }

    private void UpdateNextTime(bool isFirstTime)
    {
        float start = isFirstTime ? Time.time : nextTime;
        nextTime = start + 1 / Mathf.Log(speed + speedOffset);
    }

    private void Move()
    {
        foreach (IMovable m in movables)
        {
            m.Move();
        }

        if (appleGO.transform.position.Equals(headGO.transform.position))
        {
            ScoreUpd();
            Eat();
        }

        CheckLose();
    }

    private void ScoreUpd()
    {
        score += 1;
        GameObject.Find("Score").GetComponent<UnityEngine.UI.Text>().text = "Score: " + score;
    }

    private void Eat()
    {
        tail.AddCell();
        apple.ChangePosition();
        if (score % 10 == 0)
        {
            speed += 1;
        }
    }

    private void CheckLose()
    {
        var tailBussied = tail.GetBussiedVectors();
        if (tailBussied.Contains(headGO.transform.position))
        {
            lose = true;
        }
    }

}

interface IMovable
{
    void Move();
}


public class Head: IMovable
{

    public Vector3 previousPosition;
    private KeyCode prevKeyCode = KeyCode.W;
    private KeyCode newKeyCode = KeyCode.W;

    public readonly GameObject gameObject;

    private readonly Dictionary<KeyCode, Vector3> shifts = new Dictionary<KeyCode, Vector3>
    {
        { KeyCode.W, new Vector3(0, 0, 1f) },
        { KeyCode.D, new Vector3(1f, 0, 0) },
        { KeyCode.S, new Vector3(0, 0, -1f) },
        { KeyCode.A, new Vector3(-1f, 0, 0) }
    };

    public Head(GameObject headGO)
    {
        gameObject = headGO;
    }

    public void Move()
    {
        previousPosition = gameObject.transform.position;
        gameObject.transform.position += shifts[newKeyCode];
        prevKeyCode = newKeyCode;
    }

    public void CheckKeyDown()
    {
        foreach (KeyCode key in shifts.Keys)
        {
            if (Input.GetKeyDown(key))
            {
                if (
                    (key == KeyCode.W && prevKeyCode == KeyCode.S)
                    || (key == KeyCode.S && prevKeyCode == KeyCode.W)
                    || (key == KeyCode.A && prevKeyCode == KeyCode.D)
                    || (key == KeyCode.D && prevKeyCode == KeyCode.A)
                )
                {
                    continue;
                }
                else
                {
                    newKeyCode = key;
                    break;
                }
            }
        }
    }

}

public class Tail: IMovable
{

    readonly Head head;
    public readonly GameObject gameObject;

    private Vector3 previousLastPosition;

    public Tail(GameObject tailGO, Head head)
    {
        this.head = head;
        gameObject = tailGO;
    }

    public void Move()
    {
        previousLastPosition = head.previousPosition;

        foreach (Transform child in gameObject.transform)
        {
            Vector3 tmp = child.transform.position;
            child.transform.position = previousLastPosition;
            previousLastPosition = tmp;
        }
    }

    public void AddCell()
    {
        GameObject newCell = GameObject.Instantiate(GameObject.Find("Cell_0"));
        newCell.name = "Cell_" + (gameObject.transform.childCount);
        newCell.transform.position = previousLastPosition;
        newCell.transform.parent = gameObject.transform;
    }

    public List<Vector3> GetBussiedVectors()
    {
        var result = new List<Vector3>();

        foreach (Transform child in gameObject.transform)
        {
            result.Add(child.position);
        }

        return result;
    }
}

public class Apple
{
    readonly int planeScale;
    public readonly GameObject gameObject;
    private readonly Head head;
    private readonly Tail tail;

    public bool needMove;

    public Apple(GameObject appleGO, Head head, Tail tail, int planeScale)
    {
        this.planeScale = planeScale;
        gameObject = appleGO;
        this.head = head;
        this.tail = tail;

        ChangePosition();
    }

    public void ChangePosition()
    {
        Vector3[] availablePositions = GetAvailablePositions();
        gameObject.transform.position = availablePositions[Random.Range(0, availablePositions.Length)];
    }

    private Vector3[] GetAvailablePositions()
    {
        var result = new List<Vector3>();
        var notEmpty = GetBussiedVectors();

        for (int x = 0; x < planeScale; x++)
        {
            for (int y = 0; y < planeScale; y++)
            {
                int diff = planeScale / 2 - 1;
                Vector3 v = new Vector3(x - diff, 0.5f, y - diff);
                if (!notEmpty.Contains(v))
                {
                    result.Add(v);
                }
            }
        }

        return result.ToArray();
    }

    private List<Vector3> GetBussiedVectors()
    {
        var result = new List<Vector3>
        {
            head.gameObject.transform.position
        };

        foreach (Vector3 v in tail.GetBussiedVectors())
        {
            result.Add(v);
        }

        return result;
    }

}
