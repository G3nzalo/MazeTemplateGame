using System.Collections;
using TMPro;
using UnityEngine;

namespace Maze.Tools
{
    public class TypeWriter : MonoBehaviour
    {
        [SerializeField] private float _setTimeToNext;
        [SerializeField] private float _tymeSpeed;
        [SerializeField] private TMP_Text _tmp;
        [SerializeField] private string _text;

        private void Start()
        {
            StartCoroutine(ShowText(_tmp, _text));
        }

        public IEnumerator SetShowText(TMP_Text tmp, string text) => ShowText(tmp, text);

        private IEnumerator ShowText(TMP_Text tmp, string text)
        {
            int totalVisibleCharacters = text.Length;
            int counter = 0;
            tmp.text = text;

            while (counter <= totalVisibleCharacters)
            {
                int visibleCount = counter % (totalVisibleCharacters + 1);
                tmp.maxVisibleCharacters = visibleCount;


                if (visibleCount >= totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(_setTimeToNext);
                }

                counter++;

                yield return new WaitForSeconds(_tymeSpeed);
            }
        }
    }
}

