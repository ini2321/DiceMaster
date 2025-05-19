using BepInEx;
using System.IO;
using System.Reflection;
using UnityEngine;

#nullable disable
namespace DiceMaster;

public static class AssetBundleHelper
{
  public static bool LoadAsset<T>(string assetName, string bundlePath, out T asset, bool useFile = false) where T : Object
  {
    AssetBundle assetBundle = useFile ? AssetBundleHelper.LoadAssetBundleFromFile(bundlePath) : AssetBundleHelper.LoadAssetBundleFromEmbedded(bundlePath);
        asset = assetBundle != null ? assetBundle.LoadAsset<T>(assetName) : default(T);
        return asset != null; // UnityEngine.Object의 != 연산자 오버로드가 사용됨
    }

  private static AssetBundle LoadAssetBundleFromFile(string bundlePath)
  {
    string path = Paths.PluginPath + bundlePath;
    return !File.Exists(path) ? (AssetBundle) null : AssetBundle.LoadFromFile(path);
  }

  private static AssetBundle LoadAssetBundleFromEmbedded(string bundlePath)
  {
    Assembly executingAssembly = Assembly.GetExecutingAssembly();
    using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.{bundlePath}"))
      return manifestResourceStream != null ? AssetBundle.LoadFromStream(manifestResourceStream) : (AssetBundle) null;
  }
}
