using UnityEngine;

public class TimeSkip : MonoBehaviour
{
    [SerializeField] private float _timeScale = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            _timeScale -= 0.25f;
        if (Input.GetKeyDown(KeyCode.RightBracket))
            _timeScale += 0.25f;

        Time.timeScale = _timeScale;
    }
}