using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour
{
    [SerializeField]
    private GameObject _speechBubble;
    [SerializeField]
    private TextMeshProUGUI _speechBubbleText;
    [SerializeField]
    private GameObject _follower;
    //get set for the _follower
    public GameObject Follower
    {
        get { return _follower; }
        set { _follower = value; }
    }
    [SerializeField]
    private GameObject[] _reflectables;

    private float _offsetY = 6f;
    private float _offsetX = 0.16f;
    private float _defaultScale = 0.01f;
    private Camera _camera;
    private bool _isThinking = false;
    private bool _endThinking = false;
    private float _thinkingTimer = 0.2f;
    private float _timer = 0;
    private int _thinkingTimes = 3;

    private void Start()
    {
        _camera = Camera.main;
        Hide();
        foreach(GameObject thinker in _reflectables)
        {
            thinker.SetActive(false);
        }
        
    }

    //Set and Get Methods
    public void StartThinking()
    {
        _isThinking = true;
    }
    public void EndThinking()
    {
        _endThinking = true;
    }
    public void SetText(string text)
    {
        _speechBubbleText.text = text;
    }
    public void SetTextSize(float size)
    {
        _speechBubbleText.fontSize = size;
    }
    public void Show()
    {
        _speechBubble.gameObject.SetActive(true);
    }
    public void Hide()
    {
        _speechBubble.gameObject.SetActive(false);
    }
    public void setOffsetY(float f)
    {
        _offsetY = f;
    }


    //Other Methods
    private void LateUpdate() //LateUpdate is called after Update each frame, used for camera follow
    {
        this.transform.position = _follower.transform.position + new Vector3(_offsetX, _offsetY, 0);
        //We update the rotation of the speech bubble to face the camera, but only rotate on the y axis
        Quaternion lookaAtCamera = Quaternion.LookRotation(_camera.transform.position - this.transform.position);
        this.transform.rotation = Quaternion.Euler(0, lookaAtCamera.eulerAngles.y + 180, 0);
    }

    public void resize(Vector3 size)
    {
        this.transform.localScale = size;
        if(size == new Vector3(0,0,0) || size.x < 0 || size.y < 0 || size.z < 0)
        {
            this.transform.localScale = new Vector3(_defaultScale, _defaultScale, _defaultScale);
        }
    }

    private void Update()
    {
        if (_isThinking)
        {
            _timer -= Time.deltaTime;
            if (_timer < 0)
            {
                _timer = _thinkingTimer;
                //ThinkingTimes is modulo 4
                _thinkingTimes = (_thinkingTimes + 1) % 4;
                if(_thinkingTimes == 3)
                {
                    foreach(GameObject thinker in _reflectables)
                    {
                        thinker.SetActive(false);
                    }
                } else
                {
                    _reflectables[_thinkingTimes].SetActive(true);
                }
                
            }
            if (_endThinking)
            {
                _endThinking = false;
                _isThinking = false;
                _thinkingTimes = 3;
                foreach (GameObject thinker in _reflectables)
                {
                    thinker.SetActive(false);
                }
                _timer = _thinkingTimer;
            }
        } 
    }
}
