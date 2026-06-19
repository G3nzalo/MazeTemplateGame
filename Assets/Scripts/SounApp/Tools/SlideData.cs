using System;
using System.Collections.Generic;

[Serializable]
public class SlideData
{
    public int id;
    public string title;
    public string text;
    public float typingSpeed = 0.04f;
    public float pauseAfterPunctuation = 0.3f;
}

[Serializable]
public class SlidesRoot
{
    public List<SlideData> slides;
}
