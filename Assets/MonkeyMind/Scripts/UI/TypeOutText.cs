using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MonkeyMind.UI
{
    public class TypeOutText : MonoBehaviour
    {

        Text textObject;
        string stringToPrint = "";
        float baseTypeSFXPitch = 0;

        public float typingSpeed = 30;
        public AudioClip typeSFX;
        public float typeSFXPitchVariance = 0.1f;
        [HideInInspector]
        public bool isFinished = false;

        void OnEnable()
        {
            PrepText();
        }

        public void PrepText()
        {
            textObject = GetComponent<Text>();
            textObject.text = "";
        }

        public void StartTyping(string textToType)
        {
            StartCoroutine(TypeText(textToType));
        }

        public void Interrupt()
        {
            StopCoroutine("TypeText");
            textObject.text = stringToPrint;
            isFinished = true;
        }

        public void Cancel()
        {
            StopCoroutine("TypeText");
            isFinished = true;
        }

        public IEnumerator TypeText(string textToType)
        {
            stringToPrint = textToType;
            isFinished = false;
            textObject.text = "";
            float typeProgress = 0;

            PlayTypeSound();

            while (typeProgress < stringToPrint.Length)
            {
                for (int i = Mathf.FloorToInt(typeProgress); i < Mathf.FloorToInt(typeProgress + Time.deltaTime * typingSpeed) && i < stringToPrint.Length; i++)
                {
                    textObject.text += stringToPrint[i];
                    if (stringToPrint[i] == ' ' || stringToPrint[i] == '\n')
                    {
                        PlayTypeSound();
                    }
                }
                typeProgress += Time.deltaTime * typingSpeed;
                yield return null;
            }

            isFinished = true;
        }

        public void PlayTypeSound()
        {
            if (GetComponent<AudioSource>() == null)
                return;

            if (baseTypeSFXPitch == 0)
                baseTypeSFXPitch = GetComponent<AudioSource>().pitch;

            if (GetComponent<AudioSource>().clip != typeSFX)
                GetComponent<AudioSource>().clip = typeSFX;

            GetComponent<AudioSource>().pitch = baseTypeSFXPitch + Random.Range(-1 * typeSFXPitchVariance, typeSFXPitchVariance);
            GetComponent<AudioSource>().Play();
        }
    }
}
