using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LightmapAnalysis;

//Lightning Setting's 'Resolution' and 'Padding' values ​​must be the same when importing and exporting.
public class Example_Controller : MonoBehaviour
{
    LightmapAnalyzer lightmapAnalyzer;
    [SerializeField] private Transform testChangeTrans;//Test Example_ChangeLightmap()
    [SerializeField] private string testChangeKey;//Test Example_ChangeLightmap()
    void Start()
    {
        //You can change this path. The default path is "LightmapJsonDataFolder" in the project folder.
        Debug.Log($"Default Path : {LocalFilePath.defaultPath}");
        lightmapAnalyzer = LightmapAnalyzer.DefaultInstance;
    }
    public void Example_Export()
    {
        lightmapAnalyzer.Export();//Write“FolderName/FileName" in <PathToBeSaved>.paths
    }
    public void Example_Import()
    {
        //When importing only local files
        //Write“FolderName/FileName" in <PathToBeLoaded>.paths
        lightmapAnalyzer.Import();
    }
    public void Example_ChangeLightmap()
    {
        lightmapAnalyzer.ChangeLightmap(testChangeTrans, testChangeKey);
    }
    public void Example_ImportWeb()
    {
        //When importing web files(or web + local)
        //All json data imported from outside must be registered through the "SetDataDictionary" method.
        string keyName = "Set Your Web Json Name";
        string json = "Your Web <BakedLightmapData> Data";// == Your Exported Json Data
        lightmapAnalyzer.SetDataDictionary("Web:" + keyName, json);//Also, Write“Web:keyName" in <PathToBeLoaded>.paths

        lightmapAnalyzer.Import();
    }
}
