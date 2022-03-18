using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomState { WAITING , PLAYING, COMPLETE, CLOSED }
public class RoomController : MonoBehaviour
{
    public float timeElapsed = 0.0f;
    public GameObject exitDoor;
    public RoomState roomState;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(roomState)
        {
            case RoomState.PLAYING:
                timeElapsed += Time.deltaTime;
                break;

            case RoomState.COMPLETE:
                exitDoor.SetActive(false);
                break;

            case RoomState.CLOSED:
                exitDoor.SetActive(true);
                break;
        }
    }
}
