using UnityEngine;


namespace AlignedGames

{

    public class RotationBehaviour : MonoBehaviour

    {

        public Vector3 rotationSpeed = new Vector3(0f, 90f, 0f); // Degrees per second

        void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

    }

}