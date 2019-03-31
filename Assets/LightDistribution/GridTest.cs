using UnityEngine;
using System.Collections.Generic;

public class GridTest : MonoBehaviour
{
    public TileLogic tilePrefab;

    List<TileLogic> grid = new List<TileLogic>();

    const int GRID_SIZE = 50;

    static GridTest singleton;

    public bool visualiseAlgorithm = false;

    public static bool ready = false;

    public static bool isAnimationPlaying = false;

    public void Start()
    {
        singleton = this;

        for (int y = 0; y < GRID_SIZE; ++y)
        {
            for (int x = 0; x < GRID_SIZE; ++x)
            {
                TileLogic tile = Instantiate(tilePrefab, new Vector3(x, y, 0.0f), Quaternion.identity) as TileLogic;

                tile.x         = x;
                tile.y         = y;
                tile.intensity = 0;

                tile.fixLight();

                grid.Add(tile);
            }
        }

        ready = true;
    }

    public static void updateLightAt(int x, int y)
    {
        if (isAnimationPlaying) return;

        if (singleton.visualiseAlgorithm) singleton._updateLightAt(x, y);
        else                              singleton._updateLightAt2(x, y);
    }

    static Queue<int> resetUpdates    = new Queue<int>();
    static Queue<int> increaseUpdates = new Queue<int>();

    int lastX                = 0;
    int lastY                = 0;
    bool enteredSecondBranch = false;
    bool enteredThirdBranch  = false;

    float interval = 0.0f;
    float verboseInterval = 0.0f;

    public float stepDuration = 0.25f;
    public float verboseDuration = 2.0f;

    void Update()
    {
        interval += Time.deltaTime;
        verboseInterval += Time.deltaTime;

        if (isAnimationPlaying && interval > stepDuration)
        {
            _updateLightAt(lastX, lastY);

            interval = 0.0f;
        }

        // if (verboseInterval > verboseDuration)
        // {
            // Debug.Log("IU Queue = " + increaseUpdates.Count + " : RU Queue = " + resetUpdates.Count);
        // }
    }

    const int BITS_FOR_X     = 8;
    const int BITS_FOR_Y     = BITS_FOR_X;
    const int BITS_FOR_POS   = BITS_FOR_X + BITS_FOR_Y;
    const int BITS_FOR_LIGHT = 8;

    const int FLAG_FORCE_UPDATE = 1 << (BITS_FOR_LIGHT + BITS_FOR_POS);

    const int OFFSET_X     = 1 << (BITS_FOR_X - 1);
    const int OFFSET_Y     = 1 << (BITS_FOR_Y - 1);

    const int MASK_FOR_X     = (1 << BITS_FOR_X) - 1;
    const int MASK_FOR_Y     = (1 << BITS_FOR_Y) - 1;
    const int MASK_FOR_LIGHT = (1 << BITS_FOR_LIGHT) - 1;

    void _updateLightAt(int x, int y)
    {    
        if (!isAnimationPlaying)
        {
            isAnimationPlaying = true;

            lastX = x;
            lastY = y;

            enteredSecondBranch = false;
            enteredThirdBranch  = false;
        }

        if (computeLight(x, y) > getIntensity(x, y) && !enteredSecondBranch)
        {
            increaseUpdates.Enqueue((OFFSET_Y << BITS_FOR_X) | OFFSET_X);
        }
        else if (!enteredThirdBranch)
        {
            if (!enteredSecondBranch) resetUpdates.Enqueue((getIntensity(x, y) << BITS_FOR_POS) | (OFFSET_Y << BITS_FOR_X) | OFFSET_X);

            enteredSecondBranch = true;

            while (resetUpdates.Count > 0)
            {
                int updateData = resetUpdates.Dequeue();

                int currentX = ((updateData >> 0)          & MASK_FOR_X) - OFFSET_X + x;
                int currentY = ((updateData >> BITS_FOR_X) & MASK_FOR_Y) - OFFSET_Y + y;

                if (currentX < 0 || currentX >= GRID_SIZE || currentY < 0 || currentY >= GRID_SIZE) continue;

                int currentLight = getIntensity(currentX, currentY);
                int initialLight = (updateData >> BITS_FOR_POS) & MASK_FOR_LIGHT;

                getTile(currentX, currentY).unsign();

                if (initialLight == currentLight)
                {
                    getTile(currentX, currentY).intensity = 0;

                    if (initialLight > 0)
                    {
                        if (getIntensity(currentX, currentY + 1) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                        if (getIntensity(currentX, currentY - 1) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y - 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                        if (getIntensity(currentX - 1, currentY) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x - 1 + OFFSET_X));
                        if (getIntensity(currentX + 1, currentY) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x + 1 + OFFSET_X));

                        if (getIntensity(currentX, currentY + 1) > 0 && getTile(currentX, currentY + 1) != null) getTile(currentX, currentY + 1).sign(0,0,255);
                        if (getIntensity(currentX, currentY - 1) > 0 && getTile(currentX, currentY - 1) != null) getTile(currentX, currentY - 1).sign(0,0,255);
                        if (getIntensity(currentX - 1, currentY) > 0 && getTile(currentX - 1, currentY) != null) getTile(currentX - 1, currentY).sign(0,0,255);
                        if (getIntensity(currentX + 1, currentY) > 0 && getTile(currentX + 1, currentY) != null) getTile(currentX + 1, currentY).sign(0,0,255);
                    }
                }
                else if (currentLight > 0)
                {
                    increaseUpdates.Enqueue(updateData | FLAG_FORCE_UPDATE);
                    getTile(currentX, currentY).sign(255, 255, 0);
                }

                return;
            }
        }

        enteredThirdBranch = true;

        while (increaseUpdates.Count > 0)
        {
            int updateData = increaseUpdates.Dequeue();

            int currentX = ((updateData >> 0)          & MASK_FOR_X) - OFFSET_X + x;
            int currentY = ((updateData >> BITS_FOR_X) & MASK_FOR_Y) - OFFSET_Y + y;

            if (currentX < 0 || currentX >= GRID_SIZE || currentY < 0 || currentY >= GRID_SIZE) continue;

            int currentLight = getIntensity(currentX, currentY);
            int correctLight = computeLight(currentX, currentY);

            getTile(currentX, currentY).unsign();
            getTile(currentX, currentY).intensity = correctLight;

            bool forceUpdate = (updateData & FLAG_FORCE_UPDATE) > 0;

            if ((correctLight > currentLight) || forceUpdate)
            {
                if (getIntensity(currentX, currentY + 1) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                if (getIntensity(currentX, currentY - 1) < correctLight - 1) increaseUpdates.Enqueue((currentY - y - 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                if (getIntensity(currentX - 1, currentY) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x - 1 + OFFSET_X));
                if (getIntensity(currentX + 1, currentY) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x + 1 + OFFSET_X));

                if (getIntensity(currentX, currentY + 1) < correctLight - 1 && getTile(currentX, currentY + 1) != null) getTile(currentX, currentY + 1).sign(255, 255, 0);
                if (getIntensity(currentX, currentY - 1) < correctLight - 1 && getTile(currentX, currentY - 1) != null) getTile(currentX, currentY - 1).sign(255, 255, 0);
                if (getIntensity(currentX - 1, currentY) < correctLight - 1 && getTile(currentX - 1, currentY) != null) getTile(currentX - 1, currentY).sign(255, 255, 0);
                if (getIntensity(currentX + 1, currentY) < correctLight - 1 && getTile(currentX + 1, currentY) != null) getTile(currentX + 1, currentY).sign(255, 255, 0);
            }

            return;
        }

        isAnimationPlaying = false;
    }

    void _updateLightAt2(int x, int y)
    {
        int currentLight = getIntensity(x, y);
        int correctLight = computeLight(x, y);

        if (correctLight > currentLight)
        {
            increaseUpdates.Enqueue((OFFSET_Y << BITS_FOR_X) | OFFSET_X);
        }
        else
        {
            resetUpdates.Enqueue((currentLight << BITS_FOR_POS) | (OFFSET_Y << BITS_FOR_X) | OFFSET_X);

            while (resetUpdates.Count > 0)
            {
                int updateData = resetUpdates.Dequeue();

                int currentX = ((updateData >> 0)          & MASK_FOR_X) - OFFSET_X + x;
                int currentY = ((updateData >> BITS_FOR_X) & MASK_FOR_Y) - OFFSET_Y + y;

                if (currentX < 0 || currentX >= GRID_SIZE || currentY < 0 || currentY >= GRID_SIZE) continue;

                currentLight     = getIntensity(currentX, currentY);
                int initialLight = (updateData >> BITS_FOR_POS) & MASK_FOR_LIGHT;

                if (initialLight == currentLight)
                {
                    getTile(currentX, currentY).intensity = 0;

                    if (initialLight > 0)
                    {
                        if (getIntensity(currentX, currentY + 1) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                        if (getIntensity(currentX, currentY - 1) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y - 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                        if (getIntensity(currentX - 1, currentY) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x - 1 + OFFSET_X));
                        if (getIntensity(currentX + 1, currentY) > 0) resetUpdates.Enqueue((initialLight - 1) << BITS_FOR_POS | (currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x + 1 + OFFSET_X));
                    }
                }
                else if (currentLight > 0)
                {
                    increaseUpdates.Enqueue(updateData | FLAG_FORCE_UPDATE);
                }
            }
        }

        while (increaseUpdates.Count > 0)
        {
            int updateData = increaseUpdates.Dequeue();

            int currentX = ((updateData >> 0)          & MASK_FOR_X) - OFFSET_X + x;
            int currentY = ((updateData >> BITS_FOR_X) & MASK_FOR_Y) - OFFSET_Y + y;

            if (currentX < 0 || currentX >= GRID_SIZE || currentY < 0 || currentY >= GRID_SIZE) continue;

            currentLight = getIntensity(currentX, currentY);
            correctLight = computeLight(currentX, currentY);

            getTile(currentX, currentY).intensity = correctLight;

            bool forceUpdate = (updateData & FLAG_FORCE_UPDATE) > 0;

            if ((correctLight > currentLight) || forceUpdate)
            {
                if (getIntensity(currentX, currentY + 1) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                if (getIntensity(currentX, currentY - 1) < correctLight - 1) increaseUpdates.Enqueue((currentY - y - 1 + OFFSET_Y) << BITS_FOR_X | (currentX - x + OFFSET_X));
                if (getIntensity(currentX - 1, currentY) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x - 1 + OFFSET_X));
                if (getIntensity(currentX + 1, currentY) < correctLight - 1) increaseUpdates.Enqueue((currentY - y + OFFSET_Y) << BITS_FOR_X | (currentX - x + 1 + OFFSET_X));
            }
        }
    }

    int computeLight(int x, int y)
    {
        int result = 0;

        TileLogic tile = getTile(x, y);

        if (tile.lightSource) return TileLogic.MAX_LIGHT;
        if (tile.blockLight)  return 0;

        TileLogic top    = getTile(x, y + 1);
        TileLogic bottom = getTile(x, y - 1);
        TileLogic left   = getTile(x - 1, y);
        TileLogic right  = getTile(x + 1, y);

        if (top    != null) result = top.blockLight    ? result : top.intensity;
        if (bottom != null) result = bottom.blockLight ? result : Mathf.Max(result, bottom.intensity);
        if (left   != null) result = left.blockLight   ? result : Mathf.Max(result, left.intensity);
        if (right  != null) result = right.blockLight  ? result : Mathf.Max(result, right.intensity);

        return result - 1;
    }

    int getIntensity(int x, int y)
    {
        TileLogic result = getTile(x, y);

        return result != null ? result.intensity : 0;
    }

    TileLogic getTile(int x, int y)
    {
        if (x < 0 || x >= GRID_SIZE || y < 0 || y >= GRID_SIZE) return null;

        return grid[GRID_SIZE * y + x];
    }
}
