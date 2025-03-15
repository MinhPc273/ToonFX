using UnityEngine;

namespace JunEngine
{
    public class Destructor : MonoBehaviour
    {
        [SerializeField] private float TimeDestructor;
        [SerializeField] private bool _isDestroy;
        [SerializeField] private bool _isPooler;

        private float _timeDestructor;

        private void OnEnable()
        {
            _timeDestructor = TimeDestructor;
        }

        private void Update()
        {
            if (_timeDestructor > 0)
            {
                _timeDestructor -= Time.deltaTime;
            }
            else
            {
                OnDestructor();
            }
        }

        private void OnDestructor()
        {
            if (_isDestroy)
            {
                Destroy(this.gameObject);
            }
            else if (_isPooler)
            {
                PoolerObject.ObjectPooling.ReturnObject(this.gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void BonusTime(float time)
        {
            _timeDestructor += time;
        }
    }
}