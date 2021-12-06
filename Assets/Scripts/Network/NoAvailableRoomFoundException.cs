using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoAvailableRoomFoundException : Exception
{

    public NoAvailableRoomFoundException()
    {
        
    }

    public NoAvailableRoomFoundException(string message) : base(message)
    {
        
    }

    public NoAvailableRoomFoundException(string message, Exception inner) : base(message, inner)
    {
        
    }
}
