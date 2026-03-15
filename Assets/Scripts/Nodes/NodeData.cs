using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class NoteInfo
{
    public float time;       // 노트가 등장하는 시간
    public string type;      // 노트의 타입 (예: "tap", "hold", "slide" 등)
    public int laneIndex;
}

[Serializable]
public class ChartData
{
    public string musicTitle = "New Song";
    public string musicArtist = "Unknown Artist";
    public List<NoteInfo> notes = new List<NoteInfo>();
}