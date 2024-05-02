using System.Collections;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

public class GanasteCanvas : MonoBehaviour {
    public GameObject ganastePanel;
    public AudioClip ganasteClip;
    void Start()
    {
        QuantumEvent.Subscribe<EventGameEndEvent>(this, GameEnd);
        ganastePanel.SetActive(false);
    }

    private void GameEnd(EventGameEndEvent callback) {
        ganastePanel.SetActive(true);
        GetComponent<AudioSource>().PlayOneShot(ganasteClip);
    }
}
