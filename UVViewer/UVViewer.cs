/******
 * Written by Bunny83 
 * 
 * Update 2016.10.11
 *  - Added triangle list to better visualize where each vertex of a triangle is.
 *    This is done in the sceneview as well as in the UV view itself.
 * Update 2020.03.09
 *  - Fixed a few problems with newer versions of Unity. (PrefabUtility, SceneView callback)
 *  - The background color alpha value is now used to blend the texture
 *  - Using the mouse wheel on the "wrap" toggle button will increase / decrease
 *    the wrap count. Middle mouse button resets to the default (5)
 *  
 * 
 ******/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace B83.UVViewer
{
    public class UV_SubMesh
    {
        public Material m_Material;
        public int[] m_Indices = null;
    }

    public class UVViewer : EditorWindow
    {
        private enum TextureMode
        {
            HideTexture = 0,
            PointFilteredWithoutAlpha = 1,
            PointFilteredWithAlpha = 2,
            BilinearFilteredWithAlpha = 6,
            BilinearWithoutAlpha = 5,
        }

        [MenuItem("Tools/B83/UVViewer")]
        static void Init()
        {
            GetWindow<UVViewer>();
            //Instance.Focus();
        }
        [System.NonSerialized]
        private Mesh m_Mesh = null;
        [System.NonSerialized]
        private Renderer m_Renderer = null;
        [System.NonSerialized]
        private List<Vector2[]> m_UV = new List<Vector2[]>();
        [System.NonSerialized]
        private Vector3[] m_Vertices = null;
        [System.NonSerialized]
        private Vector3[] m_Normals = null;
        [System.NonSerialized]
        private int[] m_Triangles = null;
        [System.NonSerialized]
        private List<UV_SubMesh> m_Submeshes = new List<UV_SubMesh>();
        [System.NonSerialized]
        private UV_SubMesh m_Submesh = null;
        private int m_CurrentUVSet = 0;
        private bool m_DrawLines = true;
        private bool m_DrawTriangles = true;
        private TextureMode m_TextureMode = TextureMode.PointFilteredWithAlpha;
        private int m_CurrentIndex;
        private Color m_BackGroundColor = new Color(0, 0, 0, 0.50f);
        private Color m_FrontFaceColor = new Color(0, 1, 0, 0.05f);
        private Color m_BackFaceColor = new Color(1, 0, 0, 0.05f);
        private bool m_ClearBackGround = true;
        private bool m_ShowWrap = true;
        private int m_WrapCount = 5;
        private int m_SelectedTriangle = -1;
        [System.NonSerialized]
        private MeshCollider m_TempCollider = null;

        private Vector2 m_MousePos;
        private Rect m_ViewPort;
        private float m_Zoom = 1.0f;
        private float m_ZoomFactor = 1.0f;
        private float m_Width = 200.0f;
        private Vector2 m_Offset = new Vector2(0, 0);
        private Texture m_Texture;
        private Material m_TextureMapAlphaOnly;
        private Material m_TextureMapNormal;

        private bool m_ShowTriangleList = false;
        private Vector2 m_TriangleListScrollPos;
        private int m_SelectedVertex = -1;
        private int m_SelectedVertTriangle = -1;

        private Rect m_TriListRect;
        private int m_TriList_First = 0;
        private int m_TriList_Last = 0;


        void OnEnable()
        {
            titleContent = new GUIContent("UV Viewer");
            minSize = new Vector2(650,200);
            UpdateMesh();

            SceneView.duringSceneGui += OnSceneGUI;
            // pre 2019
            // SceneView.onSceneGUIDelegate += OnSceneGUI;

            wantsMouseMove = true;
            m_TextureMapAlphaOnly = (Material)AssetDatabase.LoadAssetAtPath("Assets/Editor/Shaders/GUIRenderAlphaMap.mat", typeof(Material));
            m_TextureMapNormal = (Material)AssetDatabase.LoadAssetAtPath("Assets/Editor/Shaders/GUIRenderWithoutAlpha.mat", typeof(Material));
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            // pre 2019
            // SceneView.onSceneGUIDelegate -= OnSceneGUI;

            if (m_TempCollider != null)
                DestroyImmediate(m_TempCollider);
        }


        void OnSelectionChange()
        {
            UpdateMesh();
        }

        void MyRepaint()
        {
            var viewCenter = new Vector2(m_ViewPort.width, m_ViewPort.height) * 0.5f;
            SetNewZoom(m_Zoom + 0.001f, viewCenter);
            SetNewZoom(m_Zoom - 0.001f, viewCenter);
            Repaint();
            if (SceneView.currentDrawingSceneView != null)
                SceneView.currentDrawingSceneView.Repaint();
        }

        void UpdateMesh()
        {
            if (m_TempCollider != null)
                DestroyImmediate(m_TempCollider);
            m_Mesh = null;
            try
            {
                var GO = Selection.activeGameObject;
                if (GO == null)
                    return;
                // do not tinker with prefab assets.
                if (PrefabUtility.IsPartOfPrefabAsset(GO))
                    return;
                var MF = GO.GetComponent<MeshFilter>();
                if (MF != null)
                    m_Mesh = MF.sharedMesh;
                else
                {
                    var SMR = GO.GetComponent<SkinnedMeshRenderer>();
                    if (SMR != null)
                        m_Mesh = SMR.sharedMesh;
                }
                if (m_Mesh == null) return;
                m_Renderer = GO.GetComponent<Renderer>();
                if (GO.GetComponent<MeshCollider>() == null)
                {
                    m_TempCollider = GO.AddComponent<MeshCollider>();
                    m_TempCollider.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
                }
                if (m_Renderer == null) return;
                var materials = m_Renderer.sharedMaterials;
                m_Submeshes.Clear();
                for (int i = 0; i < m_Mesh.subMeshCount; i++)
                {
                    var SM = new UV_SubMesh();
                    m_Submeshes.Add(SM);
                    SM.m_Indices = m_Mesh.GetIndices(i);
                    SM.m_Material = materials[i % materials.Length];
                }
                m_Triangles = m_Mesh.triangles;
                m_Vertices = m_Mesh.vertices;
                m_Normals = m_Mesh.normals;
                m_UV.Clear();
                if (m_Mesh.uv != null && m_Mesh.uv.Length > 0)
                    m_UV.Add(m_Mesh.uv);
                else
                    m_UV.Add(null);
                if (m_Mesh.uv2 != null && m_Mesh.uv2.Length > 0)
                    m_UV.Add(m_Mesh.uv2);
                else
                    m_UV.Add(null);
                if (m_Mesh.uv3 != null && m_Mesh.uv3.Length > 0)
                    m_UV.Add(m_Mesh.uv3);
                else
                    m_UV.Add(null);
                if (m_Mesh.uv4 != null && m_Mesh.uv4.Length > 0)
                    m_UV.Add(m_Mesh.uv4);
                else
                    m_UV.Add(null);
                m_CurrentUVSet = -1;
                for (int i = 0; i < m_UV.Count; i++)
                {
                    if (m_UV[i] != null)
                    {
                        m_CurrentUVSet = i;
                        break;
                    }
                }
                m_Submesh = m_Submeshes[0];
                m_Texture = m_Submesh.m_Material.mainTexture;
                m_SelectedVertex = -1;
                m_SelectedVertTriangle = -1;
            }
            finally
            {
                MyRepaint();
            }
        }

        bool SelectButton(string aCaption, bool aState)
        {
            bool b = GUILayout.Toggle(aState, aCaption, "Button");
            return b != aState;
        }

        void SetNewZoom(float aNewZoom, Vector2 aZoomCenter)
        {
            if (aNewZoom != m_Zoom)
            {
                var old = (m_Offset - aZoomCenter) / m_ZoomFactor;
                m_Zoom = aNewZoom;
                m_ZoomFactor = Mathf.Pow(2.0f, m_Zoom);
                if (m_Texture != null)
                {
                    m_Width = m_Texture.width * m_ZoomFactor;
                }
                else
                {
                    m_Width = 512f * m_ZoomFactor;
                }
                m_Offset = old * m_ZoomFactor + aZoomCenter;
            }
        }

        void DrawZoomGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel)
            {
                if (m_ViewPort.Contains(e.mousePosition))
                {
                    if (e.delta.y > 0)
                        SetNewZoom(m_Zoom - 0.1f, m_MousePos);
                    else if (e.delta.y < 0)
                        SetNewZoom(m_Zoom + 0.1f, m_MousePos);
                }
                Repaint();
            }

            var viewCenter = new Vector2(m_ViewPort.width, m_ViewPort.height) * 0.5f;
            GUILayout.Label("Zoom:", GUILayout.Width(47));
            GUI.SetNextControlName("ZoomSlider");
            SetNewZoom(EditorGUILayout.Slider(m_Zoom, -5, 10), viewCenter);

            if (e.type == EventType.MouseDown && !GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && GUI.GetNameOfFocusedControl() == "ZoomSlider")
            {
                GUIUtility.keyboardControl = -1;
            }
        }

        int FindClosestVertex(Vector2 aUV)
        {
            if (m_UV == null || m_CurrentUVSet < 0 || m_UV[m_CurrentUVSet].Length == 0)
                return -1;
            var uvSet = m_UV[m_CurrentUVSet];

            int index = 0;
            float dist = (uvSet[index] - aUV).sqrMagnitude;
            for (int i = 1; i < uvSet.Length; i++)
            {
                float d = (uvSet[i] - aUV).sqrMagnitude;
                if (d < dist)
                {
                    index = i;
                    dist = d;
                }
            }
            return index;
        }

        void VertexButton(int aIndex)
        {
            if (aIndex == m_SelectedVertex)
                GUI.color = new Color(1f, 0.5f, 0.5f, 1f);
            else
                GUI.color = new Color(0.5f, 1f, 0.5f, 1f);
            if (GUILayout.Button("V" + m_Submesh.m_Indices[aIndex], GUILayout.Width(50)))
            {
                m_SelectedVertex = aIndex;
                m_SelectedVertTriangle = -1;
                //MyRepaint();
                SceneView.RepaintAll();
            }
        }

        void DrawTriangleList()
        {
            GUILayout.BeginVertical("Window",GUILayout.Width(255));
            m_TriangleListScrollPos = GUILayout.BeginScrollView(m_TriangleListScrollPos);
            if (m_Submesh == null || m_Submesh.m_Indices == null)
            {
                GUILayout.Label("No selected submesh or there's something wrong with this submesh");
            }
            else
            {
                Color old = GUI.color;
                int triCount = m_Submesh.m_Indices.Length / 3;
                if (Event.current.type == EventType.Layout)
                {
                    m_TriList_First = Mathf.Clamp(Mathf.FloorToInt(m_TriangleListScrollPos.y / 20), 0, triCount - 1);
                    m_TriList_Last = Mathf.Clamp(m_TriList_First + Mathf.CeilToInt(m_TriListRect.height / 20) + 1, 0, triCount - 1);
                }
                GUILayout.Space(m_TriList_First * 20);
                for (int i = m_TriList_First; i <= m_TriList_Last; i++)
                {
                    int off = i * 3;
                    if (i == m_SelectedVertTriangle)
                        GUI.color = new Color(1f,0.7f,0.7f,1f);
                    if (i == m_SelectedTriangle)
                        GUI.color = Color.red;
                    GUILayout.BeginHorizontal("box", GUILayout.Height(20));
                    if (i == m_SelectedVertTriangle)
                        GUI.color = new Color(1f, 0.5f, 0.5f, 1f);
                    else
                        GUI.color = Color.yellow;
                    if (GUILayout.Button("T"+i, GUILayout.Width(50)))
                    {
                        m_SelectedVertTriangle = i;
                        m_SelectedVertex = -1;
                        SceneView.RepaintAll();
                    }
                    VertexButton(off + 0);
                    VertexButton(off + 1);
                    VertexButton(off + 2);
                    GUILayout.EndHorizontal();
                    GUI.color = old;
                }
                GUILayout.Space((triCount- m_TriList_Last) * 20);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
                m_TriListRect = GUILayoutUtility.GetLastRect();
        }

        void OnGUI()
        {
            if (m_Mesh == null)
            {
                GUILayout.Label("Select a GameObject in the scene with a MeshRenderer");
                return;
            }
            Event e = Event.current;
            m_MousePos = e.mousePosition;
            m_MousePos.x -= m_ViewPort.x;
            m_MousePos.y -= m_ViewPort.y;
            var UV = (m_MousePos - m_Offset) / m_Width;
            UV.y = 1.0f - UV.y;

            var TexArea = new Rect(m_Offset.x, m_Offset.y, m_Width, m_Width);
            if (e.type == EventType.Layout)
            {
                m_Texture = m_Submesh.m_Material.mainTexture;
            }

            GUILayout.BeginHorizontal();
            for (int i = 0; i < m_Submeshes.Count; i++)
            {
                if (SelectButton("Submesh_" + i, m_Submesh == m_Submeshes[i]))
                    m_Submesh = m_Submeshes[i];
            }
            if (GUILayout.Toggle(m_ShowTriangleList, "TriangleList", "Button", GUILayout.ExpandWidth(false)))
            {
                m_ShowTriangleList = true;
                SceneView.RepaintAll();
            }
            else
            {
                m_ShowTriangleList = false;
                SceneView.RepaintAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            DrawZoomGUI();
            m_DrawLines = GUILayout.Toggle(m_DrawLines, "DrawLines", "Button", GUILayout.Width(100));
            m_DrawTriangles = GUILayout.Toggle(m_DrawTriangles, "DrawTriangles", "Button", GUILayout.Width(100));
            if (GUILayout.Button("" + m_TextureMode))
            {
                while (true)
                {
                    m_TextureMode = m_TextureMode + 1;
                    if ((int)m_TextureMode > 6)
                    {
                        m_TextureMode = 0;
                        break;
                    }

                    if (("" + m_TextureMode).Length > 2)
                        break;
                }
            }

            bool tmpWrap = GUILayout.Toggle(m_ShowWrap, "Wrap", "Button", GUILayout.Width(50));
            if (tmpWrap != m_ShowWrap)
            {
                if (e.button == 0)
                    m_ShowWrap = tmpWrap;
                else
                    m_WrapCount = 5;
            }
            if (GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                if (e.delta.y < 0)
                    m_WrapCount = Mathf.Max(m_WrapCount - 1, 0);
                else
                    m_WrapCount = Mathf.Min(m_WrapCount + 1, 5000);
            }

            m_ClearBackGround = GUILayout.Toggle(m_ClearBackGround, "ClearBK", "Button", GUILayout.Width(60));
            m_BackGroundColor = EditorGUILayout.ColorField(m_BackGroundColor, GUILayout.Width(50));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            var oldColor = GUI.color;
            for (int i = 0; i < m_UV.Count; i++)
            {
                if (m_UV[i] == null)
                {
                    GUI.color = new Color(1,0.6f,0.6f,0.8f);
                    GUILayout.Label("No uv" + i, "Button");
                }
                else
                {
                    GUI.color = oldColor;
                    if (SelectButton("uv" + i, m_CurrentUVSet == i))
                        m_CurrentUVSet = i;
                }
            }
            GUI.color = oldColor;
            GUILayout.Label("Front:", GUILayout.Width(50));
            m_FrontFaceColor = EditorGUILayout.ColorField(m_FrontFaceColor, GUILayout.Width(50));
            GUILayout.Label("Back:", GUILayout.Width(50));
            m_BackFaceColor = EditorGUILayout.ColorField(m_BackFaceColor, GUILayout.Width(50));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Offset: " + m_Offset);
            GUILayout.Label("viewSize: " + m_ViewPort);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var R = GUILayoutUtility.GetRect(50, 10000, 50, 10000);
            if (m_ShowTriangleList)
                DrawTriangleList();
            GUILayout.EndHorizontal();
            if (e.type != EventType.Layout)
                m_ViewPort = R;

            if (e.type == EventType.MouseDrag && GUIUtility.hotControl == 0)
            {
                m_Offset += e.delta;
                MyRepaint();
            }
            if (e.type == EventType.MouseMove)
            {
                Repaint();
            }

            if (e.type == EventType.Repaint)
            {
                Drawing.EditorWindowViewport(position, m_ViewPort);
                if (m_ClearBackGround)
                    GL.Clear(true, true, m_BackGroundColor);
                if (m_Texture != null)
                {
                    var old = m_Texture.filterMode;

                    if (m_TextureMode != TextureMode.HideTexture)
                    {
                        if (((int)m_TextureMode & 4) != 0)
                            m_Texture.filterMode = FilterMode.Bilinear;
                        else
                            m_Texture.filterMode = FilterMode.Point;
                        if (m_ShowWrap)
                        {
                            
                            Rect Rtmp = TexArea;
                            var center = Rtmp.center;
                            Rtmp.width *= 1+ m_WrapCount*2;
                            Rtmp.height *= 1+ m_WrapCount*2;
                            Rtmp.center = center;
                            GUI.color = new Color(1, 1, 1, 0.5f*m_BackGroundColor.a);
                            Graphics.DrawTexture(Rtmp, m_Texture, new Rect(-m_WrapCount, -m_WrapCount, 1+ m_WrapCount*2, 1+ m_WrapCount*2), 0, 0, 0, 0, GUI.color, m_TextureMapAlphaOnly);
                        }
                        GUI.color = new Color(1, 1, 1, /*0.5f */ m_BackGroundColor.a);
                        Graphics.DrawTexture(TexArea, m_Texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, m_TextureMapNormal);
                    }
                    m_Texture.filterMode = old;
                }

                Drawing.BeginGL(new Color(1, 1, 1, 0.2f), GL.LINES);
                if (m_ShowWrap)
                {
                    for (int i = -m_WrapCount; i <= m_WrapCount+1; i++)
                    {
                        GL.Vertex(new Vector2(m_Offset.x + m_Width * i, 0));
                        GL.Vertex(new Vector2(m_Offset.x + m_Width * i, 10000));
                        GL.Vertex(new Vector2(0, m_Offset.y + m_Width * i));
                        GL.Vertex(new Vector2(10000, m_Offset.y + m_Width * i));
                    }
                }
                GL.Color(new Color(1, 1, 0, 0.2f));
                for (int i = 0; i < 2; i++)
                {
                    GL.Vertex(new Vector2(m_Offset.x + m_Width * i, 0));
                    GL.Vertex(new Vector2(m_Offset.x + m_Width * i, 10000));
                    GL.Vertex(new Vector2(0, m_Offset.y + m_Width * i));
                    GL.Vertex(new Vector2(10000, m_Offset.y + m_Width * i));
                }
                GL.End();

                if (m_CurrentUVSet >= 0)
                {
                    if (m_DrawLines)
                    {
                        var tmpRand = new System.Random(5);
                        Drawing.BeginGL(Color.red, GL.LINES);
                        for (int i = 0; i < m_Submesh.m_Indices.Length; i += 3)
                        {
                            GL.Color(new Color((float)tmpRand.NextDouble(), (float)tmpRand.NextDouble(), (float)tmpRand.NextDouble()));
                            var P1 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 0]];
                            var P2 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 1]];
                            var P3 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 2]];
                            P1.y = 1.0f - P1.y;
                            P2.y = 1.0f - P2.y;
                            P3.y = 1.0f - P3.y;
                            P1 = Vector2.Scale(P1, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            P2 = Vector2.Scale(P2, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            P3 = Vector2.Scale(P3, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            GL.Vertex(P1);
                            GL.Vertex(P2);
                            GL.Vertex(P2);
                            GL.Vertex(P3);
                            GL.Vertex(P3);
                            GL.Vertex(P1);
                        }
                        GL.End();
                    }

                    if (m_DrawTriangles)
                    {
                        var tmpRand = new System.Random(5);
                        Drawing.BeginGL(new Color(1, 0, 0, 0.2f), GL.TRIANGLES);
                        for (int i = 0; i < m_Submesh.m_Indices.Length; i += 3)
                        {
                            var P1 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 0]];
                            var P2 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 1]];
                            var P3 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[i + 2]];
                            P1.y = 1.0f - P1.y;
                            P2.y = 1.0f - P2.y;
                            P3.y = 1.0f - P3.y;
                            P1 = Vector2.Scale(P1, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            P2 = Vector2.Scale(P2, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            P3 = Vector2.Scale(P3, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                            var FColor = m_FrontFaceColor;
                            FColor *= 0.5f + (float)(tmpRand.NextDouble() * 0.5);
                            FColor.a = m_FrontFaceColor.a;
                            var BColor = m_BackFaceColor;
                            BColor *= 0.5f + (float)(tmpRand.NextDouble() * 0.5);
                            BColor.a = m_BackFaceColor.a;
                            GL.Color(FColor);
                            GL.Vertex(P1);
                            GL.Vertex(P2);
                            GL.Vertex(P3);

                            GL.Color(BColor);

                            GL.Vertex(P2);
                            GL.Vertex(P1);
                            GL.Vertex(P3);
                        }
                        GL.End();
                    }

                    if (m_SelectedTriangle >= 0 && m_SelectedTriangle< m_Triangles.Length)
                    {
                        Drawing.BeginGL(new Color(1, 0, 0, 0.2f), GL.TRIANGLES);
                        
                        var P1 = m_UV[m_CurrentUVSet][m_Triangles[m_SelectedTriangle * 3 + 0]];
                        var P2 = m_UV[m_CurrentUVSet][m_Triangles[m_SelectedTriangle * 3 + 1]];
                        var P3 = m_UV[m_CurrentUVSet][m_Triangles[m_SelectedTriangle * 3 + 2]];
                        P1.y = 1.0f - P1.y;
                        P2.y = 1.0f - P2.y;
                        P3.y = 1.0f - P3.y;
                        P1 = Vector2.Scale(P1, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                        P2 = Vector2.Scale(P2, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                        P3 = Vector2.Scale(P3, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);

                        GL.Color(new Color(0, 0, 1, 0.5f));

                        GL.Vertex(P2);
                        GL.Vertex(P1);
                        GL.Vertex(P3);

                        GL.Color(new Color(1, 0, 1, 0.5f));

                        GL.Vertex(P1);
                        GL.Vertex(P2);
                        GL.Vertex(P3);

                        GL.End();
                    }

                    if (m_SelectedVertex >= 0 && m_Submesh != null && m_Submesh.m_Indices != null)
                    {
                        m_CurrentUVSet = Mathf.Clamp(m_CurrentUVSet, 0, m_UV.Count-1);
                        m_SelectedVertex = Mathf.Clamp(m_SelectedVertex, 0, m_Submesh.m_Indices.Length - 1);
                        var P1 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[m_SelectedVertex]];
                        P1.y = 1.0f - P1.y;
                        P1 = Vector2.Scale(P1, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);

                        Drawing.BeginGL(new Color(1, 0, 0, 0.2f), GL.QUADS);

                        GL.Color(new Color(1, 1, 1, 0.7f));

                        GL.Vertex(P1 - Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P1 + Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P1 + Vector2.up * 5 - Vector2.right * 5);
                        GL.Vertex(P1 - Vector2.up * 5 - Vector2.right * 5);

                        GL.End();
                    }
                    if (m_SelectedVertTriangle >= 0 && m_Submesh != null && m_Submesh.m_Indices != null)
                    {
                        m_CurrentUVSet = Mathf.Clamp(m_CurrentUVSet, 0, m_UV.Count - 1);
                        m_SelectedVertTriangle = Mathf.Clamp(m_SelectedVertTriangle, 0, m_Submesh.m_Indices.Length/3 - 1);
                        var P1 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[m_SelectedVertTriangle * 3 + 0]];
                        var P2 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[m_SelectedVertTriangle * 3 + 1]];
                        var P3 = m_UV[m_CurrentUVSet][m_Submesh.m_Indices[m_SelectedVertTriangle * 3 + 2]];
                        P1.y = 1.0f - P1.y;
                        P2.y = 1.0f - P2.y;
                        P3.y = 1.0f - P3.y;
                        P1 = Vector2.Scale(P1, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                        P2 = Vector2.Scale(P2, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);
                        P3 = Vector2.Scale(P3, new Vector2(TexArea.width, TexArea.height)) + new Vector2(TexArea.x, TexArea.y);

                        Drawing.BeginGL(new Color(1, 0, 0, 0.2f), GL.QUADS);

                        GL.Color(new Color(1, 1, 1, 0.7f));

                        GL.Vertex(P1 - Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P1 + Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P1 + Vector2.up * 5 - Vector2.right * 5);
                        GL.Vertex(P1 - Vector2.up * 5 - Vector2.right * 5);

                        GL.Vertex(P2 - Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P2 + Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P2 + Vector2.up * 5 - Vector2.right * 5);
                        GL.Vertex(P2 - Vector2.up * 5 - Vector2.right * 5);

                        GL.Vertex(P3 - Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P3 + Vector2.up * 5 + Vector2.right * 5);
                        GL.Vertex(P3 + Vector2.up * 5 - Vector2.right * 5);
                        GL.Vertex(P3 - Vector2.up * 5 - Vector2.right * 5);

                        GL.End();
                    }
                }

                if (e.control)
                {
                    Drawing.BeginGL(new Color(0, 1, 0, 0.5f), GL.LINES);
                    GL.Vertex(m_MousePos + Vector2.up * 7);
                    GL.Vertex(m_MousePos - Vector2.up * 7);
                    GL.Vertex(m_MousePos + Vector2.right * 7);
                    GL.Vertex(m_MousePos - Vector2.right * 7);
                    m_CurrentIndex = FindClosestVertex(UV);
                    if (m_CurrentIndex >= 0)
                    {
                        var p = m_UV[m_CurrentUVSet][m_CurrentIndex];
                        p.y = 1.0f - p.y;
                        var pDraw = p * m_Width + m_Offset;

                        GL.Color(new Color(1, 1, 1, 0.8f));
                        GL.Vertex(pDraw + Vector2.up * 70);
                        GL.Vertex(pDraw - Vector2.up * 70);
                        GL.Vertex(pDraw + Vector2.right * 70);
                        GL.Vertex(pDraw - Vector2.right * 70);
                        GL.Vertex(m_MousePos);
                        GL.Vertex(pDraw);

                        GL.Color(new Color(1, 1, 1, 0.3f));
                        foreach (var vert in Drawing.GetCircleLinePoints(m_MousePos, (pDraw - m_MousePos).magnitude, 60))
                        {
                            GL.Vertex(vert);
                        }
                        GL.End();
                        GUI.color = new Color(1, 1, 1, 0.8f);
                        GUI.Label(new Rect(m_MousePos.x + 10, m_MousePos.y - 25 - 2, 200, 25), "closest U:" + p.x.ToString("0.0000") + " V:" + p.y.ToString("0.0000"), "box");

                    }
                    else
                        GL.End();

                    GUI.Label(new Rect(m_MousePos.x + 10, m_MousePos.y + 2, 200, 25), "mouse U:" + UV.x.ToString("0.0000") + " V:" + UV.y.ToString("0.0000"), "box");
                    EditorGUIUtility.AddCursorRect(m_ViewPort, (MouseCursor)(-1));
                    SceneView.RepaintAll();
                }
            }
            else if(e.type == EventType.KeyDown)
            {
                if (m_Submesh != null && m_Submesh.m_Indices != null)
                {
                    if (e.keyCode == KeyCode.UpArrow)
                    {
                        if (m_SelectedVertex >= 0) m_SelectedVertex = Mathf.Clamp(m_SelectedVertex - 3, 0, m_Submesh.m_Indices.Length - 1);
                        if (m_SelectedVertTriangle>= 0) m_SelectedVertTriangle = Mathf.Clamp(m_SelectedVertTriangle - 1, 0, m_Submesh.m_Indices.Length/3 - 1);
                    }
                    else if (e.keyCode == KeyCode.DownArrow)
                    {
                        if (m_SelectedVertex >= 0) m_SelectedVertex = Mathf.Clamp(m_SelectedVertex + 3, 0, m_Submesh.m_Indices.Length - 1);
                        if (m_SelectedVertTriangle >= 0) m_SelectedVertTriangle = Mathf.Clamp(m_SelectedVertTriangle + 1, 0, m_Submesh.m_Indices.Length / 3 - 1);
                    }
                    else if (e.keyCode == KeyCode.LeftArrow)
                    {
                        if (m_SelectedVertex >= 0) m_SelectedVertex = Mathf.Clamp(m_SelectedVertex - 1, 0, m_Submesh.m_Indices.Length - 1);
                    }
                    else if (e.keyCode == KeyCode.RightArrow)
                    {
                        if (m_SelectedVertex >= 0) m_SelectedVertex = Mathf.Clamp(m_SelectedVertex + 1, 0, m_Submesh.m_Indices.Length - 1);
                    }
                    MyRepaint();
                }
            }
            if (GUI.changed)
            {
                MyRepaint();
            }
        }

        void DrawVertex(int aIndex, ref Matrix4x4 M)
        {
            Vector3 normal = m_Normals[aIndex];
            Vector3 vertPos = M.MultiplyPoint(m_Vertices[aIndex]);
            float size = HandleUtility.GetHandleSize(vertPos);
            Handles.ArrowHandleCap(-1, vertPos, Quaternion.LookRotation(normal), size * 0.5f, EventType.Repaint);
            Handles.CubeHandleCap(-1, vertPos, Quaternion.identity, size * 0.1f, EventType.Repaint);
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (m_Mesh == null || m_Renderer == null)
                return;
            Event e = Event.current;
            var col = m_Renderer.GetComponent<MeshCollider>();
            if (e.type == EventType.MouseMove && col != null)
            {
                var SV = sceneView;
                var P = e.mousePosition;
                var r = SV.position;
                r.x = r.y = 0;
                if (r.Contains(P))
                {

                    Ray ray = HandleUtility.GUIPointToWorldRay(P);
                    RaycastHit hit;
                    if (col.Raycast(ray, out hit, float.PositiveInfinity))
                    {
                        m_SelectedTriangle = hit.triangleIndex;
                    }
                    MyRepaint();
                    SceneView.RepaintAll();
                }
                else
                {
                    m_SelectedTriangle = -1;
                }
            }
            else if (e.type == EventType.Repaint)
            {
                var M = m_Renderer.localToWorldMatrix;
                GL.PushMatrix();
                if (m_DrawLines)
                {
                    var tmpRand = new System.Random(5);
                    Drawing.BeginGL(Color.red, GL.LINES);
                    for (int i = 0; i < m_Submesh.m_Indices.Length; i += 3)
                    {
                        GL.Color(new Color((float)tmpRand.NextDouble(), (float)tmpRand.NextDouble(), (float)tmpRand.NextDouble()));
                        var P1 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 0]]);
                        var P2 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 1]]);
                        var P3 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 2]]);
                        GL.Vertex(P1);
                        GL.Vertex(P2);
                        GL.Vertex(P2);
                        GL.Vertex(P3);
                        GL.Vertex(P3);
                        GL.Vertex(P1);
                    }
                    GL.End();
                }

                if (m_DrawTriangles)
                {
                    var tmpRand = new System.Random(5);
                    GL.Clear(true, false, Color.black);
                    Drawing.BeginGL(new Color(1, 0, 0, 0.2f), GL.TRIANGLES, true);
                    for (int i = 0; i < m_Submesh.m_Indices.Length; i += 3)
                    {
                        var P1 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 0]]);
                        var P2 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 1]]);
                        var P3 = M.MultiplyPoint(m_Vertices[m_Submesh.m_Indices[i + 2]]);

                        var FColor = m_FrontFaceColor;
                        FColor *= 0.5f + (float)(tmpRand.NextDouble() * 0.5);
                        FColor.a = m_FrontFaceColor.a;
                        var BColor = m_BackFaceColor;
                        BColor *= 0.5f + (float)(tmpRand.NextDouble() * 0.5);
                        BColor.a = m_BackFaceColor.a;

                        GL.Color(FColor);
                        GL.Vertex(P1);
                        GL.Vertex(P2);
                        GL.Vertex(P3);

                        GL.Color(BColor);

                        GL.Vertex(P2);
                        GL.Vertex(P1);
                        GL.Vertex(P3);
                    }
                    GL.End();
                }

                if (e.control && m_Normals != null)
                {
                    m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Vertices.Length - 1);
                    var pos = m_Vertices[m_CurrentIndex];
                    var norm = m_Normals[m_CurrentIndex];

                    Drawing.BeginGL(new Color(1, 1, 0, 1f), GL.LINES, true);
                    pos = M.MultiplyPoint(pos);
                    norm = M.MultiplyVector(norm);
                    GL.Vertex(pos);
                    GL.Vertex(pos + norm * 30);
                    GL.End();
                }

                if (m_SelectedTriangle >= 0)
                {
                    m_SelectedTriangle = Mathf.Clamp(m_SelectedTriangle, 0, m_Triangles.Length / 3-1);
                    Drawing.BeginGL(new Color(0, 1, 0, 0.9f), GL.TRIANGLES, true);
                    var P1 = M.MultiplyPoint(m_Vertices[m_Triangles[m_SelectedTriangle * 3 + 0]]);
                    var P2 = M.MultiplyPoint(m_Vertices[m_Triangles[m_SelectedTriangle * 3 + 1]]);
                    var P3 = M.MultiplyPoint(m_Vertices[m_Triangles[m_SelectedTriangle * 3 + 2]]);
                    var normal = Vector3.Cross(P2 - P1, P3 - P1).normalized * 0.0001f;
                    GL.Vertex(P1 + normal);
                    GL.Vertex(P2 + normal);
                    GL.Vertex(P3 + normal);
                    GL.End();
                }

                if (m_ShowTriangleList && m_SelectedVertex >= 0 && m_Submesh != null && m_Submesh.m_Indices != null)
                {
                    int index = m_Submesh.m_Indices[m_SelectedVertex];
                    DrawVertex(index, ref M);
                }

                if (m_ShowTriangleList && m_SelectedVertTriangle >= 0 && m_Submesh != null && m_Submesh.m_Indices != null)
                {
                    int index = m_SelectedVertTriangle*3;
                    if (index < m_Submesh.m_Indices.Length)
                    {
                        int i0 = m_Submesh.m_Indices[index + 0];
                        int i1 = m_Submesh.m_Indices[index + 1];
                        int i2 = m_Submesh.m_Indices[index + 2];
                        DrawVertex(i0, ref M);
                        DrawVertex(i1, ref M);
                        DrawVertex(i2, ref M);
                        Drawing.BeginGL(new Color(1, 0, 0, 1f), GL.LINES, true);
                        var P1 = M.MultiplyPoint(m_Vertices[i0]);
                        var P2 = M.MultiplyPoint(m_Vertices[i1]);
                        var P3 = M.MultiplyPoint(m_Vertices[i2]);
                        var normal = Vector3.Cross(P2 - P1, P3 - P1).normalized * 0.0001f;
                        GL.Vertex(P1 + normal);
                        GL.Vertex(P2 + normal);
                        GL.Vertex(P2 + normal);
                        GL.Vertex(P3 + normal);
                        GL.Vertex(P3 + normal);
                        GL.Vertex(P1 + normal);
                        GL.End();
                    }
                }
                GL.PopMatrix();
            }
        }
    }


    // *****************************
    // * merged from seperate file *
    // *****************************
    public static class Drawing
    {
        public static Texture2D aaLineTex = null;
        public static Texture2D lineTex = null;
        private static Material m_LineMat = null;
        private static Material m_LineMatDepthTest = null;
        public static Material m_ActiveMaterial = null;

        public static Material LineMat
        {
            get
            {
                if (m_LineMat == null)
                {
                    m_LineMat = Resources.Load<Material>("Lines_Colored_Blended");
#if UNITY_EDITOR
                    if (m_LineMat == null)
                    {
                        var resDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(Application.dataPath, "Resources"));
                        if (!resDir.Exists)
                            resDir.Create();
                        Shader s = Shader.Find("Lines/Colored Blended");
                        if (s == null)
                        {
                            string shaderText = "Shader \"Lines/Colored Blended\" {" +
                                                "SubShader { Pass {" +
                                                "	BindChannels { Bind \"Color\",color }" +
                                                "	Blend SrcAlpha OneMinusSrcAlpha" +
                                                "	ZWrite On Cull Back ZTest Always Fog { Mode Off }" +
                                                "} } }";
                            string path = System.IO.Path.Combine(resDir.FullName, "Lines_Colored_Blended.shader");
                            Debug.Log("Shader missing, create asset: " + path);
                            System.IO.File.WriteAllText(path, shaderText);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            AssetDatabase.LoadAssetAtPath<Shader>("Resources/Lines_Colored_Blended.shader");
                            s = Shader.Find("Lines/Colored Blended");
                        }
                        var mat = new Material(s);
                        mat.name = "Lines/Colored Blended";
                        AssetDatabase.CreateAsset(mat, "Assets/Resources/Lines_Colored_Blended.mat");
                        m_LineMat = mat;
                    }
#endif
                }
                return m_LineMat;
            }
        }

        public static Material LineMatDepthTest
        {
            get
            {
                if (m_LineMatDepthTest == null)
                {
                    m_LineMatDepthTest = Resources.Load<Material>("Lines_Colored_Blended_Depth");
#if UNITY_EDITOR
                    if (m_LineMatDepthTest == null)
                    {
                        var resDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(Application.dataPath, "Resources"));
                        if (!resDir.Exists)
                            resDir.Create();
                        Shader s = Shader.Find("Lines/Colored Blended with DepthTest");
                        if (s == null)
                        {
                            string shaderText = "Shader \"Lines/Colored Blended with DepthTest\" {" +
                                           "SubShader { Pass {" +
                                           "	BindChannels { Bind \"Color\",color }" +
                                           "	Blend SrcAlpha OneMinusSrcAlpha" +
                                           "	ZWrite On Cull Back ZTest LEqual Fog { Mode Off }" +
                                           "} } }";
                            string path = System.IO.Path.Combine(resDir.FullName, "Lines_Colored_Blended_Depth.shader");
                            Debug.Log("Shader missing, create asset: " + path);
                            System.IO.File.WriteAllText(path, shaderText);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            AssetDatabase.LoadAssetAtPath<Shader>("Resources/Lines_Colored_Blended_Depth.shader");
                            s = Shader.Find("Lines/Colored Blended with DepthTest");
                        }
                        var mat = new Material(s);
                        mat.name = "Lines/Colored Blended with DepthTest";
                        AssetDatabase.CreateAsset(mat, "Assets/Resources/Lines_Colored_Blended_Depth.mat");
                        m_LineMatDepthTest = mat;
                    }
#endif
                }
                return m_LineMatDepthTest;
            }
        }

        public static void GUIViewport(Rect aR)
        {
            var P = GUIUtility.GUIToScreenPoint(new Vector2(aR.x, aR.y));
            aR.x = P.x;
            aR.y = Screen.height - P.y - aR.height;
            GL.Viewport(aR);
            GL.LoadPixelMatrix(0, aR.width, aR.height, 0);
        }

        public static void EditorWindowViewport(Rect aWindowRect, Rect aGUIArea)
        {
            aGUIArea.y = aWindowRect.height - aGUIArea.yMax;
            GL.Viewport(aGUIArea);
            GL.LoadPixelMatrix(0, aGUIArea.width, aGUIArea.height, 0);
        }


        public static void BeginGL(Color aColor, int aMode)
        {
            BeginGL(aColor, aMode, false);
        }


        public static void BeginGL(Color aColor, int aMode, bool aDepthTest)
        {
            if (aDepthTest)
                m_ActiveMaterial = LineMatDepthTest;
            else
                m_ActiveMaterial = LineMat;
            m_ActiveMaterial.SetPass(0);
            GL.Begin(aMode);
            GL.Color(aColor);
        }

        public static void DrawGLLine(Vector2 pointA, Vector2 pointB, Color color)
        {
            BeginGL(color, GL.LINES);
            GL.Vertex3(pointA.x, pointA.y, 0);
            GL.Vertex3(pointB.x, pointB.y, 0);
            GL.End();
        }

        public static void DrawRect(Rect R, Color color)
        {
            BeginGL(color, GL.QUADS);
            GL.TexCoord(Vector2.zero);
            GL.Vertex3(R.xMin, R.yMin, 0);
            GL.Vertex3(R.xMax, R.yMin, 0);
            GL.Vertex3(R.xMax, R.yMax, 0);
            GL.Vertex3(R.xMin, R.yMax, 0);
            GL.End();
        }

        public struct CircleEnumerator
        {
            Vector2 center;
            Vector2 current;
            Vector2 start;
            float radius;
            int segments;
            int i;
            bool state;
            public CircleEnumerator(Vector2 aCenter, float aRadius, int aSegments)
            {
                center = aCenter;
                radius = aRadius;
                segments = aSegments;
                current = Vector2.zero;
                start = Vector2.zero;
                i = 0;
                state = false;
            }
            public Vector2 Current
            {
                get { return current; }
            }

            public void Dispose() { }
            public bool MoveNext()
            {
                if (i < segments)
                {
                    if (state || i == 0)
                    {
                        float angle = (float)i / (segments - 1) * Mathf.PI * 2;
                        current = center;
                        current.x += Mathf.Cos(angle) * radius;
                        current.y += Mathf.Sin(angle) * radius;
                        if (i == 0)
                            start = current;
                        i++;
                    }
                    state = !state;
                    return true;
                }
                else if (i == segments)
                {
                    i++;
                    return true;
                }
                else if (i == segments + 1)
                {
                    current = start;
                    i++;
                    return true;
                }
                else
                    return false;
            }

            public void Reset()
            {
                i = 0;
                state = false;
            }
        }

        public struct CircleEnumerable
        {
            Vector2 center;
            float radius;
            int segments;
            public CircleEnumerable(Vector2 aCenter, float aRadius, int aSegments)
            {
                center = aCenter;
                radius = aRadius;
                segments = aSegments;
            }

            public CircleEnumerator GetEnumerator()
            {
                return new CircleEnumerator(center, radius, segments);
            }

        }

        public static CircleEnumerable GetCircleLinePoints(Vector2 aCenter, float aRadius, int segments)
        {
            return new CircleEnumerable(aCenter, aRadius, segments);
        }
    }

}
