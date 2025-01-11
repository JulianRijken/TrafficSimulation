using UnityEngine;

namespace TrafficSimulation
{
    public class RandomColorMaterial : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private int[] _materialIndex;
        [SerializeField] private Color[] _colors;

        private void Start()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshRenderer == null)
            {
                Debug.LogError("No MeshRenderer found on GameObject or assigned to RandomColorMaterial component");
                return;
            }


            Color color = _colors[UnityEngine.Random.Range(0, _colors.Length)];
            foreach (int i in _materialIndex)
            {
                _meshRenderer.materials[i].color = color;
            }
        }
    }
}
