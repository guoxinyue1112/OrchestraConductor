using UnityEngine;

public class OrchestraSectionVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color inactiveColor = new(1f, 1f, 1f, 1f);
    [SerializeField] private Color activeColor = new(1f, 0.86f, 0.62f, 1f);
    [SerializeField] private float emissionIntensity = 1.1f;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Reset()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    public void SetActiveVisual(bool isActive)
    {
        _propertyBlock ??= new MaterialPropertyBlock();

        Color baseColor = isActive ? activeColor : inactiveColor;
        Color emissionColor = isActive ? activeColor * emissionIntensity : Color.black;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, baseColor);
            _propertyBlock.SetColor(ColorId, baseColor);
            _propertyBlock.SetColor(EmissionColorId, emissionColor);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
