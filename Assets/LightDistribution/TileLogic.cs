using UnityEngine;
using System.Collections.Generic;

public class TileLogic : MonoBehaviour
{
    public int intensity
    {
        get
        {
            return _intensity;
        }

        set
        {
            //numbers[_intensity].SetActive(false);

            _intensity = value;

            _intensity = Mathf.Clamp(_intensity, 0, MAX_LIGHT);

            //numbers[_intensity].SetActive(true);

            fixLight();
        }
    }

    public bool blockLight
    {
        get
        {
            return _blockLight;
        }

        set
        {
            if (GridTest.isAnimationPlaying) return;

            _blockLight = value;

            fixLight();
        }
    }

    public int x;
    public int y;

    public bool lightSource
    {
        get
        {
            return _lightSource;
        }

        set
        {
            if (GridTest.isAnimationPlaying) return;

            _lightSource = value;

            fixLight();
        }
    }

    int  _intensity   = 0;
    bool _blockLight  = false;
    bool _lightSource = false;
    bool signed       = false;

    public const int MAX_LIGHT = 7;

    [SerializeField] public List<GameObject> numbers = new List<GameObject>();

    public void fixLight()
    {
        if (signed) return;

        float value = ((float) intensity) / MAX_LIGHT;

        if (_blockLight)       GetComponent<Renderer>().material.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        else if (_lightSource) GetComponent<Renderer>().material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        else                   GetComponent<Renderer>().material.color = new Color(value, value, value, 1.0f);
    }

    public void sign(byte r, byte g, byte b)
    {
        signed = true;
        GetComponent<Renderer>().material.color = new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
    }

    public void unsign()
    {
        signed = false;
        fixLight();
    }

    void OnMouseOver()
    {
        if (!GridTest.ready) return;

        if (Input.GetMouseButtonDown(0))
        {
            lightSource = !lightSource;
            blockLight  = false;
            GridTest.updateLightAt(x, y);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            blockLight  = !blockLight;
            lightSource = false;
            GridTest.updateLightAt(x, y);
        }
        // else if (Input.GetMouseButtonDown(2)) blockLight = !blockLight;
    }
}
