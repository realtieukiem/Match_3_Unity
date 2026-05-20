using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;


public class ShapesManager : MonoBehaviour
{
    private struct BoardMove
    {
        public readonly GameObject First;
        public readonly GameObject Second;

        public BoardMove(GameObject first, GameObject second)
        {
            First = first;
            Second = second;
        }
    }

    public TMP_Text ScoreText;

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
    public bool ClipCandyToBoard = true;
    public float BoardMaskPadding = 0f;
    public int RandomLevelRows = 7;
    public int RandomLevelColumns = 7;
    [Range(0f, 1f)]
    public float X2ItemChance = 0.1f;
    [Range(0f, 1f)]
    public float X3ItemChance = 0.05f;

    [Header("Timing")]
    public float SwapAnimationDuration = 0.2f;
    public float CollapseMoveDurationPerCell = 0.05f;
    public Ease CollapseMoveEase = Ease.Linear;
    public float DelayBetweenCascadeMatches = 0f;
    public float HideBoardBeforeItemListDelay = 0f;
    public float ShowBoardAfterItemListDelay = 0f;

    private Vector2 BoardBottomLeft;
    private Vector2 CandySize;
    private SpriteMask boardSpriteMask;
    private Sprite boardMaskSprite;

    private GameState state = GameState.None;
    private GameObject hitGo = null;
    public GameObject[] CandyPrefabs;
    public GameObject[] ExplosionPrefabs;
    //public GameObject[] BonusPrefabs;

    private IEnumerator CheckPotentialMatchesCoroutine;
    private IEnumerator AnimatePotentialMatchesCoroutine;

    IEnumerable<GameObject> potentialMatches;

    void Start()
    {
        if (!HasRequiredPrefabs())
            return;

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
            if (item == null)
                continue;

            Shape shape = item.GetComponent<Shape>();
            if (shape != null)
                shape.Type = item.name;
        }
    }

    private bool HasRequiredPrefabs()
    {
        if (CandyPrefabs != null && CandyPrefabs.Length > 0)
        {
            foreach (GameObject prefab in CandyPrefabs)
            {
                if (prefab != null && prefab.GetComponent<Shape>() != null)
                    return true;
            }
        }

        Debug.LogError("ShapesManager requires at least one candy prefab with a Shape component.", this);
        enabled = false;
        return false;
    }

    public void InitializeCandyAndSpawnPositionsFromPremadeLevel()
    {
        InitializeVariables();

        var premadeLevel = DebugUtilities.FillShapesArrayFromResourcesData();

        if (shapes != null)
            DestroyAllCandy();

        Constants.ConfigureBoardSize(premadeLevel.GetLength(0), premadeLevel.GetLength(1));
        shapes = new ShapesArray();
        UpdateBoardLayout();
        RefreshBoardMask();

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
    }


    public void InitializeCandyAndSpawnPositions()
    {
        InitializeVariables();

        if (shapes != null)
            DestroyAllCandy();

        Constants.ConfigureBoardSize(RandomLevelRows, RandomLevelColumns);
        shapes = new ShapesArray();
        UpdateBoardLayout();
        RefreshBoardMask();

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
        ApplyBoardMask(go);
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


    void Update()
    {
        if (GameController.Instance != null && !GameController.Instance.CanCurrentPlayerAct())
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        if (state == GameState.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shape selectedShape = GetShapeUnderPointer(mainCamera);
                if (selectedShape != null)
                {
                    hitGo = selectedShape.gameObject;
                    state = GameState.SelectionStarted;
                }
            }
        }
        else if (state == GameState.SelectionStarted)
        {
            if (Input.GetMouseButton(0))
            {
                Shape targetShape = GetShapeUnderPointer(mainCamera);
                if (targetShape == null || hitGo == targetShape.gameObject)
                    return;

                StopCheckForPotentialMatches();

                Shape selectedShape = hitGo.GetComponent<Shape>();
                if (!Utilities.AreVerticalOrHorizontalNeighbors(selectedShape, targetShape))
                {
                    CancelCurrentSelection();
                    return;
                }

                state = GameState.Animating;
                if (GameController.Instance != null)
                    GameController.Instance.BeginCurrentTurnAction();

                StartBoardMove(hitGo, targetShape.gameObject);
            }
        }
    }

    private Shape GetShapeUnderPointer(Camera mainCamera)
    {
        RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider == null)
            return null;

        return hit.collider.GetComponent<Shape>();
    }

    public void CancelCurrentSelection()
    {
        if (state == GameState.Animating)
            return;

        hitGo = null;
        state = GameState.None;
    }

    public bool TryPlayAutomaticMove()
    {
        if (state != GameState.None || shapes == null)
            return false;

        List<BoardMove> validMoves = FindValidMoves();
        if (validMoves.Count == 0)
            return false;

        BoardMove selectedMove = validMoves[Random.Range(0, validMoves.Count)];
        StartBoardMove(selectedMove.First, selectedMove.Second);
        return true;
    }

    private List<BoardMove> FindValidMoves()
    {
        List<BoardMove> validMoves = new List<BoardMove>();
        for (int row = 0; row < shapes.Rows; row++)
        {
            for (int column = 0; column < shapes.Columns; column++)
            {
                GameObject item = shapes[row, column];
                if (item == null)
                    continue;

                TryAddValidMove(validMoves, item, row, column + 1);
                TryAddValidMove(validMoves, item, row + 1, column);
            }
        }

        return validMoves;
    }

    private void TryAddValidMove(List<BoardMove> validMoves, GameObject first, int secondRow, int secondColumn)
    {
        if (secondRow < 0 || secondRow >= shapes.Rows || secondColumn < 0 || secondColumn >= shapes.Columns)
            return;

        GameObject second = shapes[secondRow, secondColumn];
        if (second == null || !MoveCreatesMatch(first, second))
            return;

        validMoves.Add(new BoardMove(first, second));
    }

    private bool MoveCreatesMatch(GameObject first, GameObject second)
    {
        shapes.Swap(first, second);
        bool createsMatch = shapes.GetMatches(first).MatchedCandy
            .Union(shapes.GetMatches(second).MatchedCandy)
            .Count() >= Constants.MinimumMatches;
        shapes.UndoSwap();

        return createsMatch;
    }

    private void StartBoardMove(GameObject first, GameObject second)
    {
        StopCheckForPotentialMatches();
        hitGo = first;
        state = GameState.Animating;
        FixSortingLayer(first, second);
        StartCoroutine(FindMatchesAndCollapse(second));
    }

    private void FixSortingLayer(GameObject hitGo, GameObject hitGo2)
    {
        SpriteRenderer sp1 = hitGo.GetComponent<SpriteRenderer>();
        SpriteRenderer sp2 = hitGo2.GetComponent<SpriteRenderer>();
        if (sp1 == null || sp2 == null)
            return;

        if (sp1.sortingOrder <= sp2.sortingOrder)
        {
            sp1.sortingOrder = 1;
            sp2.sortingOrder = 0;
            hitGo.GetComponent<Shape>().RefreshCountTextSorting();
            hitGo2.GetComponent<Shape>().RefreshCountTextSorting();
        }
    }




    private IEnumerator FindMatchesAndCollapse(GameObject hitGo2)
    {
        Vector3 hitGoPosition = hitGo.transform.position;
        Vector3 hitGo2Position = hitGo2.transform.position;
        shapes.Swap(hitGo, hitGo2);

        //move the swapped ones
        yield return AnimateCandySwap(hitGo, hitGo2, hitGo2Position, hitGoPosition, SwapAnimationDuration);

        //get the matches via the helper methods
        var hitGomatchesInfo = shapes.GetMatches(hitGo);
        var hitGo2matchesInfo = shapes.GetMatches(hitGo2);

        var totalMatches = hitGomatchesInfo.MatchedCandy
            .Union(hitGo2matchesInfo.MatchedCandy).Distinct();

        //if user's swap didn't create at least a 3-match, undo their swap
        if (totalMatches.Count() < Constants.MinimumMatches)
        {
            yield return AnimateCandySwap(hitGo, hitGo2, hitGoPosition, hitGo2Position, SwapAnimationDuration);

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

            //audio
            AudioController.Instance.PlaySfx("Eat");

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

            MoveAndAnimate(newCandyInfo.Moves);
            MoveAndAnimate(collapsedCandyInfo.Moves);



            //will wait for both of the above animations
            yield return new WaitForSeconds(GetCollapseMoveDuration(maxDistance));

            if (DelayBetweenCascadeMatches > 0f)
                yield return new WaitForSeconds(DelayBetweenCascadeMatches);

            //search if there are matches with the new/collapsed items
            totalMatches = shapes.GetMatches(collapsedCandyInfo.AlteredCandy).
                Union(shapes.GetMatches(newCandyInfo.AlteredCandy)).Distinct();



            timesRun++;
        }

        if (matchedItemQueue.Count > 0)
        {
            ResetAllShapeOpacity();

            if (HideBoardBeforeItemListDelay > 0f)
                yield return new WaitForSeconds(HideBoardBeforeItemListDelay);

            SetAllShapesVisible(false);

            if (GameController.Instance != null)
                yield return GameController.Instance.PlayMatchedItems(matchedItemQueue);

            if (ShowBoardAfterItemListDelay > 0f)
                yield return new WaitForSeconds(ShowBoardAfterItemListDelay);

            ResetAllShapeOpacity();
            SetAllShapesVisible(true);
        }

        ResetAllShapeOpacity();
        bool shouldEndTurn = timesRun > 1;
        state = GameState.None;

        if (GameController.Instance != null)
            GameController.Instance.CompleteCurrentTurnAction(shouldEndTurn);

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
                {
                    if (visible)
                        ResetShapeOpacity(shape);

                    shape.SetActive(visible);
                }
            }
        }
    }


    private AlteredCandyInfo CreateNewCandyInSpecificColumns(IEnumerable<int> columnsWithMissingCandy)
    {
        AlteredCandyInfo newCandyInfo = new AlteredCandyInfo();

        //find how many null values the column has
        foreach (int column in columnsWithMissingCandy)
        {
            var emptyItems = shapes.GetEmptyItemsOnColumn(column).ToList();
            for (int i = 0; i < emptyItems.Count; i++)
            {
                ShapeInfo item = emptyItems[i];
                var go = GetRandomCandy();
                int spawnRow = Constants.Rows + i;
                GameObject newCandy = Instantiate(go, GetWorldPosition(spawnRow, column), Quaternion.identity)
                    as GameObject;

                newCandy.transform.SetParent(transform);
                newCandy.GetComponent<Shape>().Assign(go.GetComponent<Shape>().Type, item.Row, item.Column, GetRandomItemCount());
                newCandy.transform.localScale = Vector3.one * GetCandyScale();
                ApplyBoardMask(newCandy);

                int distance = spawnRow - item.Row;
                if (distance > newCandyInfo.MaxDistance)
                    newCandyInfo.MaxDistance = distance;

                shapes[item.Row, item.Column] = newCandy;
                newCandyInfo.AddCandy(newCandy, spawnRow, item.Row, column);
            }
        }
        return newCandyInfo;
    }

    private void MoveAndAnimate(IEnumerable<CandyMoveData> moves)
    {
        float stepDuration = Mathf.Max(0f, CollapseMoveDurationPerCell);
        foreach (CandyMoveData move in moves)
        {
            if (move.Candy == null)
                continue;

            move.Candy.transform.DOKill();
            bool startsOutsideBoard = move.StartRow >= Constants.Rows;
            if (startsOutsideBoard)
                SetNonSpriteRenderersVisible(move.Candy, false);

            Sequence dropSequence = DOTween.Sequence();
            for (int row = move.StartRow - 1; row >= move.TargetRow; row--)
            {
                dropSequence.Append(move.Candy.transform
                    .DOMove(GetWorldPosition(row, move.Column), stepDuration)
                    .SetEase(CollapseMoveEase));

                if (startsOutsideBoard && row == Constants.Rows - 1)
                    dropSequence.AppendCallback(() => SetNonSpriteRenderersVisible(move.Candy, true));
            }
        }
    }

    private void RefreshBoardMask()
    {
        if (!ClipCandyToBoard)
        {
            if (boardSpriteMask != null)
                boardSpriteMask.enabled = false;

            return;
        }

        if (boardSpriteMask == null)
        {
            GameObject maskGo = new GameObject("BoardSpriteMask");
            maskGo.transform.SetParent(transform);
            boardSpriteMask = maskGo.AddComponent<SpriteMask>();
        }

        if (boardMaskSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            boardMaskSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            boardMaskSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        float width = Constants.Columns * CandySize.x + BoardMaskPadding * 2f;
        float height = Constants.Rows * CandySize.y + BoardMaskPadding * 2f;
        Vector2 center = BoardBottomLeft + new Vector2((Constants.Columns - 1) * CandySize.x * 0.5f, (Constants.Rows - 1) * CandySize.y * 0.5f);

        boardSpriteMask.sprite = boardMaskSprite;
        boardSpriteMask.transform.position = center;
        boardSpriteMask.transform.localScale = new Vector3(width, height, 1f);
        boardSpriteMask.enabled = true;
    }

    private void ApplyBoardMask(GameObject candy)
    {
        SpriteMaskInteraction maskInteraction = ClipCandyToBoard
            ? SpriteMaskInteraction.VisibleInsideMask
            : SpriteMaskInteraction.None;

        SpriteRenderer[] spriteRenderers = candy.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.maskInteraction = maskInteraction;
    }

    private void SetNonSpriteRenderersVisible(GameObject candy, bool visible)
    {
        Renderer[] renderers = candy.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is SpriteRenderer)
                continue;

            renderer.enabled = visible;
        }
    }

    private float GetCollapseMoveDuration(int distance)
    {
        return Mathf.Max(0f, CollapseMoveDurationPerCell) * Mathf.Max(1, distance);
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
        if (explosion != null)
        {
            var newExplosion = Instantiate(explosion, item.transform.position, Quaternion.identity) as GameObject;
            newExplosion.transform.SetParent(transform);
            Destroy(newExplosion, Constants.ExplosionDuration);
        }

        Destroy(item);
    }

    private GameObject GetRandomCandy()
    {
        if (CandyPrefabs == null || CandyPrefabs.Length == 0)
            return null;

        for (int i = 0; i < CandyPrefabs.Length; i++)
        {
            GameObject prefab = CandyPrefabs[Random.Range(0, CandyPrefabs.Length)];
            if (prefab != null && prefab.GetComponent<Shape>() != null)
                return prefab;
        }

        foreach (GameObject prefab in CandyPrefabs)
        {
            if (prefab != null && prefab.GetComponent<Shape>() != null)
                return prefab;
        }

        return null;
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
        if (ScoreText != null)
            ScoreText.text = "Score: " + score.ToString();
    }

    private GameObject GetRandomExplosion()
    {
        if (ExplosionPrefabs == null || ExplosionPrefabs.Length == 0)
            return null;

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
        ResetAllShapeOpacity();
    }

    private void ResetOpacityOnPotentialMatches()
    {
        if (potentialMatches != null)
            foreach (var item in potentialMatches)
            {
                if (item == null)
                    continue;

                ResetShapeOpacity(item);
            }
    }

    private void ResetAllShapeOpacity()
    {
        if (shapes == null)
            return;

        for (int row = 0; row < shapes.Rows; row++)
        {
            for (int column = 0; column < shapes.Columns; column++)
            {
                GameObject shape = shapes[row, column];
                if (shape != null)
                    ResetShapeOpacity(shape);
            }
        }
    }

    private void ResetShapeOpacity(GameObject shape)
    {
        SpriteRenderer[] spriteRenderers = shape.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.DOKill();
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
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
                if (item == null)
                    continue;

                Shape shape = item.GetComponent<Shape>();
                if (shape != null && shape.Type.Contains(tokens[0].Trim()))
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
