using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;


public class ShapesManager : MonoBehaviour
{
    public Text ScoreText;

    public ShapesArray shapes;

    private int score;

    public Vector2 DefaultBottomRight = new Vector2(-2.37f, -4.27f);
    public Vector2 DefaultCandySize = new Vector2(0.8f, 0.8f);
    public float HorizontalScreenPadding = 0.3f;
    public float BoardVerticalOffset = 0f;
    public bool ScaleUpSmallBoardsToFillWidth = false;
    public bool UseBoardLayoutFrame = true;
    public Vector2 BoardLayoutFrameCenter = new Vector2(0f, 0f);
    public Vector2 BoardLayoutFrameSize = new Vector2(6f, 8f);
    public int RandomLevelRows = 7;
    public int RandomLevelColumns = 7;
    [Range(0f, 1f)]
    public float X2ItemChance = 0.1f;
    [Range(0f, 1f)]
    public float X3ItemChance = 0.05f;

    private Vector2 BoardBottomLeft;
    private Vector2 CandySize;

    private GameState state = GameState.None;
    private GameObject hitGo = null;
    private Vector2[] SpawnPositions;
    public GameObject[] CandyPrefabs;
    public GameObject[] ExplosionPrefabs;
    //public GameObject[] BonusPrefabs;

    private IEnumerator CheckPotentialMatchesCoroutine;
    private IEnumerator AnimatePotentialMatchesCoroutine;

    IEnumerable<GameObject> potentialMatches;

    public SoundManager soundManager;
    void Start()
    {
        InitializeTypesOnPrefabShapesAndBonuses();

        Constants.ConfigureBoardSize(RandomLevelRows, RandomLevelColumns);
        InitializeCandyAndSpawnPositions();

        StartCheckForPotentialMatches();
    }

    private void InitializeTypesOnPrefabShapesAndBonuses()
    {
        //just assign the name of the prefab
        foreach (var item in CandyPrefabs)
        {
            item.GetComponent<Shape>().Type = item.name;

        }
    }

    public void InitializeCandyAndSpawnPositionsFromPremadeLevel()
    {
        InitializeVariables();

        var premadeLevel = DebugUtilities.FillShapesArrayFromResourcesData();

        if (shapes != null)
            DestroyAllCandy();

        Constants.ConfigureBoardSize(premadeLevel.GetLength(0), premadeLevel.GetLength(1));
        shapes = new ShapesArray();
        SpawnPositions = new Vector2[Constants.Columns];
        UpdateBoardLayout();

        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int column = 0; column < Constants.Columns; column++)
            {

                GameObject newCandy = null;

                newCandy = GetSpecificCandyOrBonusForPremadeLevel(premadeLevel[row, column]);

                var placedCandy = InstantiateAndPlaceNewCandy(row, column, newCandy, GetItemCountForPremadeLevel(premadeLevel[row, column]));
                ApplyPremadeLevelInfo(placedCandy, premadeLevel[row, column]);

            }
        }

        SetupSpawnPositions();
    }


    public void InitializeCandyAndSpawnPositions()
    {
        InitializeVariables();

        if (shapes != null)
            DestroyAllCandy();

        Constants.ConfigureBoardSize(RandomLevelRows, RandomLevelColumns);
        shapes = new ShapesArray();
        SpawnPositions = new Vector2[Constants.Columns];
        UpdateBoardLayout();

        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int column = 0; column < Constants.Columns; column++)
            {

                GameObject newCandy = GetRandomCandy();

                //check if two previous horizontal are of the same type
                while (column >= 2 && shapes[row, column - 1].GetComponent<Shape>()
                    .IsSameType(newCandy.GetComponent<Shape>())
                    && shapes[row, column - 2].GetComponent<Shape>().IsSameType(newCandy.GetComponent<Shape>()))
                {
                    newCandy = GetRandomCandy();
                }

                //check if two previous vertical are of the same type
                while (row >= 2 && shapes[row - 1, column].GetComponent<Shape>()
                    .IsSameType(newCandy.GetComponent<Shape>())
                    && shapes[row - 2, column].GetComponent<Shape>().IsSameType(newCandy.GetComponent<Shape>()))
                {
                    newCandy = GetRandomCandy();
                }
                InstantiateAndPlaceNewCandy(row, column, newCandy, GetRandomItemCount());
            }
        }

        SetupSpawnPositions();
    }



    private GameObject InstantiateAndPlaceNewCandy(int row, int column, GameObject newCandy, int itemCount)
    {
        GameObject go = Instantiate(newCandy,
            GetWorldPosition(row, column), Quaternion.identity)
            as GameObject;

        //assign the specific properties
        go.GetComponent<Shape>().Assign(newCandy.GetComponent<Shape>().Type, row, column, itemCount);
        go.transform.localScale = Vector3.one * GetCandyScale();
        go.transform.SetParent(transform);
        shapes[row, column] = go;
        return go;
    }

    private void UpdateBoardLayout()
    {
        CandySize = DefaultCandySize;

        if (UseBoardLayoutFrame)
        {
            Rect frame = GetBoardLayoutFrame();
            float frameAvailableWidth = Mathf.Max(0.1f, frame.width);
            float frameAvailableHeight = Mathf.Max(0.1f, frame.height);
            float frameBoardWidth = DefaultCandySize.x * Constants.Columns;
            float frameBoardHeight = DefaultCandySize.y * Constants.Rows;
            float frameBoardScale = Mathf.Min(frameAvailableWidth / frameBoardWidth, frameAvailableHeight / frameBoardHeight);

            if (frameBoardScale < 1f || ScaleUpSmallBoardsToFillWidth)
                CandySize = DefaultCandySize * frameBoardScale;

            float frameBoardLeftX = frame.center.x - ((Constants.Columns - 1) * CandySize.x) / 2f;
            float frameBoardBottomY = frame.center.y - ((Constants.Rows - 1) * CandySize.y) / 2f;
            BoardBottomLeft = new Vector2(frameBoardLeftX, frameBoardBottomY);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
        {
            BoardBottomLeft = DefaultBottomRight;
            return;
        }

        float cameraHeight = camera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * camera.aspect;
        float availableWidth = Mathf.Max(0.1f, cameraWidth - HorizontalScreenPadding * 2f);
        float boardWidth = DefaultCandySize.x * Constants.Columns;
        float boardScale = availableWidth / boardWidth;

        if (boardWidth > availableWidth || ScaleUpSmallBoardsToFillWidth)
            CandySize = DefaultCandySize * boardScale;

        float leftEdge = camera.transform.position.x - cameraWidth / 2f + HorizontalScreenPadding;
        float minBoardLeftX = leftEdge + CandySize.x / 2f;
        float boardCenterX = camera.transform.position.x;
        float boardLeftX = boardCenterX - ((Constants.Columns - 1) * CandySize.x) / 2f;
        float boardBottomY = DefaultBottomRight.y + BoardVerticalOffset;

        BoardBottomLeft = new Vector2(Mathf.Max(minBoardLeftX, boardLeftX), boardBottomY);
    }

    public Rect GetBoardLayoutFrame()
    {
        Vector2 size = new Vector2(Mathf.Max(0.1f, BoardLayoutFrameSize.x), Mathf.Max(0.1f, BoardLayoutFrameSize.y));
        return new Rect(BoardLayoutFrameCenter - size / 2f, size);
    }

    private float GetCandyScale()
    {
        return CandySize.x / DefaultCandySize.x;
    }

    private Vector2 GetWorldPosition(int row, int column)
    {
        return BoardBottomLeft + new Vector2(column * CandySize.x, row * CandySize.y);
    }

    private void SetupSpawnPositions()
    {
        //create the spawn positions for the new shapes (will pop from the 'ceiling')
        for (int column = 0; column < Constants.Columns; column++)
        {
            SpawnPositions[column] = BoardBottomLeft
                + new Vector2(column * CandySize.x, Constants.Rows * CandySize.y);
        }
    }

    private void DestroyAllCandy()
    {
        for (int row = 0; row < shapes.Rows; row++)
        {
            for (int column = 0; column < shapes.Columns; column++)
            {
                Destroy(shapes[row, column]);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (state == GameState.None)
        {
            //user has clicked or touched
            if (Input.GetMouseButtonDown(0))
            {
                //get the hit position
                var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null) //we have a hit!!!
                {
                    hitGo = hit.collider.gameObject;
                    state = GameState.SelectionStarted;
                }
                
            }
        }
        else if (state == GameState.SelectionStarted)
        {
            //user dragged
            if (Input.GetMouseButton(0))
            {
                

                var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                //we have a hit
                if (hit.collider != null && hitGo != hit.collider.gameObject)
                {

                    //user did a hit, no need to show him hints 
                    StopCheckForPotentialMatches();

                    //if the two shapes are diagonally aligned (different row and column), just return
                    if (!Utilities.AreVerticalOrHorizontalNeighbors(hitGo.GetComponent<Shape>(),
                        hit.collider.gameObject.GetComponent<Shape>()))
                    {
                        state = GameState.None;
                    }
                    else
                    {
                        state = GameState.Animating;
                        FixSortingLayer(hitGo, hit.collider.gameObject);
                        StartCoroutine(FindMatchesAndCollapse(hit));
                    }
                }
            }
        }
    }
    private void FixSortingLayer(GameObject hitGo, GameObject hitGo2)
    {
        SpriteRenderer sp1 = hitGo.GetComponent<SpriteRenderer>();
        SpriteRenderer sp2 = hitGo2.GetComponent<SpriteRenderer>();
        if (sp1.sortingOrder <= sp2.sortingOrder)
        {
            sp1.sortingOrder = 1;
            sp2.sortingOrder = 0;
            hitGo.GetComponent<Shape>().RefreshCountTextSorting();
            hitGo2.GetComponent<Shape>().RefreshCountTextSorting();
        }
    }




    private IEnumerator FindMatchesAndCollapse(RaycastHit2D hit2)
    {
        //get the second item that was part of the swipe
        var hitGo2 = hit2.collider.gameObject;
        Vector3 hitGoPosition = hitGo.transform.position;
        Vector3 hitGo2Position = hitGo2.transform.position;
        shapes.Swap(hitGo, hitGo2);

        //move the swapped ones
        yield return AnimateCandySwap(hitGo, hitGo2, hitGo2Position, hitGoPosition, Constants.AnimationDuration);

        //get the matches via the helper methods
        var hitGomatchesInfo = shapes.GetMatches(hitGo);
        var hitGo2matchesInfo = shapes.GetMatches(hitGo2);

        var totalMatches = hitGomatchesInfo.MatchedCandy
            .Union(hitGo2matchesInfo.MatchedCandy).Distinct();

        //if user's swap didn't create at least a 3-match, undo their swap
        if (totalMatches.Count() < Constants.MinimumMatches)
        {
            yield return AnimateCandySwap(hitGo, hitGo2, hitGoPosition, hitGo2Position, Constants.AnimationDuration);

            shapes.UndoSwap();
        }

        int timesRun = 1;
        List<ShapeMatchData> matchedItemQueue = new List<ShapeMatchData>();
        while (totalMatches.Count() >= Constants.MinimumMatches)
        {
            List<GameObject> matchedItems = totalMatches.ToList();
            AddMatchedItems(matchedItems, matchedItemQueue);

            //increase score
            IncreaseScore((matchedItems.Count - 2) * Constants.Match3Score);

            if (timesRun >= 2)
                IncreaseScore(Constants.SubsequentMatchScore);

            if (soundManager != null)
                soundManager.PlayCrincle();

            foreach (var item in matchedItems)
            {
                shapes.Remove(item);
                RemoveFromScene(item);
            }

            //get the columns that we had a collapse
            var columns = matchedItems.Select(go => go.GetComponent<Shape>().Column).Distinct();

            //the order the 2 methods below get called is important!!!
            //collapse the ones gone
            var collapsedCandyInfo = shapes.Collapse(columns);
            //create new ones
            var newCandyInfo = CreateNewCandyInSpecificColumns(columns);

            int maxDistance = Mathf.Max(collapsedCandyInfo.MaxDistance, newCandyInfo.MaxDistance);

            MoveAndAnimate(newCandyInfo.AlteredCandy, maxDistance);
            MoveAndAnimate(collapsedCandyInfo.AlteredCandy, maxDistance);



            //will wait for both of the above animations
            yield return new WaitForSeconds(Constants.MoveAnimationMinDuration * maxDistance);

            //search if there are matches with the new/collapsed items
            totalMatches = shapes.GetMatches(collapsedCandyInfo.AlteredCandy).
                Union(shapes.GetMatches(newCandyInfo.AlteredCandy)).Distinct();



            timesRun++;
        }

        if (matchedItemQueue.Count > 0)
        {
            SetAllShapesVisible(false);
            if (GameController.Instance != null)
                yield return GameController.Instance.PlayMatchedItems(matchedItemQueue);
            SetAllShapesVisible(true);
        }

        state = GameState.None;
        if (timesRun > 1 && GameController.Instance != null)
            GameController.Instance.EndCurrentTurn();

        StartCheckForPotentialMatches();
    }

    private void AddMatchedItems(IEnumerable<GameObject> matchedItems, List<ShapeMatchData> matchedItemQueue)
    {
        foreach (GameObject item in matchedItems)
        {
            Shape shape = item.GetComponent<Shape>();
            SpriteRenderer spriteRenderer = item.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            Color color = spriteRenderer != null ? spriteRenderer.color : Color.white;
            matchedItemQueue.Add(new ShapeMatchData(shape.GetResolvedItemEffect(), shape.ItemCount, sprite, color));
        }
    }

    private void SetAllShapesVisible(bool visible)
    {
        if (shapes == null)
            return;

        for (int row = 0; row < shapes.Rows; row++)
        {
            for (int column = 0; column < shapes.Columns; column++)
            {
                GameObject shape = shapes[row, column];
                if (shape != null)
                    shape.SetActive(visible);
            }
        }
    }

   
    private AlteredCandyInfo CreateNewCandyInSpecificColumns(IEnumerable<int> columnsWithMissingCandy)
    {
        AlteredCandyInfo newCandyInfo = new AlteredCandyInfo();

        //find how many null values the column has
        foreach (int column in columnsWithMissingCandy)
        {
            var emptyItems = shapes.GetEmptyItemsOnColumn(column);
            foreach (var item in emptyItems)
            {
                var go = GetRandomCandy();
                GameObject newCandy = Instantiate(go, SpawnPositions[column], Quaternion.identity)
                    as GameObject;

                newCandy.transform.SetParent(transform);
                newCandy.GetComponent<Shape>().Assign(go.GetComponent<Shape>().Type, item.Row, item.Column, GetRandomItemCount());
                newCandy.transform.localScale = Vector3.one * GetCandyScale();

                if (Constants.Rows - item.Row > newCandyInfo.MaxDistance)
                    newCandyInfo.MaxDistance = Constants.Rows - item.Row;

                shapes[item.Row, item.Column] = newCandy;
                newCandyInfo.AddCandy(newCandy);
            }
        }
        return newCandyInfo;
    }

    private void MoveAndAnimate(IEnumerable<GameObject> movedGameObjects, int distance)
    {
        float duration = Constants.MoveAnimationMinDuration * distance;
        foreach (var item in movedGameObjects)
        {
            item.transform.DOKill();
            item.transform.DOMove(GetWorldPosition(item.GetComponent<Shape>().Row, item.GetComponent<Shape>().Column), duration);
        }
    }

    private IEnumerator AnimateCandySwap(GameObject firstCandy, GameObject secondCandy,
        Vector3 firstDestination, Vector3 secondDestination, float duration)
    {
        firstCandy.transform.DOKill();
        secondCandy.transform.DOKill();

        Sequence swapSequence = DOTween.Sequence();
        swapSequence.Join(firstCandy.transform.DOMove(firstDestination, duration));
        swapSequence.Join(secondCandy.transform.DOMove(secondDestination, duration));
        yield return swapSequence.WaitForCompletion();
    }

    private void RemoveFromScene(GameObject item)
    {
        item.transform.DOKill();
        GameObject explosion = GetRandomExplosion();
        var newExplosion = Instantiate(explosion, item.transform.position, Quaternion.identity) as GameObject;
        newExplosion.transform.SetParent(transform);
        Destroy(newExplosion, Constants.ExplosionDuration);
        Destroy(item);
    }

    private GameObject GetRandomCandy()
    {
        return CandyPrefabs[Random.Range(0, CandyPrefabs.Length)];
    }

    private int GetRandomItemCount()
    {
        float randomValue = Random.value;

        if (randomValue < X3ItemChance)
            return 3;

        if (randomValue < X3ItemChance + X2ItemChance)
            return 2;

        return 1;
    }

    private void InitializeVariables()
    {
        score = 0;
        ShowScore();
    }

    private void IncreaseScore(int amount)
    {
        score += amount;
        ShowScore();
    }

    private void ShowScore()
    {
        ScoreText.text = "Score: " + score.ToString();
    }

    private GameObject GetRandomExplosion()
    {
        return ExplosionPrefabs[Random.Range(0, ExplosionPrefabs.Length)];
    }

    private void StartCheckForPotentialMatches()
    {
        StopCheckForPotentialMatches();
        //get a reference to stop it later
        CheckPotentialMatchesCoroutine = CheckPotentialMatches();
        StartCoroutine(CheckPotentialMatchesCoroutine);
    }

    /// <summary>
    /// Stops the coroutines
    /// </summary>
    private void StopCheckForPotentialMatches()
    {
        if (AnimatePotentialMatchesCoroutine != null)
            StopCoroutine(AnimatePotentialMatchesCoroutine);
        if (CheckPotentialMatchesCoroutine != null)
            StopCoroutine(CheckPotentialMatchesCoroutine);
        ResetOpacityOnPotentialMatches();
    }

    private void ResetOpacityOnPotentialMatches()
    {
        if (potentialMatches != null)
            foreach (var item in potentialMatches)
            {
                if (item == null) break;

                item.GetComponent<SpriteRenderer>().DOKill();
                Color c = item.GetComponent<SpriteRenderer>().color;
                c.a = 1.0f;
                item.GetComponent<SpriteRenderer>().color = c;
            }
    }
    private IEnumerator CheckPotentialMatches()
    {
        yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
        potentialMatches = Utilities.GetPotentialMatches(shapes);
        if (potentialMatches != null)
        {
            while (true)
            {

                AnimatePotentialMatchesCoroutine = Utilities.AnimatePotentialMatches(potentialMatches);
                StartCoroutine(AnimatePotentialMatchesCoroutine);
                yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
            }
        }
    }

    private GameObject GetSpecificCandyOrBonusForPremadeLevel(string info)
    {
        var tokens = info.Split('_');

        if (tokens.Count() >= 1)
        {
            foreach (var item in CandyPrefabs)
            {
                if (item.GetComponent<Shape>().Type.Contains(tokens[0].Trim()))
                    return item;
            }

        }
        throw new System.Exception("Wrong type '" + info + "', check your premade level");
    }

    private void ApplyPremadeLevelInfo(GameObject candy, string info)
    {
        var tokens = info.Split('_');

        if (tokens.Count() < 2)
            return;

        for (int i = 1; i < tokens.Count(); i++)
        {
            if (tokens[i].Trim() == "B")
                candy.GetComponent<Shape>().Bonus = BonusType.DestroyWholeRowColumn;
        }
    }

    private int GetItemCountForPremadeLevel(string info)
    {
        var tokens = info.Split('_');

        for (int i = 1; i < tokens.Count(); i++)
        {
            string token = tokens[i].Trim().ToLower();
            if (token == "2" || token == "x2")
                return 2;

            if (token == "3" || token == "x3")
                return 3;
        }

        return GetRandomItemCount();
    }

    private void OnDrawGizmosSelected()
    {
        if (!UseBoardLayoutFrame)
            return;

        Rect frame = GetBoardLayoutFrame();
        Vector3 bottomLeft = new Vector3(frame.xMin, frame.yMin, transform.position.z);
        Vector3 topLeft = new Vector3(frame.xMin, frame.yMax, transform.position.z);
        Vector3 topRight = new Vector3(frame.xMax, frame.yMax, transform.position.z);
        Vector3 bottomRight = new Vector3(frame.xMax, frame.yMin, transform.position.z);

        Gizmos.color = new Color(0f, 0.8f, 1f, 1f);
        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }
}
