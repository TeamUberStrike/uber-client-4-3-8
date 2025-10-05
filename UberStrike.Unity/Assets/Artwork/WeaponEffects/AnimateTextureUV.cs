using UnityEngine;
using System.Collections;

public class AnimateTextureUV : MonoBehaviour
{
    public int uvAnimationTileX = 1;
    public int uvAnimationTileY = 1;
    public int framesPerSecond = 10;

    void Update()
    {
     // Calculate index
    int index = Mathf.RoundToInt(Time.time * framesPerSecond);
    
    // repeat when exhausting all frames
    index = index % (uvAnimationTileX * uvAnimationTileY);
    
    // Size of every tile
    Vector2 size = new Vector2 (1.0f / uvAnimationTileX, 1.0f / uvAnimationTileY);
    
    // split into horizontal and vertical index
    int uIndex = index % uvAnimationTileX;
    int vIndex = index / uvAnimationTileX;

    // build offset
    // v coordinate is the bottom of the image in opengl so we need to invert.
    Vector2 offset = new Vector2 (uIndex * size.x, 1.0f - size.y - vIndex * size.y);
    
    renderer.material.SetTextureOffset ("_MainTex", offset);
    renderer.material.SetTextureScale ("_MainTex", size);
    }
}
