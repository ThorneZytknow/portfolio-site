using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // Lista de alvos (jogadores) na arena que a câmera deve enquadrar
    public List<Transform> targets = new List<Transform>();

    [Header("Configurações de Posição")]
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Afastamento padrão Z para 2D
    public float smoothTime = 0.5f; // Suavidade da movimentação da câmera

    [Header("Configurações de Zoom (Ortho/Persp)")]
    public float minZoom = 5f;
    public float maxZoom = 15f;
    public float zoomLimiter = 10f; // Quanto maior, menos zoom out é feito baseado na distância

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (targets.Count == 0) return;

        MoveCamera();
        ZoomCamera();
    }

    /// <summary>
    /// Adiciona um jogador à lista de alvos (chamado pelo GameManager/PlayerController no Spawn)
    /// </summary>
    public void AddTarget(Transform newTarget)
    {
        if (!targets.Contains(newTarget))
        {
            targets.Add(newTarget);
        }
    }

    /// <summary>
    /// Remove um jogador (em caso de Ring Out ou Desconexão)
    /// </summary>
    public void RemoveTarget(Transform oldTarget)
    {
        if (targets.Contains(oldTarget))
        {
            targets.Remove(oldTarget);
        }
    }

    /// <summary>
    /// Move a câmera para o ponto central entre todos os jogadores vivos
    /// </summary>
    private void MoveCamera()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 newPosition = centerPoint + offset;

        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    /// <summary>
    /// Ajusta o Field of View (3D) ou o Orthographic Size (2D) da câmera baseado na distância entre os jogadores
    /// </summary>
    private void ZoomCamera()
    {
        float greatestDistance = GetGreatestDistance();

        // Se a câmera for Ortográfica (2D)
        if (cam.orthographic)
        {
            float newZoom = Mathf.Lerp(minZoom, maxZoom, greatestDistance / zoomLimiter);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime);
        }
        else // Se a câmera for Perspectiva (3D)
        {
            float newZoom = Mathf.Lerp(minZoom, maxZoom, greatestDistance / zoomLimiter);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
        }
    }

    /// <summary>
    /// Encontra a distância máxima entre os jogadores mais distantes um do outro
    /// </summary>
    private float GetGreatestDistance()
    {
        if (targets.Count <= 1) return 0f;

        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            // Ignora jogadores destruídos ou inativos
            if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
            {
                bounds.Encapsulate(targets[i].position);
            }
        }

        // A maior distância pode ser na largura (X) ou altura (Y)
        return Mathf.Max(bounds.size.x, bounds.size.y);
    }

    /// <summary>
    /// Encontra o ponto central perfeito enquadrando todos os alvos
    /// </summary>
    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
        {
            return targets[0] != null ? targets[0].position : Vector3.zero;
        }

        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
            {
                bounds.Encapsulate(targets[i].position);
            }
        }

        return bounds.center;
    }
}
