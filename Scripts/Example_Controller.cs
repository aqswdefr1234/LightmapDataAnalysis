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
    void Start()
    {
        Debug.Log($"Default Path : {LocalFilePath.defaultPath}");
        lightmapAnalyzer = LightmapAnalyzer.DefaultInstance;
    }
    public void Example_Export()
    {
        lightmapAnalyzer.Export();
    }
    public void Example_Import()
    {
        lightmapAnalyzer.Import();
    }
    public void Example_ChangeLightmap()
    {
        //lightmapAnalyzer.ChangeLightmap(testChangeTrans, testChangeKey);
    }
    public void Example_ImportWeb()
    {
        string keyName = "Set Your Web Json Name";
        string json = "Your Web <BakedLightmapData> Data";
        lightmapAnalyzer.SetDataDictionary("Web:" + keyName, json);

        lightmapAnalyzer.Import();
    }
}
