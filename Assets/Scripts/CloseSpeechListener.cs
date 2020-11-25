using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class CloseSpeechListener : MonoBehaviour, IMixedRealitySpeechHandler
{
    void IMixedRealitySpeechHandler.OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        if (eventData.Command.Keyword == "Close")
        {
            Object.Destroy(this.gameObject);
        }
    }
}
