using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeyMind.TwoD
{
    public class SwapSpriteSheet : MonoBehaviour
    {
        public string alternateSheetName;
        void LateUpdate()
        {
            Sprite[] newSprites = Resources.LoadAll<Sprite>(alternateSheetName);
            SpriteRenderer render = GetComponent<SpriteRenderer>();
            Sprite newSprite = Array.Find(newSprites, item => item.name == render.sprite.name);
            if (newSprite)
            {
                render.sprite = newSprite;
            }
        }
    }
}
