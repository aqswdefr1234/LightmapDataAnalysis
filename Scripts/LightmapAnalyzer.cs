namespace LightmapAnalysis
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using UnityEngine.Experimental.Rendering;

    public class LightmapAnalyzer
    {
        //Data Dictionary
        public Dictionary<string, BakedLightmapData> loadedDataDict = new Dictionary<string, BakedLightmapData>();
        public Dictionary<string, (int, int)> lightIndexDict = new Dictionary<string, (int, int)>();
        //Instance
        public static LightmapAnalyzer instance = null;
        public static LightmapAnalyzer DefaultInstance
        {
            get
            {
                if (instance == null)
                    instance = new LightmapAnalyzer();
                return instance;
            }
        }
        //private
        bool isTaskSuccess = false;
        List<Texture2D> colorList = new List<Texture2D>();
        List<Texture2D> dirList = new List<Texture2D>();

        //Export
        public void Export()
        {
            Transform[] transArray = FindPathToBeSaved();
            string[] pathArr = new string[transArray.Length];
            LightmapExporter exporter = new LightmapExporter();

            for (int i = 0; i < transArray.Length; i++)
            {
                pathArr[i] = transArray[i].GetComponent<PathToBeSaved>().path;
                if (pathArr[i] == "" || pathArr[i] == null)
                {
                    Debug.LogError($"The Path isn't set : {transArray[i].name}");
                    return;
                }
            }
            //Save data for all objects with <PathToBeSaved>
            for (int i = 0; i < transArray.Length; i++)
                exporter.LightmapDataSave(transArray[i], pathArr[i]);
        }
        private Transform[] FindPathToBeSaved()
        {
            PathToBeSaved[] foundScripts = MonoBehaviour.FindObjectsByType<PathToBeSaved>(FindObjectsSortMode.None);
            Transform[] foundTransforms = new Transform[foundScripts.Length];
            for (int i = 0; i < foundScripts.Length; i++)
            {
                foundTransforms[i] = foundScripts[i].transform;
            }
            return foundTransforms;
        }

        //Import
        public void Import()
        {
            isTaskSuccess = false;
            LightmapSettings.lightmaps = null;

            Transform[] transArr = FindPathToBeLoaded();
            string[] localPaths = SetLocalPaths(transArr);
            
            ReadLocalData(localPaths).ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Task Failed");
                    return;
                }
                isTaskSuccess = true;
            });
            CoroutineManager.Instance.StartCoroutines(ImportData());
            CoroutineManager.Instance.StartCoroutines(SetLightmap());
            CoroutineManager.Instance.StartCoroutines(SetMeshLightmapIndex(transArr));
        }
        private Transform[] FindPathToBeLoaded()
        {
            PathToBeLoaded[] foundScripts = MonoBehaviour.FindObjectsByType<PathToBeLoaded>(FindObjectsSortMode.None);
            Transform[] foundTransforms = new Transform[foundScripts.Length];
            for (int i = 0; i < foundScripts.Length; i++)
            {
                foundTransforms[i] = foundScripts[i].transform;
            }
            return foundTransforms;
        }
        private string[] SetLocalPaths(Transform[] transArray)
        {
            List<string> pathList = new List<string>();
            foreach (Transform trans in transArray)
            {
                pathList.AddRange(trans.GetComponent<PathToBeLoaded>().paths);
            }
            pathList = pathList.Distinct().ToList();
            return pathList.ToArray();
        }
        private async Task ReadLocalData(string[] paths)
        {
            if (paths.Length == 0)
                Debug.LogError("The local path is empty.");

            string json = "";
            string filePath = "";
            await Task.Run(() => {
                foreach (string path in paths)
                {
                    if (path.Substring(0, 4) == "Web:")
                        continue;

                    if(path == null || path == "")
                    {
                        Debug.LogError("The path is empty");
                        return;
                    }
                        
                    filePath = Path.Combine(LocalFilePath.defaultPath, path);
                    json = File.ReadAllText(filePath);
                    SetDataDictionary(path, json);
                }
            });
        }
        public void SetDataDictionary(string keyName, string json)//void??
        {
            loadedDataDict[keyName] = JsonUtility.FromJson<BakedLightmapData>(json);
        }
        IEnumerator ImportData()
        {
            while (isTaskSuccess == false)
            {
                yield return null;
            }

            LightmapImporter importer = new LightmapImporter();
            int startIndex = -1;
            int endIndex = -1;
            foreach (KeyValuePair<string, BakedLightmapData> pair in loadedDataDict)
            {
                importer.LightmapDataLoad(pair.Value);

                startIndex = colorList.Count;
                colorList.AddRange(importer.colorArr);
                dirList.AddRange(importer.dirArr);
                endIndex = colorList.Count - 1;

                lightIndexDict[pair.Key] = (startIndex, endIndex);
            }
            yield return null;
        }
        IEnumerator SetLightmap()
        {
            while (isTaskSuccess == false)
            {
                yield return null;
            }
            int count = colorList.Count;
            LightmapData[] lightmapDataArr = new LightmapData[count];

            for (int i = 0; i < count; i++)
            {
                LightmapData lightmapData = new LightmapData();
                lightmapData.lightmapColor = colorList[i];
                lightmapData.lightmapDir = dirList[i];
                lightmapDataArr[i] = lightmapData;
            }
            LightmapSettings.lightmaps = lightmapDataArr;
        }
        IEnumerator SetMeshLightmapIndex(Transform[] transArr)
        {
            while (isTaskSuccess == false)
            {
                yield return null;
            }
            foreach (Transform trans in transArr)
            {
                SetTransLightmap(trans, null);
            }
        }
        private void SetTransLightmap(Transform targetTransform, string key)
        {
            string keyName = key;
            if (keyName == null)
                keyName = targetTransform.GetComponent<PathToBeLoaded>().paths[0];
            List<MeshRenderer> meshList = new List<MeshRenderer>();
            
            //Parent
            meshList.Add(targetTransform.GetComponent<MeshRenderer>());
            //Child
            foreach (Transform childTrans in targetTransform)
            {
                meshList.Add(childTrans.GetComponent<MeshRenderer>());
            }

            (int start, int end) = lightIndexDict[keyName];//Search Index
            Vector4[] tilingArr = loadedDataDict[keyName]._tilingArray;
            int[] indexArr = loadedDataDict[keyName]._indexArray;
            for (int i = 0; i < meshList.Count; i++)
            {
                meshList[i].lightmapIndex = start + indexArr[i];
                meshList[i].lightmapScaleOffset = tilingArr[i];
            }
        }
        public void ChangeLightmap(Transform target, string keyName)
        {
            SetTransLightmap(target, keyName);
        }
    }
    public class LightmapExporter
    {
        public void LightmapDataSave(Transform targetTransform, string path)
        {
            string folderName = path.Split("/")[0];
            LocalFilePath.IsExistFolder(folderName);
            if (targetTransform == null)
            {
                Debug.LogError("The target is null");
                return;
            }
            if (path == "" || path == null)
            {
                Debug.LogError("The path is empty.");
                return;
            }
            MeshRenderer[] meshRendArray = TargetMeshRendererArray(targetTransform);
            BakedLightmapData lightmapData = MeshArrayAnalyze(meshRendArray);
            Task.Run(() => {
                JsonSave(lightmapData, path);
            });

        }
        private MeshRenderer[] TargetMeshRendererArray(Transform targetTransform)
        {
            List<MeshRenderer> meshRendList = new List<MeshRenderer>();
            meshRendList.Add(targetTransform.GetComponent<MeshRenderer>());
            foreach (Transform child in targetTransform)
            {
                meshRendList.Add(child.GetComponent<MeshRenderer>());
            }
            return meshRendList.ToArray();
        }
        private BakedLightmapData MeshArrayAnalyze(MeshRenderer[] meshRendArray)
        {
            try
            {
                List<int> usedIndex = new List<int>();
                List<int> meshIndex = new List<int>();
                List<Vector4> tiling = new List<Vector4>();
                foreach (MeshRenderer meshRend in meshRendArray)
                {
                    if (meshRend == null)
                    {
                        meshIndex.Add(-1);
                        tiling.Add(new Vector4(0, 0, 0, 0));
                        continue;
                    }
                    int index = meshRend.lightmapIndex;
                    if (!usedIndex.Contains(index))
                        usedIndex.Add(index);

                    meshIndex.Add(index);
                    tiling.Add(meshRend.lightmapScaleOffset);
                }

                int[] newMeshArray = RelocationIndex(usedIndex.ToArray(), meshIndex.ToArray());
                (string[] colorArray, string[] dirArray) = TextureToString(usedIndex.ToArray());
                Vector2Int[] sizeArray = TextureSize(usedIndex.ToArray());
                Vector4[] tilingArray = tiling.ToArray();
                return new BakedLightmapData(colorArray, dirArray, sizeArray, tilingArray, newMeshArray);

            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }

        }
        private int[] RelocationIndex(int[] usedIndex, int[] meshIndex)
        {
            int[] newMeshArray = new int[meshIndex.Length];
            System.Array.Fill(newMeshArray, -1);
            for (int i = 0; i < usedIndex.Length; i++)
            {
                int changedIndex = usedIndex[i];
                for (int j = 0; j < meshIndex.Length; j++)
                {
                    if (meshIndex[j] == changedIndex)
                        newMeshArray[j] = i;
                }
            }
            return newMeshArray;
        }
        private (string[], string[]) TextureToString(int[] lightmapIndexArr)
        {
            int count = lightmapIndexArr.Length;
            string[] colorArr = new string[count];
            string[] dirArr = new string[count];
            for (int i = 0; i < count; i++)
            {
                int index = lightmapIndexArr[i];
                colorArr[i] = ConvertTasks(LightmapSettings.lightmaps[index].lightmapColor, "EXR");
                dirArr[i] = ConvertTasks(LightmapSettings.lightmaps[index].lightmapDir, "PNG");
            }
            //내부에 동기 코드만 있을 경우 : Task.Run(() =>  , 내부에 비동기 코드가 있을 경우 : Task.Run(async () =>
            return (colorArr, dirArr);
        }

        private string ConvertTasks(Texture2D texture, string type)
        {
            if (type == "EXR")
            {
                Texture2D exrTexture = new Texture2D(texture.width, texture.height, TextureFormat.BC6H, false);
                exrTexture.LoadRawTextureData(texture.GetRawTextureData());
                exrTexture.Apply();
                byte[] exrBytes = exrTexture.GetRawTextureData();
                UnityEngine.MonoBehaviour.Destroy(exrTexture);
                return Convert.ToBase64String(exrBytes);
            }
            else if (type == "PNG")
            {
                Texture2D pngTexture = new Texture2D(texture.width, texture.height, GraphicsFormat.RGBA_BC7_UNorm, TextureCreationFlags.None);
                pngTexture.LoadRawTextureData(texture.GetRawTextureData());
                pngTexture.Apply();
                byte[] bytes = pngTexture.GetRawTextureData();
                UnityEngine.MonoBehaviour.Destroy(pngTexture);
                return Convert.ToBase64String(bytes);
            }
            return "";
        }
        private Vector2Int[] TextureSize(int[] lightmapIndexArr)
        {
            int count = lightmapIndexArr.Length;
            Vector2Int[] sizes = new Vector2Int[count];
            for (int i = 0; i < count; i++)
            {
                int index = lightmapIndexArr[i];
                sizes[i] = new Vector2Int(LightmapSettings.lightmaps[index].lightmapColor.width, LightmapSettings.lightmaps[index].lightmapColor.height);
            }
            return sizes;
        }
        private void JsonSave(BakedLightmapData data, string path)
        {
            string localPath = Path.Combine(LocalFilePath.defaultPath, path);
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(localPath, json);
            Debug.Log(localPath);
        }
    }

    public class LightmapImporter
    {
        public Texture2D[] colorArr;
        public Texture2D[] dirArr;
        public void LightmapDataLoad(BakedLightmapData data)
        {
            (colorArr, dirArr) = ToTexture(data);
        }
        private (Texture2D[], Texture2D[]) ToTexture(BakedLightmapData data)
        {
            int textureCount = data._colorArray.Length;
            Texture2D[] colorArr = new Texture2D[textureCount];
            Texture2D[] dirArr = new Texture2D[textureCount];
            ForLoopDecoding(data, colorArr, dirArr, textureCount);
            return (colorArr, dirArr);
        }
        private void ForLoopDecoding(BakedLightmapData data, Texture2D[] colorArr, Texture2D[] dirArr, int textureCount)
        {
            for (int i = 0; i < textureCount; i++)
            {
                colorArr[i] = Decoding(data._colorArray[i], data._sizeArray[i], "EXR");
                dirArr[i] = Decoding(data._dirArray[i], data._sizeArray[i], "PNG");
            }
        }
        private Texture2D Decoding(string json, Vector2Int size, string type)
        {
            int width = (int)size.x;
            int height = (int)size.y;
            byte[] bytes = Convert.FromBase64String(json);

            if (type == "EXR")
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.BC6H, false);
                tex.LoadRawTextureData(bytes);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                return tex;
            }
            else if (type == "PNG")
            {
                Texture2D tex = new Texture2D(width, height, GraphicsFormat.RGBA_BC7_UNorm, TextureCreationFlags.None);
                tex.LoadRawTextureData(bytes);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                return tex;
            }
            return null;
        }
        private MeshRenderer[] TargetMeshRendererArray(Transform targetTransform)
        {
            List<MeshRenderer> meshRendList = new List<MeshRenderer>();
            meshRendList.Add(targetTransform.GetComponent<MeshRenderer>());
            foreach (Transform child in targetTransform)
            {
                meshRendList.Add(child.GetComponent<MeshRenderer>());
            }
            return meshRendList.ToArray();
        }
    }

    [System.Serializable]
    public class BakedLightmapData
    {
        public string[] _colorArray;
        public string[] _dirArray;
        public Vector2Int[] _sizeArray;
        public Vector4[] _tilingArray;
        public int[] _indexArray;

        public BakedLightmapData(string[] colorArray, string[] dirArray, Vector2Int[] sizeArray, Vector4[] tilingArray, int[] indexArray)
        {
            _colorArray = colorArray;
            _dirArray = dirArray;
            _sizeArray = sizeArray;
            _tilingArray = tilingArray;
            _indexArray = indexArray;
        }
    }
    public class LocalFilePath
    {
        public static string defaultPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../")) + "LightmapJsonDataFolder/";
        public static string customFolderPath = "";
        public static bool IsExistFolder(string folderName)
        {
            bool exist = Directory.Exists(folderName);
            if (exist == false)
            {
                Directory.CreateDirectory(Path.Combine(defaultPath, folderName));
                return true;
            }
            return true;
        }
    }
    public class CoroutineManager : MonoBehaviour
    {
        public static CoroutineManager instance = null;
        public static CoroutineManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameObject("CoroutineManager").AddComponent<CoroutineManager>();
                return instance;
            }

        }

        public void StartCoroutines(IEnumerator method)
        {
            if (method != null)
                StartCoroutine(method);
        }
    }
}

