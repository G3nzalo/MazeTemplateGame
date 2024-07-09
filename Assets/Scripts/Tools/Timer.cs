using System.Collections;
using UnityEngine;
using System;
using TMPro;

namespace Maze.Tools
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timerTxt = null;
        [SerializeField] private TimesTypes _timeType;
        private bool _isOn = false;
        private IEnumerator _timerCoroutine = null;
        private float _currentTime = 0;
        public float CurrentTime => _currentTime;

        public void SetStaticInitialValue(float initialValue)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Ceiling(initialValue));
            UpdateTimeDisplay(timeSpan);
        }

        public void Pause()
        {
            _isOn = false;
        }
        public void Unpause()
        {
            _isOn = true;
        }

        public void StartTimer(float time = 0)
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = TimerCO(time);
            _isOn = true;
            StartCoroutine(_timerCoroutine);
        }

        public void ResetTimer(float time = 0)
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            time = 0;
        }

        public void Stop()
        {
            _isOn = false;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        protected IEnumerator TimerCO(float time = 0)
        {
            if (time == 0)
            {
                _currentTime = 0;
                while (true)
                {
                    if (_isOn)
                    {
                        TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTime);
                        UpdateTimeDisplay(timeSpan);
                        _currentTime += Time.deltaTime;
                    }
                    yield return null;
                }
            }
            else
            {
                _currentTime = time;
                while (_currentTime > 0)
                {
                    if (_isOn)
                    {
                        TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Ceiling(_currentTime));
                        UpdateTimeDisplay(timeSpan);
                        _currentTime += Time.deltaTime;
                    }
                    yield return null;
                }
                _currentTime = 0;
                _timerTxt.text = _currentTime.ToString();
            }
        }
        protected void UpdateTimeDisplay(TimeSpan timeSpan)
        {
            switch (_timeType)
            {
                case TimesTypes.Seconds:
                    _timerTxt.text = timeSpan.TotalSeconds.ToString();
                    break;
                case TimesTypes.Minutes:
                    var formattedTime = $"{timeSpan.Minutes:0}:{timeSpan.Seconds:00}";
                    _timerTxt.text = formattedTime;
                    break;
            }
        }
        enum TimesTypes
        {
            Seconds,
            Minutes,
        }
    }

}
