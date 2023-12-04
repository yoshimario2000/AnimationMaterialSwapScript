/*  Animation Material Swap Script © 2023 by Yoshark is licensed under CC BY-SA 4.0
 * 
 * Version 1.0 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
namespace yosharkTools
{
    public class AnimatorMaterialSwapTool : EditorWindow
    {
        // Varriables
        private Vector2 scrollPosition;
        private AnimatorController OriginalController;
        private AnimatorController OldOriginalController;
        private List<Material> newMaterialList = new List<Material>();
        private List<Material> materialList = new List<Material>();
        private List<AnimationClip> AnimsToRemake = new List<AnimationClip>();
        private string GenerationPath = "Generated/Anim_Swap";
        private string GenerationFolder = "New Folder";
        private string AnimatorSubfolder = "Animator";
        private string AnimationClipSubfolder = "AnimtionClips";
        private bool makeUnique = true;
        private bool subfolders = false;


        [MenuItem("Tools/Yoshark Tools/Animator Tool")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorMaterialSwapTool>("Anim Mat Swap");
        }


        // The Fuction that calls generates the new files and populates them with the new information
        private void GenerateNewAnims()
        {
            // Varriables
            int offsetCount = 0;
            bool useOffset = false;
            string path = "Assets";


            AnimatorController NewController;
            List<AnimationClip> NewAnimClips = new List<AnimationClip>();

            // Generate the paths to put the generated animations and controller into
            GeneratePaths(ref offsetCount, ref useOffset, ref path);

            // Making a Copy of the Animator to work on, and referencing it.
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(OriginalController), path + "/" + OriginalController.name + ".controller");
            NewController = (AnimatorController)AssetDatabase.LoadAssetAtPath(path + "/" + OriginalController.name + ".controller", typeof(AnimatorController));

            // Time to make a list that removes any null'd materials
            List<Material> culledMaterialList = new List<Material>();
            List<Material> culledNewMaterialList = new List<Material>();
            for (int t = 0; t < newMaterialList.Count; t++)
            {
                if (newMaterialList[t] != null)
                {
                    culledMaterialList.Add(materialList[t]);
                    culledNewMaterialList.Add(newMaterialList[t]);
                }
            }
            Debug.Log("Culled Material list at Count: " + culledNewMaterialList.Count.ToString() + "\nOriginal Material list at Count: " + materialList.Count.ToString());
            // newMaterialList;

            List<AnimationClip> NewAnimations = new List<AnimationClip>();
            // Construct the New Animation files and swap the Animations in them
            foreach (var animclip in AnimsToRemake)
            {
                bool remakeThisAnim = false;

                // Filters for the animations we're actually replacing
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animclip))
                {
                    // Get the keyframes of each animation
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(animclip, binding);
                    foreach (var key in keyframes)
                    {
                        if (culledMaterialList.Contains((Material)key.value))
                        {
                            remakeThisAnim = true;
                            int location = culledMaterialList.IndexOf((Material)key.value);
                        }
                    }
                }
                if (remakeThisAnim)
                {
                    string oldClipPath = AssetDatabase.GetAssetPath(animclip);
                    AssetDatabase.CopyAsset(oldClipPath, path + "/" + animclip.name + ".anim");

                    AnimationClip newClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(path + "/" + animclip.name + ".anim", typeof(AnimationClip));
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(newClip))
                    {
                        // I wish I could tell you I understand what I'm writing as I write this, but I dont. Sorry :D
                        // This here is where we're actually modifying the data of the animation

                        // Get the keyframes of each animation
                        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(animclip, binding);

                        // Construct the new curve
                        EditorCurveBinding materialCurve = EditorCurveBinding.PPtrCurve(binding.path, binding.type, binding.propertyName);
                        for (var key = 0; key < keyframes.Length; key++)
                        {
                            if (culledMaterialList.Contains((Material)keyframes[key].value))
                            {
                                int location = culledMaterialList.IndexOf((Material)keyframes[key].value);
                                ObjectReferenceKeyframe[] array = new ObjectReferenceKeyframe[1];

                                array[0] = default(ObjectReferenceKeyframe);
                                array[0].value = (Object)culledNewMaterialList[location];
                                array[0].time = keyframes[key].time;

                                AnimationUtility.SetObjectReferenceCurve(newClip, materialCurve, array);

                                //keyframes[key].value = (Object)culledNewMaterialList[location];
                            }
                        }
                    }
                    NewAnimations.Add(newClip);
                }
                else
                {
                    NewAnimations.Add(null);
                }
            }
            Debug.Log("The Count of NewAnimations is " + NewAnimations.Count +
                    "\nThe Count of AnimsToRemake is " + AnimsToRemake.Count +
                    "\nThese should be the same. Are they?"
                    + ((NewAnimations.Count == AnimsToRemake.Count) ? "yes" : "no"));


            // Finally, we take our new animations and replace the old animations within our new animator.
            foreach (var stateToModify in
            from AnimatorControllerLayer layer in NewController.layers
            from ChildAnimatorState animstate in layer.stateMachine.states
            let stateToModify = animstate.state
            select stateToModify)
            {
                for (int i = 0; i < AnimsToRemake.Count; i++)
                {
                    if (AnimsToRemake[i] == stateToModify.motion && NewAnimations[i] != null)
                    {
                        stateToModify.motion = NewAnimations[i];
                    }
                }
            }
        }

        // Code for generating the new folders for where to store 
        private void GeneratePaths(ref int offsetCount, ref bool useOffset, ref string path)
        {
            char[] folderSplit = new char[] { '/' };
            string[] folders = GenerationPath.Split(folderSplit);

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(path + "/" + folder))
                {
                    AssetDatabase.CreateFolder(path, folder);
                }
                path = path + "/" + folder;
            }
            if (!AssetDatabase.IsValidFolder(path + "/" + GenerationFolder))
            {
                AssetDatabase.CreateFolder(path, GenerationFolder);
                path = path + "/" + GenerationFolder;
            }
            else if (makeUnique)
            {
                useOffset = true;
                while (AssetDatabase.IsValidFolder(path + "/" + GenerationFolder + " " + offsetCount.ToString()) && useOffset)
                {
                    offsetCount++;
                }
                AssetDatabase.CreateFolder(path, GenerationFolder + " " + offsetCount.ToString());
                path = path + "/" + GenerationFolder + " " + offsetCount.ToString();
            }
            else
            {
                path = path + "/" + GenerationFolder;
            }
        }

        /* Main Rendering Function for the GUI
         */
        void OnGUI()
        {
            EditorGUILayout.LabelField("A tool for automating the tedious task \nof swapping materials on Animations", GUILayout.Height(30));
            OriginalController = EditorGUILayout.ObjectField("Source Animator", OriginalController, typeof(AnimatorController), false) as AnimatorController;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, (GUIStyle.none));// Scroll bar for the GUI
                                                                                              // apparently GUIStyle.none is needed to have it not show the horizontal bars
                                                                                              // samplingAnimation = EditorGUILayout.ObjectField("Animation Inspect Test", samplingAnimation, typeof(AnimationClip), false) as AnimationClip;
                                                                                              // If we have an animator to check, preform the anim mat check
            if (OriginalController != OldOriginalController && OriginalController != null)
            {
                newMaterialList = new List<Material>();
                materialList = new List<Material>();
                AnimsToRemake = new List<AnimationClip>();
                // Find all the animations in the animator
                foreach (var animclip in OriginalController.animationClips)
                {
                    // Get the Curves of the animation
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animclip))
                    {
                        // Get the keyframes of each animation
                        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(animclip, binding);
                        foreach (var key in keyframes)
                        {
                            // Get the Material in the Animation and see if we already have it
                            // If not, add it to our list
                            if (!materialList.Contains((Material)key.value))
                            {
                                materialList.Add((Material)key.value);
                                newMaterialList.Add(null);
                            }
                            if (!AnimsToRemake.Contains(animclip) && (key.value.GetType() == typeof(Material)))
                            {
                                AnimsToRemake.Add(animclip);
                            }
                        }
                    }
                }
                OldOriginalController = OriginalController;
            }
            else if (OriginalController == null)
            {
                newMaterialList = new List<Material>();
                materialList = new List<Material>();
                AnimsToRemake = new List<AnimationClip>();
                OldOriginalController = null;
            }
            if (GUILayout.Button("Generate Animator & Animations"))
            {
                GenerateNewAnims();
            }
            EditorGUILayout.Space();
            makeUnique = EditorGUILayout.Toggle("Make Filepath Unique?", makeUnique);
            GenerationPath = EditorGUILayout.TextField("Generation Path:", GenerationPath);
            GenerationFolder = EditorGUILayout.TextField("Folder:", GenerationFolder);
            EditorGUILayout.Space();
            /*subfolders = EditorGUILayout.Toggle("Use Subfolders?", subfolders);
            if (subfolders)
            {
                AnimatorSubfolder = EditorGUILayout.TextField("Animator Subfolder:", AnimatorSubfolder);
                AnimationClipSubfolder = EditorGUILayout.TextField("AnimationClip Subfolder:", AnimationClipSubfolder);
            }*/
            EditorGUILayout.Space();

            if (Foldout("AnimatorMaterialRemap_MaterialList", "Materials To Swap"))
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Material Count: ", materialList.Count.ToString());
                int usedMatCount = 0;
                for (int t = 0; t < newMaterialList.Count; t++)
                {
                    if (newMaterialList[t] != null)
                    {
                        usedMatCount++;
                    }
                }

                EditorGUILayout.LabelField("Material Swap Count: ", usedMatCount.ToString());


                for (int i = 0; i < materialList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(materialList[i], typeof(Material), false);
                    newMaterialList[i] = EditorGUILayout.ObjectField(newMaterialList[i], typeof(Material), false) as Material;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            if (Foldout("AnimatorMaterialRemap_AnimationList", "Source Animations"))
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Animations With Materials: ", AnimsToRemake.Count.ToString());
                if (OriginalController != null)
                {
                    EditorGUILayout.LabelField("Animator Clip Count: ", OriginalController.animationClips.Length.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField("Animator Clip Count: 0");
                }

                for (int i = 0; i < AnimsToRemake.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.ObjectField(AnimsToRemake[i], typeof(AnimationClip), false);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("Version 1.0 By Yoshark");
        }

        static bool Foldout(string editorPreferencesKey, string label, bool defaultState = false)
        {
            bool oldState = EditorPrefs.GetBool(editorPreferencesKey, defaultState);
            bool currentState = EditorGUILayout.Foldout(oldState, label);
            if (currentState != oldState)
                EditorPrefs.SetBool(editorPreferencesKey, currentState);
            return currentState;
        }
    }
}

