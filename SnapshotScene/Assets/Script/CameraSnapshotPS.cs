using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

[ExecuteInEditMode]
public class CameraHelper : MonoBehaviour
{
    private float m_time = 0;
    private float m_endTime = 0;
    private int m_interval;
    private Color m_backgroundColor;
    private int m_width;
    private int m_height;
    private bool m_isWork;
    private Action m_completeFun;
    private List<RenderTexture> m_renderTextures = new List<RenderTexture>();
    private int[] m_oldLayers;

    private float m_startSysTime = 0;
    public bool StartSnapshot(int startTime,
        int duration,
        int interval,
        Color backgroundColor,
        int width,
        int height,
        Action completeFun)
    {
        if (m_isWork) return false;
        m_isWork = true;
        m_time = startTime;
        m_endTime = startTime + duration;
        m_interval = interval;
        m_backgroundColor = backgroundColor;
        m_width = width;
        m_height = height;
        m_completeFun = completeFun;
        m_renderTextures.Clear();
        m_start();
        m_startSysTime = Time.realtimeSinceStartup;
        return true;
    }

    public bool getIsWork()
    {
        return m_isWork;
    }

    public List<RenderTexture> getRenderTextures()
    {
        return m_renderTextures;
    }

    void OnEnable()
    {
        EditorApplication.update += Update;
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_isWork == false) return;
        var camera = GetComponent<Camera>();
        var renderTextures = m_renderTextures;
        var width = m_width;
        var height = m_height;
        var time = m_time;

        var passTime = Time.realtimeSinceStartup - m_startSysTime;
        passTime *= 1000;
        if (passTime < time) return;

        //start to render game objects by camera.

        //create render texture.
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        renderTextures.Add(renderTexture);

        //set camera.
        camera.targetTexture = renderTexture;
        camera.backgroundColor = m_backgroundColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.Render();
        time += m_interval;
        m_time = time;
        if (time > m_endTime)
        {
            m_isWork = false;
            m_completeFun();
            m_end();
        }




    }

    private void m_start()
    {
       
    }

    private void m_end()
    {
        //release render textures.
        var renderTextures = m_renderTextures;
        int count = renderTextures.Count;
        for (int i = 0; i < count; ++i)
        {
            RenderTexture.ReleaseTemporary(renderTextures[i]);
        }
    }
}
