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

    private float _offsetY = 1.2f;
    private float _offsetX = 0.01f;
    private float _defaultScale = 0.01f;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    //Set and Get Methods
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
        this.gameObject.SetActive(true);
    }
    public void Hide()
    {
        this.gameObject.SetActive(false);
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
}
