using UnityEngine;

public class VerticalOscillator : MonoBehaviour
{
    public float topPosition;
    public float bottomPosition;

    private void Update()
    {
        transform.position = GetNewPosition();
    }

    private float GetNewHeight()
    {
        var halfHeightRange = (topPosition - bottomPosition) * 0.5f;
        var midHeight = bottomPosition + halfHeightRange;
        var heightDelta = Mathf.Sin(Time.time) * halfHeightRange;

        return midHeight + heightDelta;
    }

    private Vector3 GetNewPosition()
    {
        return new Vector3(transform.position.x, GetNewHeight(), transform.position.z);
    }
}