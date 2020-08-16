using UnityEngine;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Wowsome {
  /// <summary>
  /// For ios, it adds the StoreKit.framework when building
  /// </summary>
  public class WowStorePostBuildProcessor : MonoBehaviour {
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string buildPath) {
      if (target == BuildTarget.iOS) {
        PBXProject project = new PBXProject();
        string pbxFilename = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
        project.ReadFromFile(pbxFilename);

#if UNITY_2019_3_OR_NEWER
        string targetId = project.GetUnityFrameworkTargetGuid();
#else
        string targetId = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        string guid = project.TargetGuidByName(targetId);
        project.AddFrameworkToProject(guid, "StoreKit.framework", false);

        project.WriteToFile(pbxFilename);
      }
    }
  }
}

