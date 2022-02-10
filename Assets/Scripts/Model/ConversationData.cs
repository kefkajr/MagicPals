using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Create new conversation data")]
public class ConversationData : ScriptableObject 
{
	public List<SpeakerData> list;
}
