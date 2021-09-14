using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
public class Exit_script : MonoBehaviour
{
    public bool message_to_be_analyzed=false;

    public class orologio_request
    {
        public int battery { get; set; }
        public string version { get; set; }
        public orologio_request(int battery_, string version_)
        {
            battery = battery_;
            version = version_;

        }
    }
    public class orologio_response
    {
        public int paired { get; set; }
        public int parameter_update { get; set; }
        public int vibrator { get; set; }
        public orologio_response(int paired_, int parameter_update_,int vibrator_)
        {
            paired = paired_;
            parameter_update = parameter_update_;
            vibrator = vibrator_;
        }
    }

    public void Response_from_server(string return_string)
    {
        message_to_be_analyzed = true;
        JObject json = JObject.Parse(return_string);

        Debug.Log(json);
        foreach (var e in json)
        {
            Debug.Log(e);
        }

        Debug.Log("Checking single values: paired:" + json["paired"]);


    }
    public void exitgame()
    {
        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-edge." + site_domain + "/api";
        string uri = edge_url + "/v1/device_heartbeat/" + chiave_orologio;



        orologio_request orologio_obj = new orologio_request(60,"federico-sim");

        string bodyJsonString = JsonConvert.SerializeObject(orologio_obj);
        //Debug.Log("Json:"+bodyJsonString);

        //PostData(uri, bodyJsonString);


        StartCoroutine(PostData_Coroutine(uri, bodyJsonString,(value)=> { Response_from_server(value);  })); 
        

    }
    //void PostData(string url, string bodyJsonString) => StartCoroutine(PostData_Coroutine(url, bodyJsonString));
    
    IEnumerator PostData_Coroutine(string url, string bodyJsonString, System.Action<string> callback)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Status Code: " + request.responseCode);
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error: " + request.error);
        }
        callback(request.downloadHandler.text);

    }

}
