using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HandwritingLevelConfig", menuName = "ScriptableObjects/HandwritingLevelConfig", order = 1)]
public class HandwritingLevelConfig : ScriptableObject
{
    public List<string> targetWords;
}