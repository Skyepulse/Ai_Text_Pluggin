using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Python_CScript : MonoBehaviour
{
    private TcpListener server;
    private TcpClient client;
    private Thread serverThread;
    private int port = 8052;
    private string localHost = "127.0.0.1";
    private bool isRunning = false;

    public static Python_CScript instance { get; private set; }

    void Start()
    {
        isRunning = true;
        serverThread = new(new ThreadStart(ListenForIncommingRequests))
        {
            IsBackground = true
        };
        serverThread.Start();
    }

    // Singleton pattern
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            SendDataToClient("Hello grandad how are you? I've been meaning to ask you what your role was during the second world war.", "Hello grandson. I had many friends that fell to the bullet hell of the germans. I am afraid of what AI will do to our world next, if only you understood that feeling.");
        }
        else if(Input.GetKeyDown(KeyCode.K))
        {
            SendDataToClient("Can you explain the process of photosynthesis and its importance in the ecosystem?", "Photosynthesis is the biological process by which green plants and other organisms convert light energy into chemical energy in the form of glucose. This process occurs in the chloroplasts of plant cells and involves the absorption of sunlight, carbon dioxide from the atmosphere, and water from the soil to produce glucose and oxygen as byproducts. Photosynthesis is crucial for the ecosystem as it serves as the primary source of energy for most living organisms. It releases oxygen into the atmosphere, which is essential for aerobic respiration in animals and other organisms. Additionally, photosynthesis plays a vital role in regulating the Earth's climate by removing carbon dioxide from the atmosphere, thus mitigating the greenhouse effect and global warming. Overall, photosynthesis is fundamental to sustaining life on Earth and maintaining the balance of the ecosystem.");
        }
        
    }

    private void ListenForIncommingRequests()
    {
        try
        {
            server = new TcpListener(IPAddress.Parse(localHost), port);
            server.Start();
            Debug.Log("Server is listening on port " + port);
            Byte[] bytes = new Byte[1024];
            while(isRunning)
            {
                if(server.Pending())
                {
                    client = server.AcceptTcpClient();
                    using(NetworkStream stream = client.GetStream())
                    {
                        // Handshake - Wait for initial message from client
                        int length = stream.Read(bytes, 0, bytes.Length);
                        var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        string clientMessage = Encoding.UTF8.GetString(incomingData);
                        Debug.Log("Client says: " + clientMessage);

                        // Respond to handshake message
                        SendMessageToClient("Hello, client. Ready for data.");

                        try
                        {
                            // Inner loop for reading from the stream
                            while (isRunning && (length = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                incomingData = new byte[length];
                                Array.Copy(bytes, 0, incomingData, 0, length);
                                clientMessage = Encoding.UTF8.GetString(incomingData);
                                MainThreadDispatcher.ExecuteOnMainThread(() =>
                                {
                                    Debug.Log("client message: " + clientMessage);
                                });
                            }
                        }
                        catch (Exception ex) when (ex is ObjectDisposedException || ex is IOException)
                        {
                            Debug.Log("Stream closed or client disconnected.");
                            break; // Exit the loop if the stream is closed or an IO exception occurs
                        }
                    }
                } else
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
        finally
        {
            if(server != null)
            {
                server.Stop();
            }
        }
    }

    private void SendMessageToClient(string message)
    {
        if (client == null)
            return;
        try
        {
            message += "\n";
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
            Debug.Log("Sent: " + message);
        }
        catch (Exception e)
        {
            Debug.Log("Socket exception: " + e);
        }
    }

    public void SendDataToClient(string question, string answer)
    {
        string to_send = "Question: " + question + "Your answer: " + answer;
        SendMessageToClient(to_send);
    }

    private void OnApplicationQuit()
    {
        isRunning = false; // Signal the server loop to exit
        SendMessageToClient("exit"); // Send an exit message to client
        if (client != null)
        {
            client.Close(); // Close the client connection
        }
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Join(); // Wait for the server thread to finish
        }
    }
}