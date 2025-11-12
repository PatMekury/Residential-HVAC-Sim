using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

[InitializeOnLoad]
public static class InspectAudioTrigger
{
    static InspectAudioTrigger()
    {
        EditorApplication.delayCall += () =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var audioTriggerType = assembly.GetTypes().FirstOrDefault(t => t.Name == "AudioTrigger" && t.Namespace == "Oculus.Interaction");
                
                if (audioTriggerType != null)
                {
                    Debug.Log($"<color=cyan>=== AudioTrigger Class Info ===</color>");
                    Debug.Log($"Full Name: {audioTriggerType.FullName}");
                    Debug.Log($"Assembly: {assembly.GetName().Name}");
                    
                    Debug.Log($"\n<color=yellow>Public Methods:</color>");
                    var methods = audioTriggerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var method in methods)
                    {
                        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Debug.Log($"  - {method.ReturnType.Name} {method.Name}({parameters})");
                    }
                    
                    Debug.Log($"\n<color=yellow>Public Fields:</color>");
                    var fields = audioTriggerType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        Debug.Log($"  - {field.FieldType.Name} {field.Name}");
                    }
                    
                    Debug.Log($"\n<color=yellow>Public Properties:</color>");
                    var properties = audioTriggerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        Debug.Log($"  - {prop.PropertyType.Name} {prop.Name}");
                    }
                    
                    break;
                }
            }
        };
    }
}
