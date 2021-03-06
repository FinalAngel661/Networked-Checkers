﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { set; get; }

    public GameObject Menu;
    public GameObject serverMenu;
    public GameObject connectMenu;

    public GameObject serverPrefab;
    public GameObject clientPrefab;

    public InputField username;

    private void Start()
    {
        Instance = this;
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    public void JoinButton()
    {
        Menu.SetActive(false);
        connectMenu.SetActive(true);
    }

    public void HostButton()
    {
        try
        {
            Server s = Instantiate(serverPrefab).GetComponent<Server>();
            s.Init();

            Client c = Instantiate(clientPrefab).GetComponent<Client>();
            c.clientName = username.text;
            c.isHost = true;
            if (c.clientName == "")
            {
                c.clientName = "Host";
            }
            c.ConnectToServer("127.0.0.1", 1234);

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        Menu.SetActive(false);
        serverMenu.SetActive(true);
    }

    public void ConnectToServerButton()
    {
        string hostAddress = GameObject.Find("HostInput").GetComponent<InputField>().text;
        if (hostAddress == "")
        {
            hostAddress = "127.0.0.1";
        }

        try
        {
            Client c = Instantiate(clientPrefab).GetComponent<Client>();
            c.clientName = username.text;
            if (c.clientName == "")
            {
                c.clientName = "Client";
            }

            c.ConnectToServer(hostAddress, 1234);
            connectMenu.SetActive(false);
        }
        catch(Exception e)
        {
            Debug.Log("Client error : " + e.Message);
        }

    }
    public void BackButton()
    {
        Menu.SetActive(true);
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);

        Server s = FindObjectOfType<Server>();
        if (s != null)
            Destroy(s.gameObject);

        Client c = FindObjectOfType<Client>();
        if (c != null)
            Destroy(c.gameObject);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Checkers");
    }
}
