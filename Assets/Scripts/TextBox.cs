using System;
using UnityEngine;

public class TextBox
{
    public TextBox(GUIStyle style)
    {
        _style = style;
    }
    
    public void displayMessage(String msg, int x, int y)
    {
        String[] msgList = msg.Split('\n');
        int maxLength = 0;
        foreach (String line in msgList)
        {
            maxLength = Math.Max(maxLength, line.Length);
        }
        int count = msgList.Length;
        GUILayout.BeginArea(new Rect(x, y, maxLength*_style.fontSize/2, (count+1.2f)*_style.fontSize*1.0f), _style);  
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(msg);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
    
    GUIStyle _style = null;
}
