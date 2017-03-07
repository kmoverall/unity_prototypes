using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Reflection;


namespace MonkeyMind.DebugTools
{
    public class DisplayVariable : MonoBehaviour
    {

        [Serializable]
        public struct VariableWatch
        {
            public GameObject watchedObject;
            [HideInInspector]
            public Component component;
            public string watchedComponent;
            public string watchedVariable;
        }

        private Text debugText;
        public VariableWatch[] variablesToWatch;

        void Start()
        {
            debugText = GetComponent<Text>();
            //Retrieve all of the components from the objects set to watch
            for (int i = 0; i < variablesToWatch.Length; i++)
            {
                if (variablesToWatch[i].watchedObject != null)
                {
                    variablesToWatch[i].component = variablesToWatch[i].watchedObject.GetComponent(variablesToWatch[i].watchedComponent);
                }
            }
        }

        void Update()
        {
            debugText.text = "";
            foreach (VariableWatch var in variablesToWatch)
            {
                debugText.text += "> ";

                if (var.watchedObject != null)
                {
                    debugText.text += var.watchedObject.name + "." + var.watchedComponent;

                    if (var.component != null)
                    {
                        debugText.text += "." + var.watchedVariable;

                        //Attempt to find the variable as a field
                        FieldInfo field = var.component.GetType().GetField(var.watchedVariable, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            if (field.GetValue(var.component) != null)
                            {
                                debugText.text += " : " + field.GetValue(var.component).ToString() + "\n";
                            }
                            else {
                                debugText.text += " : null\n";
                            }
                        }
                        else {
                            //If it's not a field, check if it's a property instead
                            PropertyInfo prop = var.component.GetType().GetProperty(var.watchedVariable, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (prop != null)
                            {
                                if (prop.GetValue(var.component, null) != null)
                                {
                                    debugText.text += " : " + prop.GetValue(var.component, null).ToString() + "\n";
                                }
                                else {
                                    debugText.text += " : null\n";
                                }
                            }
                            else {
                                debugText.text += " : Variable Not Found\n";
                            }
                        }
                    }

                    else {
                        debugText.text += " : Component Not Found\n";
                    }
                }

                else {
                    debugText.text += "GameObject Not Found\n";
                }
            }
        }
    }
}
