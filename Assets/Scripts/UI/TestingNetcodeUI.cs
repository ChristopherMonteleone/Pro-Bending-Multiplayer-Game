using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;


    private void Awake() {
        startHostButton.onClick.AddListener(() => {
            Debug.Log("HOST");
            NetworkManager.Singleton.StartHost();
            Hide();
        });
        startHostButton.onClick.AddListener(() => {
            Debug.Log("HOST");
            NetworkManager.Singleton.StartClient();
            Hide();
        });
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
