using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SnapshootFrameAnimationEditorWindow : EditorWindow
{

    [MenuItem("Tool/SnapshootFrameAnimation")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SnapshootFrameAnimationEditorWindow));
    }

    //private static readonly string DEFAULT_SAVE_DIR = Application.dataPath;
    private static readonly string DEFAULT_FOLDER = "";
    private static readonly string DEFAULT_FILE_NAME = "ParticleSystem";
    private static readonly int SNAPSHOOT_LAYER = 30;

    private Camera m_camera = null;
    private int m_particleSystemCount = 0;
    private List<ParticleSystem> m_particleSystems = new List<ParticleSystem>();
    private string m_saveFolder = null;
    private string m_fileName = null;
    private int m_startTime = 0; //per millisecond
    private int m_interval = 0;  //per millisecond
    private int m_duration = 0;  //per millisecond
    private int m_width = 0;
    private int m_height = 0;
    private Color m_backgroundColor = new Color(0, 0, 0, 0);

    private Camera m_useCamra;
    private int[] m_oldLayers;
    private bool m_isSnapshot;

    private void OnGUI()
    {
        ParticleSystem particleSystem;
        List<ParticleSystem> particleSystemList;
        int count;
        int i;

        if (m_camera == null)
            m_camera = Camera.main;

        GUILayout.BeginVertical();
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Camera");
                m_camera = EditorGUILayout.ObjectField(m_camera, typeof(Camera), true) as Camera;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {

                GUILayout.Label("Particle System");
                GUILayout.BeginVertical();
                {
                    bool updateFromScene = GUILayout.Button("Update From Scene");
                    if (updateFromScene)
                    {
                        _updateParticleSystemFromScene();
                    }

                    count = m_particleSystemCount;

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Size");
                        count = EditorGUILayout.IntField(count);
                    }
                    GUILayout.EndHorizontal();

                    m_particleSystemCount = count;
                    particleSystemList = m_particleSystems;
                    if (count < particleSystemList.Count)
                        particleSystemList.RemoveRange(count, particleSystemList.Count - count);

                    for (i = 0; i < count; ++i)
                    {
                        if (i < particleSystemList.Count)
                        {
                            particleSystem = particleSystemList[i];
                        }
                        else
                        {
                            particleSystem = null;
                        }

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("ParticleSystem " + i);
                            particleSystem = EditorGUILayout.ObjectField(particleSystem, typeof(ParticleSystem), true) as ParticleSystem;
                        }
                        GUILayout.EndHorizontal();

                        if (i < particleSystemList.Count)
                        {
                            particleSystemList[i] = particleSystem;
                        }
                        else
                        {
                            particleSystemList.Add(particleSystem);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                bool isSelectSaveFolder = GUILayout.Button("Select Save Folder.");
                if (isSelectSaveFolder)
                {
                    string title = "Select Directory For Save Image";
                    string pathDesktop = Application.dataPath;
                    string defaultFolder = DEFAULT_FOLDER;
                    m_saveFolder = EditorUtility.SaveFolderPanel(title, pathDesktop, defaultFolder);
                    //if (String.IsNullOrEmpty(m_saveFolder)) m_saveFolder = pathDesktop + "\\" + defaultFolder;
                }
                string currSaveFolder = _getCurrSaveFolder();
                GUILayout.Label(currSaveFolder);

            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Export File Name");
                m_fileName = EditorGUILayout.TextField(m_fileName);
                if (String.IsNullOrEmpty(m_fileName)) m_fileName = DEFAULT_FILE_NAME;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Start Time (s)");
                m_startTime = (int)Math.Round(EditorGUILayout.FloatField(m_startTime / 1000f) * 1000f);
                if (m_startTime < 0) m_startTime = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Frame Rate (t/s)");
                float currFrameRate;
                if (m_interval <= 0)
                {
                    currFrameRate = 0;
                }
                else
                {
                    currFrameRate = 1.0f / (m_interval / 1000.0f);
                }

                currFrameRate = EditorGUILayout.FloatField(currFrameRate);
                if (currFrameRate <= 0)
                {
                    m_interval = 0;
                }
                else
                {
                    m_interval = (int)Math.Round(1.0f / currFrameRate * 1000f);
                }

            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Duration (s)");
                m_duration = (int)Math.Round(EditorGUILayout.FloatField(m_duration / 1000f) * 1000f);
                if (m_duration < 0) m_duration = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Width Of Export Image (pixel)");
                m_width = EditorGUILayout.IntField(m_width);
                if (m_width < 0) m_width = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Height Of Export Image (pixel) ");
                m_height = EditorGUILayout.IntField(m_height);
                if (m_height < 0) m_height = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Background Color");
                m_backgroundColor = EditorGUILayout.ColorField(m_backgroundColor);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                bool start = GUILayout.Button("Start");
                if (start)
                {
                    _createFrameAnimation();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if(m_isSnapshot)GUILayout.Label("working, wait...");
            }
            GUILayout.EndHorizontal();
        }

    }

    private void _updateParticleSystemFromScene()
    {
        ParticleSystem[] particleSystems = GameObject.FindObjectsOfType<ParticleSystem>();
        List<ParticleSystem> particleSystemList = m_particleSystems;
        int count = particleSystems != null ? particleSystems.Length : 0;
        int count2 = particleSystemList.Count;
        int i;
        for (i = 0; i < count; ++i)
        {
            //check if game obect is active in scene.
            if (particleSystems[i].gameObject.activeInHierarchy)
            {
                if (i < count2)
                {
                    particleSystemList[i] = particleSystems[i];
                }
                else
                {
                    particleSystemList.Add(particleSystems[i]);
                }
            }
        }

        for (; i < count2; ++i)
        {
            particleSystemList[i] = null;
        }

        if (m_particleSystemCount < count)
            m_particleSystemCount = count;
    }

    private void _createFrameAnimation()
    {
        if (m_isSnapshot) return;
        m_isSnapshot = true;
        Camera camera = m_camera;
        List<ParticleSystem> particleSystems = m_particleSystems;
        int count = m_particleSystemCount;
        int width = m_width;
        int height = m_height;
        int duration = m_duration;
        int interval = m_interval;

        //check
        if (camera == null)
        {
            Debug.LogError("Invalid Camera. The camera is null.");
            return;
        }
        if (width == 0 || height == 0)
        {
            Debug.LogWarning("Width or height of export image is equal to 0. Export action end ！");
            return;
        }
        if (duration == 0)
        {
            Debug.LogWarning("Duration is equal to 0. Export action end ！");
            return;
        }
        if (interval == 0)
        {
            Debug.LogWarning("Interval is equal to 0. Export action end ！");
            return;
        }

        //create (copy) objects.
        GameObject gameObject_Camera = camera.gameObject;
        gameObject_Camera = Instantiate(gameObject_Camera);
        camera = gameObject_Camera.GetComponent<Camera>();
        
        ParticleSystem particleSystem;
        //only render objects in the layer SNAPSHOT_LAYER.
        camera.cullingMask = 1 << SNAPSHOOT_LAYER;

        //set particleSystem gameOject layer
        int[] oldLayers = new int[count];
        for (int i = 0; i < count; ++i)
        {
            particleSystem = particleSystems[i];
            if (particleSystem != null)
            {
                //save old layer data;
                oldLayers[i] = particleSystem.gameObject.layer;

                particleSystem.gameObject.layer = SNAPSHOOT_LAYER;
            }
        }
        m_oldLayers = oldLayers;

        m_useCamra = camera;
        var cameraHelper = gameObject_Camera.AddComponent<CameraHelper>();
        cameraHelper.StartSnapshot(m_startTime,
            m_duration,
            m_interval,
            m_backgroundColor,
            m_width,
            m_height,
            _onCompelete);
        
    }

    private void _onCompelete()
    {
        var camera = m_useCamra;
        var gameObject_Camera = camera.gameObject;
        var cameraHelper = gameObject_Camera.GetComponent<CameraHelper>();
        var renderTextures = cameraHelper.getRenderTextures();

        var oldLayers = m_oldLayers;
        var particleSystems = m_particleSystems;
        int count = m_particleSystemCount;
        ParticleSystem particleSystem;
        //recover particleSystem gameObject layer set
        for (int i = 0; i < count; ++i)
        {
            particleSystem = particleSystems[i];
            if (particleSystem != null)
            {
                particleSystem.gameObject.layer = oldLayers[i];
            }
        }


        camera.targetTexture = null;
        //destory objects;
        DestroyImmediate(gameObject_Camera);
        m_useCamra = null;
        //read pixel from render texture and export data to image.
        _readTextureAndExport(renderTextures);

        m_isSnapshot = false;
    }

    private void _readTextureAndExport(List<RenderTexture> renderTextures)
    {
        string saveFolder = _getCurrSaveFolder();
        string fileName = m_fileName;
        int count = renderTextures.Count;
        for (int i = 0; i < count; ++i)
        {
            RenderTexture.active = renderTextures[i];
            Texture2D texture = new Texture2D(m_width, m_height);
            texture.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0);
            texture.Apply();

            var bytesData = texture.EncodeToPNG();

            string path = saveFolder + "/" + fileName + i + ".png";
            File.WriteAllBytes(path, bytesData);
            DestroyImmediate(texture);
        }

        RenderTexture.active = null;

    }

    private string _getCurrSaveFolder()
    {
        return String.IsNullOrEmpty(m_saveFolder) ? Application.dataPath : m_saveFolder;
    }
}
