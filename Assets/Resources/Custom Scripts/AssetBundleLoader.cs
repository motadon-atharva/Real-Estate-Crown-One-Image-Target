using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable] 
public class Models
{
    public Model[] body;
}

[Serializable] 
public class Model
{
    public string model_url;
    public string prefab_name;
    public string model_show_text;
}
public class ModelsParents
{
    public AssetBundle Bundle;
    public string Name;
    public Transform Parent;
    public string ModelShowText;
}

public class FinalObjects
{
    public GameObject Model;
    public Transform Parent;
    public string ModelShowText;
}
public class AssetBundleLoader : MonoBehaviour
{
    private string asset_url = "https://api.backendless.com/4BF8614F-72BC-4B4B-83B4-6B7EC005E492/FB5E05E2-E656-40A1-8D84-E424E3F78476/data/one_image_target?sortBy=model_show_text%20asc";
//    private string asset_url = "https://api.backendless.com/4BF8614F-72BC-4B4B-83B4-6B7EC005E492/FB5E05E2-E656-40A1-8D84-E424E3F78476/data/one_image_target_android";
    private Transform _parent;
    private GameObject _parentGameObject;

    public GameObject sliderPanel;
    public Slider slider;
    public Text informationBox;
    public GameObject buttonPanel;
    public List<FinalObjects> final_objects_list = new List<FinalObjects>();

    private List<ModelsParents> models_parents = new List<ModelsParents>();

    // Start is called before the first frame update
    void Start()
    {
        buttonPanel.SetActive(false);
        sliderPanel.SetActive(true);
        _parentGameObject = GameObject.Find("ControllerObject");
        if (_parentGameObject != null)
        {
            Debug.Log("Got Parent GameObject: " + _parentGameObject.name);
            _parent = _parentGameObject.transform;
        }

        StartCoroutine(RestClient.Instance.Get(asset_url));
    }
    
//    IEnumerator GetAssetBundle(string jsonResult)
//    {
//        Debug.Log(jsonResult);
//        Models jsonObject = JsonUtility.FromJson<Models>(jsonResult);
//        Debug.Log(jsonObject.body[0].model_url);
//        Debug.Log("Download Started...");
//
//        float maxProgress = jsonObject.body.Length;
//        float progress = 0;
//        sliderPanel.SetActive(true);
//        for (int i=0; i<jsonObject.body.Length; i++)
//        {
//            UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(jsonObject.body[i].model_url);
//            www.SendWebRequest();
//
//            if (www.isNetworkError || www.isHttpError)
//            {
//                Debug.Log(www.error);
//            }
//            else
//            {
//                //To remember the last progress
//                float lastProgress = progress;
//                informationBox.text = "Downloading resources...";
//                while (!www.isDone)
//                {
//                    //Calculate the current progress
//                    progress = lastProgress + www.downloadProgress;
//                    //Get a percentage
//                    float progressPercentage = (progress / maxProgress) * 100;
//                    Debug.Log("Downloaded: " + progressPercentage + "%");
//                    yield return new WaitForSeconds(0.1f);
//                    slider.value = Mathf.Clamp01(progress / maxProgress);
//                }
//                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
//                Debug.Log("Download Completed.");
//                
//                //Add the model and parent combination to list for later instantiation
//                ModelsParents modelsParents = new ModelsParents();
//                //Downloaded Asset Bundle
//                modelsParents.Bundle = bundle;
//                //Asset Bundle Name
//                modelsParents.Name = jsonObject.body[i].prefab_name;
//                //GameObject for the model to be placed in
//                modelsParents.Parent = _parent;
//                //Model Show Text
//                modelsParents.ModelShowText = jsonObject.body[i].model_show_text;
//                models_parents.Add(modelsParents);
//            }
//        }
//        
//        informationBox.text = "Setting up resources...";
//        yield return new WaitForSeconds(0.1f);
//        //Instantiate the models in their parents
//        foreach (var obj in models_parents)
//        {
//            AssetBundleRequest request = obj.Bundle.LoadAssetAsync(obj.Name);
//            yield return request;
//            GameObject gameObject = request.asset as GameObject;
//            
//            FinalObjects finalObjects = new FinalObjects();
//            finalObjects.Model = gameObject;
//            finalObjects.Parent = obj.Parent;
//            finalObjects.ModelShowText = obj.ModelShowText;
//            finalObjects.Model.SetActive(false);
//            final_objects_list.Add(finalObjects);
//            
//            Instantiate(finalObjects.Model, finalObjects.Parent);
//        }
//        
//        sliderPanel.SetActive(false);
//        buttonPanel.SetActive(true);
//        
//        //Prepare arugments to send
//        object[] args = new object[2];
//        args[0] = _parent;
//        args[1] = final_objects_list;
//        
//        _parent.SendMessage("populateModelsArray", args);
//        yield return null;
//    }

    IEnumerator GetAssetBundle(string jsonResult)
    {
        Debug.Log(jsonResult);
        Models jsonObject = JsonUtility.FromJson<Models>(jsonResult);
        Debug.Log(jsonObject.body[0].model_url);
        Debug.Log("Download Started...");

        // Wait for the Caching system to be ready
        while (!Caching.ready)
        {
            yield return null;
        }
        
        // if you want to always load from server, can clear cache first
//        Caching.CleanCache();

        float maxProgress = jsonObject.body.Length;
        float progress = 0;
        for (int i=0; i<jsonObject.body.Length; i++)
        {
            // get current bundle hash from server, random value added to avoid caching
            UnityWebRequest www = UnityWebRequest.Get(jsonObject.body[i].model_url + ".manifest?r=" + (Random.value * 9999999));
            Debug.Log("Loading manifest:" + jsonObject.body[i].model_url + ".manifest");
            
            // wait for load to finish
            yield return www.SendWebRequest();
            
            // if received error, exit
            if (www.isNetworkError == true)
            {
                Debug.LogError("www error: " + www.error);
                www.Dispose();
                www = null;
                yield break;
            }
            
            // create empty hash string
            Hash128 hashString = (default(Hash128));// new Hash128(0, 0, 0, 0);
            
            // check if received data contains 'ManifestFileVersion'
            if (www.downloadHandler.text.Contains("ManifestFileVersion"))
            {
                // extract hash string from the received data, TODO should add some error checking here
                var hashRow = www.downloadHandler.text.ToString().Split("\n".ToCharArray())[5];
                hashString = Hash128.Parse(hashRow.Split(':')[1].Trim());

                if (hashString.isValid == true)
                {
                    // we can check if there is cached version or not
                    if (Caching.IsVersionCached(jsonObject.body[i].model_url, hashString) == true)
                    {
                        Debug.Log("Bundle with this hash is already cached!");
                    } else
                    {
                        Debug.Log("No cached version founded for this hash..");
                    }
                } else
                {
                    // invalid loaded hash, just try loading latest bundle
                    Debug.LogError("Invalid hash:" + hashString);
                    yield break;
                }

            } else
            {
                Debug.LogError("Manifest doesn't contain string 'ManifestFileVersion': " + jsonObject.body[i].model_url + ".manifest");
                yield break;
            }

            
            // now download the actual bundle, with hashString parameter it uses cached version if available
            www = UnityWebRequestAssetBundle.GetAssetBundle(jsonObject.body[i].model_url + "?r=" + (Random.value * 9999999), hashString, 0);
            
            // wait for load to finish
            www.SendWebRequest();
            
            if (www.error != null)
            {
                Debug.LogError("www error: " + www.error);
                www.Dispose();
                www = null;
                yield break;
            }
            else
            {
                //To remember the last progress
                float lastProgress = progress;
                informationBox.text = "Downloading resources...";
                while (!www.isDone)
                {
                    //Calculate the current progress
                    progress = lastProgress + www.downloadProgress;
                    //Get a percentage
                    float progressPercentage = (progress / maxProgress) * 100;
                    Debug.Log("Downloaded: " + progressPercentage + "%");
                    yield return new WaitForSeconds(0.1f);
                    slider.value = Mathf.Clamp01(progress / maxProgress);
                }
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                Debug.Log("Download Completed.");
                
                //Add the model and parent combination to list for later instantiation
                ModelsParents modelsParents = new ModelsParents();
                //Downloaded Asset Bundle
                modelsParents.Bundle = bundle;
                //Asset Bundle Name
                modelsParents.Name = jsonObject.body[i].prefab_name;
                //GameObject for the model to be placed in
                modelsParents.Parent = _parent;
                //Model Show Text
                modelsParents.ModelShowText = jsonObject.body[i].model_show_text;
                models_parents.Add(modelsParents);
            }
        }
        
        informationBox.text = "Setting up resources...";
        yield return new WaitForSeconds(0.1f);
        //Instantiate the models in their parents
        foreach (var obj in models_parents)
        {
            AssetBundleRequest request = obj.Bundle.LoadAssetAsync(obj.Name);
            yield return request;
            GameObject gameObject = request.asset as GameObject;
            
            FinalObjects finalObjects = new FinalObjects();
            finalObjects.Model = gameObject;
            finalObjects.Parent = obj.Parent;
            finalObjects.ModelShowText = obj.ModelShowText;
            finalObjects.Model.SetActive(false);
            final_objects_list.Add(finalObjects);
            
            Instantiate(finalObjects.Model, finalObjects.Parent);
        }
        
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(true);
        
        //Prepare arugments to send
        object[] args = new object[2];
        args[0] = _parent;
        args[1] = final_objects_list;
        
        _parent.SendMessage("populateModelsArray", args);
        yield return null;
    }
}
