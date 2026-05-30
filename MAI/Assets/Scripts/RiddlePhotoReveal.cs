using UnityEngine;

public static class RiddlePhotoReveal
{
    public static void Hide(SpriteRenderer photo)
    {
        if (photo == null)
            return;

        photo.gameObject.SetActive(false);
    }

    public static void Show(SpriteRenderer photo)
    {
        if (photo == null)
            return;

        photo.gameObject.SetActive(true);

        var color = photo.color;
        color.a = 1f;
        photo.color = color;
    }

    public static void HideAll(SpriteRenderer[] photos)
    {
        if (photos == null)
            return;

        foreach (var photo in photos)
            Hide(photo);
    }
}
