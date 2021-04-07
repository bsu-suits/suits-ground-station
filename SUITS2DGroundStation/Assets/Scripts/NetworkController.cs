using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.IO;
using System.Threading;
using UnityEngine;

//NetworkController.cs
//Written by Daniel Lambert for the NASA Suits 2019 project.
//This is prototype code, don't expect perfection. This is divided up and structured only enough to enable my own comprehension. 



public class NetworkController : MonoBehaviour
{
    public static NetworkController networkControllerSingleton = null;
    public int port = 32123;
    public int udpPort = 32124;
    public UdpClient myUDP = null;// new UdpClient(port);
    public float udpLastSendTime = 0;
    public bool udpBound = false;
    private bool skipUDP = false;
    public TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    public GameObject inMeshInstancePrefab = null;
    public GameObject textureProjectorPrefab = null;
    public Queue<HoloToolkit.Unity.SimpleMeshSerializer.MeshData> incomingMeshes = null;
    private Queue<imageProjectorData> incomingProjections = null;
    public Vector3 camv = new Vector3();
    public Quaternion camq = new Quaternion();
    private bool doReconnect = false;

    private struct imageProjectorData
    {
        public byte[] imageData;
        public Vector3 projectionPos;
        public Quaternion projectionRot;
    }

    public NetworkController()
    {
        if (networkControllerSingleton != null)
            Debug.LogError("Singleton already created, tried to make a second one. Bad!");
        if (networkControllerSingleton == null)
        {
            networkControllerSingleton = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        incomingMeshes = new Queue<HoloToolkit.Unity.SimpleMeshSerializer.MeshData>();
        incomingProjections = new Queue<imageProjectorData>();
        myUDP = new UdpClient();
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        try
        {

            myUDP.EnableBroadcast = true;
            udpBound = true;
            byte[] myIP = System.Text.Encoding.UTF8.GetBytes(GetLocalIPAddress());
            myUDP.Send(myIP, myIP.Length, "255.255.255.255", udpPort);
            udpLastSendTime = Time.realtimeSinceStartup;
            Debug.Log("Broadcast " + GetLocalIPAddress() + " as target ip.");
        }
        catch (Exception e)
        {
            Debug.Log("" + e.Message);
        }
    }

    public static string GetLocalIPAddress()
    {
        
        var host = Dns.GetHostEntry(Dns.GetHostName());
        if (host.AddressList.Length < 1)
            return "ERROR";
        var lastip = host.AddressList[0];
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
            lastip = ip;
        }
        return lastip.ToString();
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private void FixedUpdate()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            SendTestLineRenderer();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            SendTestLineRendererUndo();
        }


        if(doReconnect)
        {
            doReconnect = false;
            tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        }

        if ((udpLastSendTime+10.0f < Time.realtimeSinceStartup)&&(udpBound)&&(!skipUDP))
        {   

            byte[] myIP = System.Text.Encoding.UTF8.GetBytes(GetLocalIPAddress());
            myUDP.Send(myIP, myIP.Length, "255.255.255.255", udpPort);
            udpLastSendTime = Time.realtimeSinceStartup;
            Debug.Log("Broadcast " + GetLocalIPAddress() + " as target ip.");
        }
        
        //handle incoming meshes
        if (incomingMeshes.Count > 0)
        {
            try
            {
                HoloToolkit.Unity.SimpleMeshSerializer.MeshData md = incomingMeshes.Dequeue();
                Mesh mesh = new Mesh();

                Debug.Log("Verices: " + md.vertices.Length);
                Debug.Log("Tringle Indices: " + md.triangleIndices.Length);

                mesh.vertices = md.vertices;
                mesh.triangles = md.triangleIndices;
                mesh.RecalculateNormals();
                GameObject go = Instantiate(networkControllerSingleton.inMeshInstancePrefab);
                Vector3 v = new Vector3(md.x1, md.y1, md.z1);
                Quaternion q = new Quaternion(md.x2, md.y2, md.z2, md.w2);
                go.transform.SetPositionAndRotation(v, q);
                go.GetComponent<InMeshInstance>().updateRenderedMesh(mesh);
            }
            catch (Exception e)
            {
                Debug.LogError("" + e.Message + "\n" + e.StackTrace);
            }
        }

        //handle incoming textures
       /* if(incomingProjections.Count>0)
        {
            try
            {
                imageProjectorData inc = incomingProjections.Dequeue();
                GameObject go = Instantiate(textureProjectorPrefab);
                Texture2D tex = new Texture2D(1280, 720, TextureFormat.PVRTC_RGBA4, false);
                tex.LoadImage(inc.imageData, false);
                go.GetComponent<Projector>().material.mainTexture = tex;
                go.transform.parent = this.gameObject.transform;
                go.transform.position = inc.projectionPos;
                go.transform.rotation = inc.projectionRot;
            }
            catch (Exception e)
            {
                Debug.LogError("" + e.Message + "\n" + e.StackTrace);
            }
        }*/
    }

    void OnDestroy()
    {        
        if(myUDP!=null)
        {
            myUDP.Close();
            myUDP.Dispose();
            myUDP = null;
        }

        if(connectedTcpClient!=null)
        {
            connectedTcpClient.Close();
            connectedTcpClient.Dispose();
            connectedTcpClient = null;
        }

        if(tcpListenerThread!=null)
        {
            tcpListenerThread.Abort();
            tcpListenerThread = null;
        }

        if (tcpListener!=null)
        {           
            tcpListener.Stop();
            tcpListener = null;
        }
    }

    //Monolithic, she said.
    private void ListenForIncommingRequests()
    {

        try
        {
            System.Net.IPAddress weee;
            System.Net.IPAddress.TryParse(GetLocalIPAddress(), out weee);
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            Debug.Log("Server is listening on "+ GetLocalIPAddress()+" "+port);

            while (true)
            {
                byte[] bytes = new Byte[1000000];
                byte[] buffer = null;
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    skipUDP = true;
                    Debug.Log("Connection. Restart system to allow a new client to connect.");
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        while (connectedTcpClient.Connected)
                        {
                            try
                            {
                                bytes = new Byte[1000000];
                                buffer = null;
                                byte[] sizeBytes = new byte[4];
                                stream.Read(sizeBytes, 0, sizeBytes.Length);
                                int size = System.BitConverter.ToInt32(sizeBytes, 0);
                                if (size < 4)
                                {
                                    Debug.LogError("IncomingSize is <4!!!!!!!!");
                                }
                                int remaining = size - 4;

                                if (remaining < 32 || remaining > 1000000)
                                {
                                    Debug.Log((remaining < 32) ? "<32" : ">1mil");
                                    stream.Read(bytes, 0, 1000000);//dump what remains and hope we realign.
                                    debugByteOut(bytes);
                                    continue;
                                }

                                // Read incomming stream into byte arrary.
                                while (remaining > 0)
                                {

                                    int length = 0;
                                    if ((length = stream.Read(bytes, 0, remaining)) != 0)
                                    {
                                        byte[] minBuf = new byte[length];
                                        if (buffer == null)
                                        {
                                            buffer = new byte[length];
                                            Array.Copy(bytes, 0, buffer, 0, length);
                                        }
                                        else
                                        {
                                            Array.Copy(bytes, 0, minBuf, 0, length);
                                            buffer = Combine(buffer, minBuf);
                                        }
                                    }
                                    remaining -= length;
                                }
                                var incommingData = new byte[buffer.Length];
                                Array.Copy(buffer, 0, incommingData, 0, buffer.Length);
                                Byte[] inBytes = incommingData;
                                Debug.Log("" + incommingData.Length + " Compressed bytes received.");
                                int packetType = System.BitConverter.ToInt32(inBytes, 0);

                                //Throw an error if we get a bad packet type.
                                switch(packetType)
                                {
                                    case (1):
                                    case (2):
                                    case (3):
                                    case (4):
                                    case (5):
                                        break;
                                    default:
                                        Debug.LogError("BAD PACKET TYPE ENCOUNTERED: " + packetType);
                                        debugByteOut(inBytes);
                                        continue;
                                }

                                float x1 = System.BitConverter.ToSingle(inBytes, 4);
                                float y1 = System.BitConverter.ToSingle(inBytes, 8);
                                float z1 = System.BitConverter.ToSingle(inBytes, 12);
                                float x2 = System.BitConverter.ToSingle(inBytes, 16);
                                float y2 = System.BitConverter.ToSingle(inBytes, 20);
                                float z2 = System.BitConverter.ToSingle(inBytes, 24);
                                float w2 = System.BitConverter.ToSingle(inBytes, 28);
                                byte[] subset = new byte[inBytes.Length - 32];
                                Array.Copy(inBytes, 32, subset, 0, inBytes.Length - 32);

                                if (packetType == 2)//headset location update
                                {
                                    Vector3 v = new Vector3(x1, y1, z1);
                                    Quaternion q = new Quaternion(x2, y2, z2, w2);
                                    camv = v;
                                    camq = q;
                                }

                                if (packetType==3)//camera image, create projector.
                                {
                                    Vector3 v = new Vector3(x1, y1, z1);
                                    Quaternion q = new Quaternion(x2, y2, z2, w2);
                                    camv = v;
                                    camq = q;
                                    //subset contains our image data. Pass it back to the main thread for conversion into a Texture2D
                                    imageProjectorData newProj = new imageProjectorData
                                    {
                                        imageData = subset,
                                        projectionPos = new Vector3(x1, y1, z1),
                                        projectionRot = new Quaternion(x2, y2, z2, w2)
                                    };
                                    incomingProjections.Enqueue(newProj);
                                }

                                if (packetType != 1)
                                    continue;

                                HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(subset);

                                Debug.Log(x1 + " " + x2 + " " + y1 + " " + y2 + " " + z1 + " " + z2 + " " + w2); 

                                mesh.x1 = x1;
                                mesh.x2 = x2;
                                mesh.y1 = y1;
                                mesh.y2 = y2;
                                mesh.z1 = z1;
                                mesh.z2 = z2;
                                mesh.w2 = w2;
                                
                                networkControllerSingleton.incomingMeshes.Enqueue(mesh);

                            } catch(Exception e)
                            {
                                Debug.LogError("Bug in Dans Code:"+e.Message + "\n" + e.StackTrace);
                                doReconnect = true;
                                return;
                                // reconnect here.
                            }
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.LogError("SocketException " + socketException.ToString());
        }
    }

    private void debugByteOut(byte[] bb)
    {
        string derp = "";
        for(int i = 0; i+4<bb.Length; i+=4)
        {
            derp+=System.BitConverter.ToInt32(bb, i)+" ";
        }
        derp += "\n";
        Debug.Log(derp);
    }


    public void SendLineRenderer(LineRenderer lr)
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            Vector3[] verts = new Vector3[lr.positionCount];
            lr.GetPositions(verts);

            byte[] outgoingVerts = new byte[verts.Length * 12];

            for(int i = 0; i < verts.Length; i++)
            {
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].x), 0, outgoingVerts, ((i * 12) + 0), 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].y), 0, outgoingVerts, ((i * 12) + 4), 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].z), 0, outgoingVerts, ((i * 12) + 8), 4);
            }

            byte[] bytes = new byte[4 + 12 + 20]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36 + (outgoingVerts.Length)), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(4), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.r), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.g), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.b), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.a), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.positionCount), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.startWidth), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.endWidth), 0, bytes, 32, 4);
            bytes = Combine(bytes, outgoingVerts);	
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
             
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("LineRenderDataSent of length "+ outgoingVerts.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public void SendTestLineRenderer()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            Vector3[] verts = { new Vector3(0, 0, 0), new Vector3(1, 1, 1), new Vector3(2, 2, 2), new Vector3(3, 3, 3) };
            byte[] outgoingVerts = new byte[verts.Length * 12];
            for (int i = 0; i < verts.Length; i++)
            {
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].x), 0, outgoingVerts, ((i * 12) + 0), 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].y), 0, outgoingVerts, ((i * 12) + 4), 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(verts[i].z), 0, outgoingVerts, ((i * 12) + 8), 4);
            }
            byte[] bytes = new byte[4 + 12 + 20];
            System.Buffer.BlockCopy(BitConverter.GetBytes(36 + (outgoingVerts.Length)), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(4), 0, bytes, 4, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(2.0f), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(3.0f), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(4.0f), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(verts.Length), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(5.0f), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(6.0f), 0, bytes, 32, 4);
            bytes = Combine(bytes, outgoingVerts);
            NetworkStream stream = connectedTcpClient.GetStream();

            if (stream.CanWrite)
            {

                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("LineRenderDataTestSent of length " + outgoingVerts.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public void SendTestLineRendererUndo()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            byte[] bytes = new byte[4 + 12 + 20];
            System.Buffer.BlockCopy(BitConverter.GetBytes(36), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(5), 0, bytes, 4, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(2.0f), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(3.0f), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(4.0f), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(0), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(5.0f), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(6.0f), 0, bytes, 32, 4);
            NetworkStream stream = connectedTcpClient.GetStream();

            if (stream.CanWrite)
            {
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("LineRenderDataTestUndoSent of length ");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public void SendLineUndoRenderer()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
           
            Vector3 location = new Vector3();
            Quaternion rotation = new Quaternion();
            byte[] bytes = new byte[4 + 12 + 20]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(5), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 32, 4);
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Undo Sent");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    //stolen useful code.
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }
}

//stale code lives here

/*
public static byte[] Compress(byte[] raw)
{
    using (MemoryStream memory = new MemoryStream())
    {
        using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
        {
            gzip.Write(raw, 0, raw.Length);
        }
        return memory.ToArray();
    }
}

static byte[] Decompress(byte[] gzip)
{
    using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
    {
        const int size = 4096;
        byte[] buffer = new byte[size];
        using (MemoryStream memory = new MemoryStream())
        {
            int count = 0;
            do
            {
                count = stream.Read(buffer, 0, size);
                if (count > 0)
                {
                    memory.Write(buffer, 0, count);
                }
            }
            while (count > 0);
            return memory.ToArray();
        }
    }
}
*/
