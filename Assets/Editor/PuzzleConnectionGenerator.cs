using UnityEngine;
using UnityEditor;

public class PuzzleConnectionGenerator : EditorWindow
{
    float neighborDistance = 250f;

    [MenuItem("Tools/Generate Puzzle Connections")]
    static void Init()
    {
        PuzzleConnectionGenerator window =
            (PuzzleConnectionGenerator)EditorWindow.GetWindow(typeof(PuzzleConnectionGenerator));
        window.titleContent = new GUIContent("Puzzle Generator");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Puzzle Connection Generator", EditorStyles.boldLabel);

        neighborDistance = EditorGUILayout.FloatField("Neighbor Distance", neighborDistance);
        if (GUILayout.Button("Generate Connections"))
            GenerateConnections();
    }

    void GenerateConnections()          // generate the connection offsets for the puzzle pieces automatically
    {
        PuzzlePiece[] pieces = FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.None);
        foreach (var piece in pieces)
        {
            System.Collections.Generic.List<PuzzlePiece.PieceConnection> connections =
                new System.Collections.Generic.List<PuzzlePiece.PieceConnection>();

            foreach (var other in pieces)
            {
                if (piece == other) continue;

                float dist = Vector2.Distance(piece.rect.position, other.rect.position);
                if (dist < neighborDistance)
                {
                    PuzzlePiece.PieceConnection connection =
                        new PuzzlePiece.PieceConnection();

                    connection.otherPiece = other;
                    connection.expectedOffset = (Vector2)other.rect.position - (Vector2)piece.rect.position;
                    connection.connected = false;
                    connections.Add(connection);
                }
            }
            piece.connections = connections.ToArray();
            EditorUtility.SetDirty(piece);
        }
        Debug.Log("Puzzle connection offsets generated!");
    }
}