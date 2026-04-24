using System.Collections.Generic;
using UnityEngine;

public static class GridAStar2D
{
    public readonly struct Settings
    {
        public Settings(
            float cellSize,
            LayerMask obstacleLayers,
            Vector2 probeSize,
            Vector2 probeOffset,
            bool allowDiagonal,
            int maxIterations,
            float maxSearchDistance,
            Transform ignoredRoot = null)
        {
            this.cellSize = Mathf.Max(0.05f, cellSize);
            this.obstacleLayers = obstacleLayers;
            this.probeSize = new Vector2(
                Mathf.Max(0.05f, probeSize.x),
                Mathf.Max(0.05f, probeSize.y));
            this.probeOffset = probeOffset;
            this.allowDiagonal = allowDiagonal;
            this.maxIterations = Mathf.Max(64, maxIterations);
            this.maxSearchDistance = Mathf.Max(this.cellSize * 2f, maxSearchDistance);
            this.ignoredRoot = ignoredRoot;
        }

        public readonly float cellSize;
        public readonly LayerMask obstacleLayers;
        public readonly Vector2 probeSize;
        public readonly Vector2 probeOffset;
        public readonly bool allowDiagonal;
        public readonly int maxIterations;
        public readonly float maxSearchDistance;
        public readonly Transform ignoredRoot;
    }

    private sealed class NodeRecord
    {
        public Vector2Int cell;
        public Vector2Int parent;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    public static bool TryFindPath(Vector2 startWorld, Vector2 goalWorld, Settings settings, List<Vector2> results)
    {
        results.Clear();

        Vector2Int startCell = WorldToCell(startWorld, settings.cellSize);
        Vector2Int goalCell = WorldToCell(goalWorld, settings.cellSize);

        if (!TryGetNearestWalkableCell(startCell, settings, out startCell) ||
            !TryGetNearestWalkableCell(goalCell, settings, out goalCell))
            return false;

        if (startCell == goalCell)
        {
            results.Add(CellToWorld(goalCell, settings.cellSize));
            return true;
        }

        List<NodeRecord> openList = new List<NodeRecord>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, NodeRecord> nodeLookup = new Dictionary<Vector2Int, NodeRecord>();

        NodeRecord startNode = new NodeRecord
        {
            cell = startCell,
            parent = startCell,
            gCost = 0f,
            hCost = Heuristic(startCell, goalCell, settings.allowDiagonal)
        };

        openList.Add(startNode);
        nodeLookup[startCell] = startNode;
        NodeRecord bestNode = startNode;

        int iterations = 0;
        while (openList.Count > 0 && iterations < settings.maxIterations)
        {
            iterations++;

            NodeRecord current = GetLowestCostNode(openList);
            if (current.cell == goalCell)
            {
                ReconstructPath(current, nodeLookup, settings.cellSize, results);
                return true;
            }

            if (current.hCost < bestNode.hCost ||
                (Mathf.Approximately(current.hCost, bestNode.hCost) && current.gCost < bestNode.gCost))
            {
                bestNode = current;
            }

            openList.Remove(current);
            closedSet.Add(current.cell);

            ExploreDirections(CardinalDirections, current, goalCell, settings, openList, closedSet, nodeLookup, startCell);

            if (settings.allowDiagonal)
            {
                ExploreDirections(DiagonalDirections, current, goalCell, settings, openList, closedSet, nodeLookup, startCell);
            }
        }

        // Fall back to the closest reachable cell we discovered so NPCs can
        // stop short of furniture-hugging waypoints instead of getting stuck.
        if (bestNode != null && bestNode.cell != startCell)
        {
            ReconstructPath(bestNode, nodeLookup, settings.cellSize, results);
            return results.Count > 0;
        }

        return false;
    }

    private static bool TryGetNearestWalkableCell(Vector2Int origin, Settings settings, out Vector2Int walkableCell)
    {
        if (IsWalkable(origin, settings))
        {
            walkableCell = origin;
            return true;
        }

        int maxRadius = Mathf.Max(1, Mathf.CeilToInt(settings.probeSize.magnitude / settings.cellSize) + 2);

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int candidate = new Vector2Int(origin.x + x, origin.y + y);
                    if (IsWalkable(candidate, settings))
                    {
                        walkableCell = candidate;
                        return true;
                    }
                }
            }
        }

        walkableCell = origin;
        return false;
    }

    private static void ExploreDirections(
        Vector2Int[] directions,
        NodeRecord current,
        Vector2Int goalCell,
        Settings settings,
        List<NodeRecord> openList,
        HashSet<Vector2Int> closedSet,
        Dictionary<Vector2Int, NodeRecord> nodeLookup,
        Vector2Int startCell)
    {
        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborCell = current.cell + direction;

            if (closedSet.Contains(neighborCell))
                continue;

            if (Vector2.Distance(CellToWorld(startCell, settings.cellSize), CellToWorld(neighborCell, settings.cellSize)) > settings.maxSearchDistance)
                continue;

            if (!IsWalkable(neighborCell, settings))
                continue;

            if (direction.x != 0 && direction.y != 0)
            {
                Vector2Int horizontal = new Vector2Int(current.cell.x + direction.x, current.cell.y);
                Vector2Int vertical = new Vector2Int(current.cell.x, current.cell.y + direction.y);
                if (!IsWalkable(horizontal, settings) || !IsWalkable(vertical, settings))
                    continue;
            }

            float moveCost = current.gCost + ((direction.x != 0 && direction.y != 0) ? 1.4142135f : 1f);

            if (!nodeLookup.TryGetValue(neighborCell, out NodeRecord neighborNode))
            {
                neighborNode = new NodeRecord
                {
                    cell = neighborCell,
                    parent = current.cell,
                    gCost = moveCost,
                    hCost = Heuristic(neighborCell, goalCell, settings.allowDiagonal)
                };

                nodeLookup[neighborCell] = neighborNode;
                openList.Add(neighborNode);
                continue;
            }

            if (moveCost < neighborNode.gCost)
            {
                neighborNode.parent = current.cell;
                neighborNode.gCost = moveCost;

                if (!openList.Contains(neighborNode))
                    openList.Add(neighborNode);
            }
        }
    }

    private static NodeRecord GetLowestCostNode(List<NodeRecord> openList)
    {
        NodeRecord bestNode = openList[0];

        for (int i = 1; i < openList.Count; i++)
        {
            NodeRecord candidate = openList[i];
            if (candidate.fCost < bestNode.fCost ||
                (Mathf.Approximately(candidate.fCost, bestNode.fCost) && candidate.hCost < bestNode.hCost))
            {
                bestNode = candidate;
            }
        }

        return bestNode;
    }

    private static void ReconstructPath(NodeRecord goalNode, Dictionary<Vector2Int, NodeRecord> nodeLookup, float cellSize, List<Vector2> results)
    {
        List<Vector2> reversedPath = new List<Vector2>();
        NodeRecord current = goalNode;

        while (true)
        {
            reversedPath.Add(CellToWorld(current.cell, cellSize));
            if (current.parent == current.cell)
                break;

            current = nodeLookup[current.parent];
        }

        for (int i = reversedPath.Count - 1; i >= 0; i--)
            results.Add(reversedPath[i]);
    }

    private static float Heuristic(Vector2Int a, Vector2Int b, bool allowDiagonal)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        if (!allowDiagonal)
            return dx + dy;

        int diagonal = Mathf.Min(dx, dy);
        int straight = Mathf.Abs(dx - dy);
        return diagonal * 1.4142135f + straight;
    }

    private static bool IsWalkable(Vector2Int cell, Settings settings)
    {
        Vector2 worldCenter = CellToWorld(cell, settings.cellSize) + settings.probeOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, settings.probeSize, 0f, settings.obstacleLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.enabled || hit.isTrigger)
                continue;

            if (IsPlayerCollider(hit))
                continue;

            if (settings.ignoredRoot != null && hit.transform.IsChildOf(settings.ignoredRoot))
                continue;

            return false;
        }

        return true;
    }

    private static bool IsPlayerCollider(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
            return true;

        Transform transform = collider.transform;
        if (transform.CompareTag("Player"))
            return true;

        Rigidbody2D attachedBody = collider.attachedRigidbody;
        if (attachedBody != null)
        {
            if (attachedBody.CompareTag("Player"))
                return true;

            if (attachedBody.transform.CompareTag("Player"))
                return true;
        }

        Transform root = transform.root;
        return root != null && root.CompareTag("Player");
    }

    private static Vector2Int WorldToCell(Vector2 worldPosition, float cellSize)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / cellSize),
            Mathf.RoundToInt(worldPosition.y / cellSize));
    }

    private static Vector2 CellToWorld(Vector2Int cell, float cellSize)
    {
        return new Vector2(cell.x * cellSize, cell.y * cellSize);
    }
}
