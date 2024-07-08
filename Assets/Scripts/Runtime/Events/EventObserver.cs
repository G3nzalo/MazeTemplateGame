using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface EventObserver
{
    void Process(EventData eventData);
}
