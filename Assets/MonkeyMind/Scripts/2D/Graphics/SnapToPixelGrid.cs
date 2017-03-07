using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapToPixelGrid : MonoBehaviour {

    public SpriteRenderer spriteRender;

    float pixelSize;

	// Use this for initialization
	void Start () {
        if (spriteRender == null) {
            spriteRender = GetComponent<SpriteRenderer>();
        }

        if (spriteRender == null)
        {
            Debug.Log("Not Sprite Renderer Found");
            return;
        }

        pixelSize = 1 / spriteRender.sprite.pixelsPerUnit;
    }
	

	void LateUpdate() {
        if (spriteRender == null)
            return;

        Vector3 tmpPos = transform.position;

        tmpPos /= pixelSize;
        tmpPos.x = Mathf.Round(tmpPos.x);
        tmpPos.y = Mathf.Round(tmpPos.y);
        tmpPos.z = Mathf.Round(tmpPos.z);
        tmpPos *= pixelSize;

        transform.position = tmpPos;
    }
}
