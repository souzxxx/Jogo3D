using UnityEngine;

namespace Quintessence.Game
{
    /// Mantem um rotulo do mundo virado para a camera.
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
