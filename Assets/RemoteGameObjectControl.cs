using UnityEngine;
using System.Collections;
using System.Text;
using System;

public class Logger
{
    public static void Log(string fmt, params System.Object[] values)
    {
        //Console.WriteLine(fmt, values);
        Debug.LogFormat(fmt, values);
    }


    public static void LogError(string fmt, params System.Object[] values)
    {
        //Console.WriteLine(fmt, values);
        Debug.LogErrorFormat(fmt, values);
    }
}

public class RemoteGameObjectControl : MonoBehaviour {
    
    public string HostAddress = "127.0.0.1";
    
    public enum Type
    {
        Server,
        Client
    }

    public Type type = Type.Server;

    static SocketMessager messager = new SocketMessager();

	// Use this for initialization
	void Start () {

        if(type == Type.Server)
        {
            messager.StartListen(HostAddress);
        }

        if(type == Type.Client)
        {
            messager.Connect(HostAddress);
        }	
	}
	
	// Update is called once per frame
	void Update () {

        SocketMessager.Msg[] msgs = messager.PopMessage();
        for(int i = 0; i < msgs.Length; i++)
        {
            if (msgs[i].cmd.Equals("upd"))
            {
                byte[] data = HierarchySerializer.Serialize();
                messager.Send("hie", data);
            }

            if(msgs[i].cmd.Equals("hie"))
            {
                HierarchySerializer.Deserialize(msgs[i].data);
            }
            
            if (msgs[i].cmd.Equals("act"))
            {
                GameObject go = HierarchySerializer.FindGameObject(msgs[i].data);
                if(go == null)
                {
                    //Logger.LogError("can not find game object {0}", path);
                }
                else
                {
                    go.SetActive(true);
                }
            }

            if (msgs[i].cmd.Equals("dac"))
            {
                GameObject go = HierarchySerializer.FindGameObject(msgs[i].data);
                if (go == null)
                {
                    //Logger.LogError("can not find game object");
                }
                else
                {
                    go.SetActive(false);
                }
            }

        }	
	}

    public static void RemoteActive(GameObject go, bool active)
    {
        byte[] data = HierarchySerializer.BuildGameObjectId(go);
        if(active)
        {
            messager.Send("act", data);
        }
        else
        {
            messager.Send("dac", data);
        }
    }

    void OnGUI()
    {
        if(type == Type.Client)
        {
            return;
        }

        GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Height(100), GUILayout.Width(120) };
        GUILayout.BeginVertical();

        if (GUILayout.Button("update", options))
        {
            messager.Send("upd");
        }
        
        GUILayout.EndVertical();
    }

    void OnDestroy()
    {
        Logger.Log("socket destroy");
        messager.Dispose();
        messager = null;

        GC.Collect();
    }

}
