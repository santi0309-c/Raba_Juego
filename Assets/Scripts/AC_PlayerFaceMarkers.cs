using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class AC_PlayerFaceMarkers : MonoBehaviour
{
    [Header("Ojos")]
    public float eyeHeight = 0.65f;
    public float eyeForward = 0.505f;
    public float eyeSeparation = 0.28f;
    public float eyeSize = 0.095f;

    private const string LeftEyeName = "Eye_L";
    private const string RightEyeName = "Eye_R";
    private static Material sharedEyeMaterial;

    private void OnEnable()
    {
        EnsureEyes();
    }

    private void OnValidate()
    {
        EnsureEyes();
    }

    private void EnsureEyes()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        EnsureEye(LeftEyeName, -eyeSeparation * 0.5f);
        EnsureEye(RightEyeName, eyeSeparation * 0.5f);
    }

    private void EnsureEye(string eyeName, float x)
    {
        Transform eye = transform.Find(eyeName);
        if (eye == null)
        {
            GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeObject.name = eyeName;
            eyeObject.transform.SetParent(transform, false);

            Collider eyeCollider = eyeObject.GetComponent<Collider>();
            if (eyeCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(eyeCollider);
                }
                else
                {
                    DestroyImmediate(eyeCollider);
                }
            }

            eye = eyeObject.transform;
        }

        eye.localPosition = new Vector3(x, eyeHeight, eyeForward);
        eye.localRotation = Quaternion.identity;
        eye.localScale = new Vector3(eyeSize, eyeSize, eyeSize * 0.35f);

        Renderer renderer = eye.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetEyeMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static Material GetEyeMaterial()
    {
        if (sharedEyeMaterial == null)
        {
            sharedEyeMaterial = new Material(Shader.Find("Standard"));
            sharedEyeMaterial.name = "Runtime_Black_Eyes";
            sharedEyeMaterial.color = Color.black;
        }

        return sharedEyeMaterial;
    }
}
