using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    private Board _board;
    private bool _isMoving;

    public InterpolationType interpolation = InterpolationType.SmootherStep;
    public enum InterpolationType
    {
        Linear,
        EaseOut,
        EaseIn,
        SmoothStep,
        SmootherStep
    }
    
    private void Start()
    {
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move((int) transform.position.x + 2, (int) transform.position.y, 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move((int) transform.position.x - 2, (int) transform.position.y, 0.5f);
        }
    }

    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }
    
    public void Init(Board board)
    {
        _board = board;
    }

    public void Move(int destX, int destY, float timeToMove)
    {
        if (!_isMoving)
        {
            StartCoroutine(MoveRoutine(new Vector3(destX, destY, 0), timeToMove));
        }
    }

    IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = transform.position;
        
        float elapsedTime = 0f;
        _isMoving = true;

        while (true)
        {
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                transform.position = destination;
                SetCoord((int) destination.x, (int) destination.y);
                break;
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);

            switch (interpolation)
            {
                case InterpolationType.Linear:
                    break;
                case InterpolationType.EaseOut:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);
                    break;
                case InterpolationType.EaseIn:
                    t = 1f - (float) Math.Cos(t * Mathf.PI * 0.5f);
                    break;
                case InterpolationType.SmoothStep:
                    t = t * t * (3 - 2 * t);
                    break;
                case InterpolationType.SmootherStep:
                    t = t * t * t * (t * (t * 6 - 15) + 10);
                    break;
            }

            transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null;
        }

        _isMoving = false;
    }
}