using UnityEngine.UI;
using UnityEngine;

public static partial class UIUtils
{
    public static void FitToTargetHeight(this Image imageElement, float targetHeight)
    {
        if (imageElement == null || imageElement.sprite == null)
            return;

        Rect rect = imageElement.sprite.rect;
        float aspectRatio = rect.width / rect.height;
        float newWidth = targetHeight * aspectRatio;

        imageElement.rectTransform.sizeDelta = new Vector2(newWidth, targetHeight);
    }
    
    public static void FitToTargetWidth(this Image image, float targetWidth)
    {
        if (image == null || image.sprite == null)
            return;

        Rect rect = image.sprite.rect;
        float aspectRatio = rect.height / rect.width; 
        float newHeight = targetWidth * aspectRatio;

        image.rectTransform.sizeDelta = new Vector2(targetWidth, newHeight);
    }

    public static void FitToSquareBase(this Image image, float baseSize = 100f)
    {
        if (image == null || image.sprite == null)
            return;

        Rect rect = image.sprite.rect;
        float aspectRatio = rect.width / rect.height;

        float width, height;
        if (aspectRatio >= 1f)
        {
            width = baseSize;
            height = baseSize / aspectRatio;
        }
        else
        {
            height = baseSize;
            width = baseSize * aspectRatio;
        }

        image.rectTransform.sizeDelta = new Vector2(width, height);
    }
    
    public static void SetSprite(this Image image, Sprite newSprite)
    {
        image.sprite = newSprite;
    }
}